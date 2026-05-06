using System;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class UpdateMapObjectPropertiesCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private readonly MapObjectPropertySnapshot _oldSnapshot;

	private readonly MapObjectPropertySnapshot _newSnapshot;

	public string Description => "Update " + _oldSnapshot.Name;

	public UpdateMapObjectPropertiesCommand(
		MapDocument map,
		Guid objectId,
		string newName,
		string? newLabelText = null,
		string? newStyleKey = null,
		string? newDescription = null,
		string? newCategory = null,
		string? newRoadKind = null,
		string? newDistrictKind = null,
		string? newLabelKind = null,
		bool updateDistrictTextureFill = false,
		string? newFillTextureAssetId = null,
		double? newTextureTileSizeMeters = null)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		if (string.IsNullOrWhiteSpace(newName))
		{
			throw new ArgumentException("Map object name cannot be empty.", "newName");
		}

		_map = map;
		_objectId = objectId;

		MapObject mapObject = ResolveObject();
		_oldSnapshot = MapObjectPropertySnapshot.FromObject(mapObject);
		_newSnapshot = CreateNewSnapshot(mapObject, newName, newLabelText, newStyleKey, newDescription, newCategory, newRoadKind, newDistrictKind, newLabelKind, updateDistrictTextureFill, newFillTextureAssetId, newTextureTileSizeMeters);
	}

	public void Execute()
	{
		Apply(_newSnapshot);
	}

	public void Undo()
	{
		Apply(_oldSnapshot);
	}

	private static MapObjectPropertySnapshot CreateNewSnapshot(
		MapObject mapObject,
		string newName,
		string? newLabelText,
		string? newStyleKey,
		string? newDescription,
		string? newCategory,
		string? newRoadKind,
		string? newDistrictKind,
		string? newLabelKind,
		bool updateDistrictTextureFill,
		string? newFillTextureAssetId,
		double? newTextureTileSizeMeters)
	{
		string? labelText = null;
		string? category = null;
		string? roadKind = null;
		string? districtKind = null;
		string? labelKind = null;
		string? fillTextureAssetId = null;
		double? textureTileSizeMeters = null;

		if (mapObject is PointOfInterest pointOfInterest)
		{
			ThrowIfProvided(newLabelText, "Only map labels can update label text.", "newLabelText");
			ThrowIfProvided(newRoadKind, "Only road lines can update road kind.", "newRoadKind");
			ThrowIfProvided(newDistrictKind, "Only district shapes can update district kind.", "newDistrictKind");
			ThrowIfProvided(newLabelKind, "Only map labels can update label kind.", "newLabelKind");
			ThrowIfDistrictTextureProvided(updateDistrictTextureFill, newFillTextureAssetId, newTextureTileSizeMeters);
			category = newCategory ?? pointOfInterest.Category;
		}
		else if (mapObject is RoadLine roadLine)
		{
			ThrowIfProvided(newLabelText, "Only map labels can update label text.", "newLabelText");
			ThrowIfProvided(newCategory, "Only points of interest can update category.", "newCategory");
			ThrowIfProvided(newDistrictKind, "Only district shapes can update district kind.", "newDistrictKind");
			ThrowIfProvided(newLabelKind, "Only map labels can update label kind.", "newLabelKind");
			ThrowIfDistrictTextureProvided(updateDistrictTextureFill, newFillTextureAssetId, newTextureTileSizeMeters);
			roadKind = newRoadKind ?? roadLine.RoadKind;
		}
		else if (mapObject is DistrictShape districtShape)
		{
			ThrowIfProvided(newLabelText, "Only map labels can update label text.", "newLabelText");
			ThrowIfProvided(newCategory, "Only points of interest can update category.", "newCategory");
			ThrowIfProvided(newRoadKind, "Only road lines can update road kind.", "newRoadKind");
			ThrowIfProvided(newLabelKind, "Only map labels can update label kind.", "newLabelKind");
			districtKind = newDistrictKind ?? districtShape.DistrictKind;
			fillTextureAssetId = districtShape.FillTextureAssetId;
			textureTileSizeMeters = districtShape.TextureTileSizeMeters;
			if (updateDistrictTextureFill)
			{
				fillTextureAssetId = newFillTextureAssetId;
				textureTileSizeMeters = newTextureTileSizeMeters ?? throw new ArgumentException("Texture tile size must be provided when updating district texture fill.", "newTextureTileSizeMeters");
			}
		}
		else if (mapObject is MapLabel mapLabel)
		{
			ThrowIfProvided(newCategory, "Only points of interest can update category.", "newCategory");
			ThrowIfProvided(newRoadKind, "Only road lines can update road kind.", "newRoadKind");
			ThrowIfProvided(newDistrictKind, "Only district shapes can update district kind.", "newDistrictKind");
			ThrowIfDistrictTextureProvided(updateDistrictTextureFill, newFillTextureAssetId, newTextureTileSizeMeters);
			labelText = mapLabel.Text;
			labelKind = newLabelKind ?? mapLabel.LabelKind;
			if (newLabelText != null)
			{
				if (string.IsNullOrWhiteSpace(newLabelText))
				{
					throw new ArgumentException("Label text cannot be empty.", "newLabelText");
				}
				labelText = newLabelText.Trim();
			}
		}
		else
		{
			throw new NotSupportedException("Unsupported map object type '" + mapObject.GetType().Name + "'.");
		}

		return new MapObjectPropertySnapshot(
			newName.Trim(),
			(newStyleKey ?? mapObject.StyleKey).Trim(),
			newDescription ?? mapObject.Description,
			labelText,
			category,
			roadKind,
			districtKind,
			labelKind,
			fillTextureAssetId,
			textureTileSizeMeters);
	}

	private void Apply(MapObjectPropertySnapshot snapshot)
	{
		MapObject mapObject = ResolveObject();
		mapObject.Rename(snapshot.Name);
		mapObject.SetStyleKey(snapshot.StyleKey);
		mapObject.SetDescription(snapshot.Description);
		if (mapObject is PointOfInterest pointOfInterest && snapshot.Category != null)
		{
			pointOfInterest.SetCategory(snapshot.Category);
		}
		else if (mapObject is RoadLine roadLine && snapshot.RoadKind != null)
		{
			roadLine.SetRoadKind(snapshot.RoadKind);
		}
		else if (mapObject is DistrictShape districtShape && snapshot.DistrictKind != null)
		{
			districtShape.SetDistrictKind(snapshot.DistrictKind);
			if (snapshot.TextureTileSizeMeters.HasValue)
			{
				if (snapshot.FillTextureAssetId == null)
				{
					districtShape.ClearTextureFill();
				}
				else
				{
					districtShape.SetTextureFill(snapshot.FillTextureAssetId, snapshot.TextureTileSizeMeters.Value);
				}
			}
		}
		else if (mapObject is MapLabel mapLabel)
		{
			if (snapshot.LabelText != null)
			{
				mapLabel.SetText(snapshot.LabelText);
			}
			if (snapshot.LabelKind != null)
			{
				mapLabel.SetLabelKind(snapshot.LabelKind);
			}
		}
	}

	private static void ThrowIfProvided(string? value, string message, string parameterName)
	{
		if (value != null)
		{
			throw new ArgumentException(message, parameterName);
		}
	}

	private static void ThrowIfDistrictTextureProvided(bool updateDistrictTextureFill, string? fillTextureAssetId, double? textureTileSizeMeters)
	{
		if (updateDistrictTextureFill || fillTextureAssetId != null || textureTileSizeMeters.HasValue)
		{
			throw new ArgumentException("Only district shapes can update texture fill.");
		}
	}

	private MapObject ResolveObject()
	{
		foreach (MapLayer layer in _map.Layers)
		{
			MapObject? mapObject = layer.Objects.FirstOrDefault((MapObject candidate) => candidate.Id == _objectId);
			if (mapObject != null)
			{
				return mapObject;
			}
		}
		throw new InvalidOperationException($"Map object '{_objectId}' was not found in map '{_map.Id}'.");
	}

	private sealed record MapObjectPropertySnapshot(
		string Name,
		string StyleKey,
		string Description,
		string? LabelText,
		string? Category,
		string? RoadKind,
		string? DistrictKind,
		string? LabelKind,
		string? FillTextureAssetId,
		double? TextureTileSizeMeters)
	{
		public static MapObjectPropertySnapshot FromObject(MapObject mapObject)
		{
			return new MapObjectPropertySnapshot(
				mapObject.Name,
				mapObject.StyleKey,
				mapObject.Description,
				mapObject is MapLabel mapLabel ? mapLabel.Text : null,
				mapObject is PointOfInterest pointOfInterest ? pointOfInterest.Category : null,
				mapObject is RoadLine roadLine ? roadLine.RoadKind : null,
				mapObject is DistrictShape districtShape ? districtShape.DistrictKind : null,
				mapObject is MapLabel label ? label.LabelKind : null,
				mapObject is DistrictShape districtForTexture ? districtForTexture.FillTextureAssetId : null,
				mapObject is DistrictShape districtForTileSize ? districtForTileSize.TextureTileSizeMeters : null);
		}
	}
}
