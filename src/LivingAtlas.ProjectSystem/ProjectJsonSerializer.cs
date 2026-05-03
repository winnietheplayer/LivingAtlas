using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.ProjectSystem;

public static class ProjectJsonSerializer
{
	private sealed class ProjectDto
	{
		public Guid Id { get; init; }

		public string Name { get; init; } = string.Empty;

		public Guid RootMapId { get; init; }

		public List<MapDocumentDto> Maps { get; init; } = new List<MapDocumentDto>();

		public static ProjectDto FromProject(CampaignMapProject project)
		{
			return new ProjectDto
			{
				Id = project.Id,
				Name = project.Name,
				RootMapId = project.RootMapId,
				Maps = project.Maps.Select(MapDocumentDto.FromMap).ToList()
			};
		}

		public CampaignMapProject ToProject()
		{
			return new CampaignMapProject(Id, Name, RootMapId, Maps.Select((MapDocumentDto map) => map.ToMapDocument()));
		}
	}

	private sealed class MapDocumentDto
	{
		public Guid Id { get; init; }

		public string Name { get; init; } = string.Empty;

		public MapScaleType ScaleType { get; init; }

		public SizeDto RealSizeMeters { get; init; } = new SizeDto();

		public Guid? ParentMapId { get; init; }

		public GridSettingsDto GridSettings { get; init; } = new GridSettingsDto();

		public List<MapLayerDto> Layers { get; init; } = new List<MapLayerDto>();

		public List<Guid> ChildrenMapIds { get; init; } = new List<Guid>();

		public static MapDocumentDto FromMap(MapDocument map)
		{
			return new MapDocumentDto
			{
				Id = map.Id,
				Name = map.Name,
				ScaleType = map.ScaleType,
				RealSizeMeters = SizeDto.FromSize(map.RealSizeMeters),
				ParentMapId = map.ParentMapId,
				GridSettings = GridSettingsDto.FromGridSettings(map.GridSettings),
				Layers = map.Layers.Select(MapLayerDto.FromLayer).ToList(),
				ChildrenMapIds = map.ChildrenMapIds.ToList()
			};
		}

		public MapDocument ToMapDocument()
		{
			MapDocument mapDocument = new MapDocument(Id, Name, ScaleType, RealSizeMeters.ToSizeD(), ParentMapId, GridSettings.ToGridSettings());
			foreach (MapLayerDto layer in Layers)
			{
				mapDocument.AddLayer(layer.ToMapLayer());
			}
			foreach (Guid childrenMapId in ChildrenMapIds)
			{
				mapDocument.AddChildMapId(childrenMapId);
			}
			return mapDocument;
		}
	}

	private sealed class SizeDto
	{
		public double Width { get; init; }

		public double Height { get; init; }

		public static SizeDto FromSize(SizeD size)
		{
			return new SizeDto
			{
				Width = size.Width,
				Height = size.Height
			};
		}

		public SizeD ToSizeD()
		{
			return new SizeD(Width, Height);
		}
	}

	private sealed class GridSettingsDto
	{
		public bool IsEnabled { get; init; }

		public double CellSizeMeters { get; init; } = 1.0;

		public bool ShowGrid { get; init; }

		public bool SnapToGrid { get; init; }

		public static GridSettingsDto FromGridSettings(GridSettings gridSettings)
		{
			return new GridSettingsDto
			{
				IsEnabled = gridSettings.IsEnabled,
				CellSizeMeters = gridSettings.CellSizeMeters,
				ShowGrid = gridSettings.ShowGrid,
				SnapToGrid = gridSettings.SnapToGrid
			};
		}

		public GridSettings ToGridSettings()
		{
			return new GridSettings(IsEnabled, CellSizeMeters, ShowGrid, SnapToGrid);
		}
	}

	private sealed class MapLayerDto
	{
		public Guid Id { get; init; }

		public string Name { get; init; } = string.Empty;

		public MapLayerType LayerType { get; init; }

		public bool IsVisible { get; init; }

		public List<MapObjectDto> Objects { get; init; } = new List<MapObjectDto>();

		public static MapLayerDto FromLayer(MapLayer layer)
		{
			return new MapLayerDto
			{
				Id = layer.Id,
				Name = layer.Name,
				LayerType = layer.LayerType,
				IsVisible = layer.IsVisible,
				Objects = layer.Objects.Select(MapObjectDto.FromMapObject).ToList()
			};
		}

		public MapLayer ToMapLayer()
		{
			MapLayer mapLayer = new MapLayer(Id, Name, LayerType, IsVisible);
			foreach (MapObjectDto @object in Objects)
			{
				mapLayer.AddObject(@object.ToMapObject());
			}
			return mapLayer;
		}
	}

	private sealed record MapObjectDto
	{
		public Guid Id { get; init; }

		public string Name { get; init; } = string.Empty;

		public MapObjectType ObjectType { get; init; }

		public Guid LayerId { get; init; }

		public List<string> Tags { get; init; } = new List<string>();

		public string StyleKey { get; init; } = string.Empty;

		public List<PointDto> Points { get; init; } = new List<PointDto>();

		public PointDto? Position { get; init; }

		public string? Text { get; init; }

		public string? IconKey { get; init; }

		public Guid? ChildMapId { get; init; }

		public static MapObjectDto FromMapObject(MapObject mapObject)
		{
			return mapObject switch
			{
				DistrictShape districtShape => CreateBase(mapObject)with
				{
					Points = districtShape.PolygonPoints.Select(PointDto.FromPoint).ToList(),
					ChildMapId = districtShape.ChildMapId
				},
				RoadLine roadLine => CreateBase(mapObject)with
				{
					Points = roadLine.Points.Select(PointDto.FromPoint).ToList()
				},
				MapLabel mapLabel => CreateBase(mapObject)with
				{
					Position = PointDto.FromPoint(mapLabel.Position),
					Text = mapLabel.Text
				},
				PointOfInterest pointOfInterest => CreateBase(mapObject)with
				{
					Position = PointDto.FromPoint(pointOfInterest.Position),
					IconKey = pointOfInterest.IconKey
				},
				_ => throw new NotSupportedException("Unsupported map object type '" + mapObject.GetType().Name + "'.")
			};

			static MapObjectDto CreateBase(MapObject mapObject2)
			{
				return new MapObjectDto
				{
					Id = mapObject2.Id,
					Name = mapObject2.Name,
					ObjectType = mapObject2.ObjectType,
					LayerId = mapObject2.LayerId,
					Tags = mapObject2.Tags.ToList(),
					StyleKey = mapObject2.StyleKey
				};
			}
		}

		public MapObject ToMapObject()
		{
			return ObjectType switch
			{
				MapObjectType.DistrictShape => new DistrictShape(Id, Name, LayerId, Points.Select((PointDto point) => point.ToPointD()), Tags, StyleKey, ChildMapId), 
				MapObjectType.RoadLine => new RoadLine(Id, Name, LayerId, Points.Select((PointDto point) => point.ToPointD()), Tags, StyleKey), 
				MapObjectType.MapLabel => new MapLabel(Id, Name, LayerId, RequirePosition().ToPointD(), RequireText(), Tags, StyleKey), 
				MapObjectType.PointOfInterest => new PointOfInterest(Id, Name, LayerId, RequirePosition().ToPointD(), RequireIconKey(), Tags, StyleKey), 
				_ => throw new NotSupportedException($"Unsupported map object type '{ObjectType}'."), 
			};
		}

		private PointDto RequirePosition()
		{
			return Position ?? throw new InvalidDataException($"Map object '{Id}' requires a position.");
		}

		private string RequireText()
		{
			return Text ?? throw new InvalidDataException($"Map label '{Id}' requires text.");
		}

		private string RequireIconKey()
		{
			return IconKey ?? throw new InvalidDataException($"Point of interest '{Id}' requires an icon key.");
		}
	}

	private sealed class PointDto
	{
		public double X { get; init; }

		public double Y { get; init; }

		public static PointDto FromPoint(PointD point)
		{
			return new PointDto
			{
				X = point.X,
				Y = point.Y
			};
		}

		public PointD ToPointD()
		{
			return new PointD(X, Y);
		}
	}

	private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

	public static async Task SaveAsync(CampaignMapProject project, string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		ArgumentNullException.ThrowIfNull(project, "project");
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Project path cannot be empty.", "path");
		}
		string fullPath = Path.GetFullPath(path);
		string directory = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}
		await using FileStream stream = File.Create(fullPath);
		await JsonSerializer.SerializeAsync((Stream)stream, ProjectDto.FromProject(project), JsonOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static async Task<CampaignMapProject> LoadAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Project path cannot be empty.", "path");
		}
		CampaignMapProject result;
		await using (FileStream stream = File.OpenRead(Path.GetFullPath(path)))
		{
			result = (await JsonSerializer.DeserializeAsync<ProjectDto>((Stream)stream, JsonOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false))?.ToProject() ?? throw new InvalidDataException("Project file '" + path + "' is empty or invalid.");
		}
		return result;
	}

	private static JsonSerializerOptions CreateJsonOptions()
	{
		JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};
		jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
		return jsonSerializerOptions;
	}
}
