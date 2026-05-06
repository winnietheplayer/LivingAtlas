using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Export;
using SkiaSharp;
using Xunit;

namespace LivingAtlas.Tests;

public class PngExportTests
{
	[Fact]
	public void PngExportOptions_DefaultDisablesChildMapPreviews()
	{
		var options = new PngExportOptions(CreateTempPngPath());

		Assert.False(options.IncludeChildMapPreviews);
	}

	[Fact]
	public void ExportPngViewModel_DefaultDisablesChildMapPreviews()
	{
		var viewModel = new ExportPngViewModel(CreateTempPngPath(), includeGrid: true);

		Assert.False(viewModel.IncludeChildMapPreviews);
	}

	[Fact]
	public void ValidateAndGetImageSize_RejectsInvalidScale()
	{
		var map = CreateSmallMap();
		var options = new PngExportOptions(CreateTempPngPath())
		{
			ResolutionScale = 3
		};

		Assert.Throws<ArgumentOutOfRangeException>(() => PngMapExporter.ValidateAndGetImageSize(map, options));
	}

	[Fact]
	public void ValidateAndGetImageSize_RejectsTooLargeDimensions()
	{
		var map = new MapDocument(Guid.NewGuid(), "Huge Map", MapScaleType.City, new SizeD(8193.0, 64.0));
		var options = new PngExportOptions(CreateTempPngPath())
		{
			ResolutionScale = 1
		};

		Assert.Throws<InvalidOperationException>(() => PngMapExporter.ValidateAndGetImageSize(map, options));
	}

	[Fact]
	public void Export_CreatesPngWithExpectedDimensions()
	{
		string path = CreateTempPngPath();
		try
		{
			var map = CreateSmallMap();
			var project = new CampaignMapProject(Guid.NewGuid(), "Export Test", map.Id, new[] { map });
			var options = new PngExportOptions(path)
			{
				ResolutionScale = 1
			};

			new PngMapExporter().Export(project, map, options);

			Assert.True(File.Exists(path));
			Assert.True(new FileInfo(path).Length > 0);
			using SKBitmap bitmap = SKBitmap.Decode(path);
			Assert.Equal(64, bitmap.Width);
			Assert.Equal(48, bitmap.Height);
		}
		finally
		{
			DeleteIfExists(path);
		}
	}

	[Fact]
	public void Export_SkipsObjectsOnHiddenLayers()
	{
		string path = CreateTempPngPath();
		try
		{
			var map = CreateSmallMap();
			var hiddenLayer = new MapLayer(Guid.NewGuid(), "Hidden POI", MapLayerType.PointsOfInterest, isVisible: false);
			hiddenLayer.AddObject(new PointOfInterest(Guid.NewGuid(), "Hidden Gate", hiddenLayer.Id, new PointD(32.0, 24.0), "gate", styleKey: "poi.gate"));
			map.AddLayer(hiddenLayer);
			var project = new CampaignMapProject(Guid.NewGuid(), "Export Test", map.Id, new[] { map });

			new PngMapExporter().Export(project, map, new PngExportOptions(path));

			using SKBitmap bitmap = SKBitmap.Decode(path);
			Assert.Equal(new SKColor(38, 43, 50), bitmap.GetPixel(32, 24));
		}
		finally
		{
			DeleteIfExists(path);
		}
	}

	[Fact]
	public void Export_DoesNotMutateMapOrProjectState()
	{
		string path = CreateTempPngPath();
		try
		{
			var map = CreateSmallMap();
			var layer = new MapLayer(Guid.NewGuid(), "POI", MapLayerType.PointsOfInterest);
			var poi = new PointOfInterest(Guid.NewGuid(), "Gate", layer.Id, new PointD(12.0, 18.0), "gate");
			layer.AddObject(poi);
			map.AddLayer(layer);
			var project = new CampaignMapProject(Guid.NewGuid(), "Export Test", map.Id, new[] { map });

			new PngMapExporter().Export(project, map, new PngExportOptions(path)
			{
				IncludeGrid = true,
				IncludeChildMapPreviews = true
			});

			Assert.Equal(map.Id, project.RootMapId);
			Assert.Single(project.Maps);
			Assert.Single(map.Layers);
			Assert.Single(layer.Objects);
			Assert.Equal(new PointD(12.0, 18.0), poi.Position);
			Assert.True(layer.IsVisible);
		}
		finally
		{
			DeleteIfExists(path);
		}
	}

	[Theory]
	[InlineData(2, 128, 96)]
	[InlineData(4, 256, 192)]
	public void Export_ResolutionScaleChangesDimensions(int scale, int expectedWidth, int expectedHeight)
	{
		string path = CreateTempPngPath();
		try
		{
			var map = CreateSmallMap();
			var project = new CampaignMapProject(Guid.NewGuid(), "Export Test", map.Id, new[] { map });

			new PngMapExporter().Export(project, map, new PngExportOptions(path)
			{
				ResolutionScale = scale
			});

			using SKBitmap bitmap = SKBitmap.Decode(path);
			Assert.Equal(expectedWidth, bitmap.Width);
			Assert.Equal(expectedHeight, bitmap.Height);
		}
		finally
		{
			DeleteIfExists(path);
		}
	}

	[Fact]
	public void Export_CanOmitPointsOfInterest()
	{
		string withPoiPath = CreateTempPngPath();
		string withoutPoiPath = CreateTempPngPath();
		try
		{
			var map = CreateSmallMap();
			var layer = new MapLayer(Guid.NewGuid(), "POI", MapLayerType.PointsOfInterest);
			layer.AddObject(new PointOfInterest(Guid.NewGuid(), "Gate", layer.Id, new PointD(32.0, 24.0), "gate", styleKey: "poi.gate"));
			map.AddLayer(layer);
			var project = new CampaignMapProject(Guid.NewGuid(), "Export Test", map.Id, new[] { map });

			new PngMapExporter().Export(project, map, new PngExportOptions(withPoiPath)
			{
				IncludePointsOfInterest = true
			});
			new PngMapExporter().Export(project, map, new PngExportOptions(withoutPoiPath)
			{
				IncludePointsOfInterest = false
			});

			using SKBitmap withPoi = SKBitmap.Decode(withPoiPath);
			using SKBitmap withoutPoi = SKBitmap.Decode(withoutPoiPath);
			Assert.NotEqual(new SKColor(38, 43, 50), withPoi.GetPixel(32, 24));
			Assert.Equal(new SKColor(38, 43, 50), withoutPoi.GetPixel(32, 24));
		}
		finally
		{
			DeleteIfExists(withPoiPath);
			DeleteIfExists(withoutPoiPath);
		}
	}

	[Fact]
	public void Export_CanOmitLabels()
	{
		string emptyMapPath = CreateTempPngPath();
		string labelEnabledPath = CreateTempPngPath();
		string labelDisabledPath = CreateTempPngPath();
		try
		{
			var emptyMap = CreateTextMap();
			var mapWithLabel = CreateTextMap();
			var layer = new MapLayer(Guid.NewGuid(), "Labels", MapLayerType.Labels);
			layer.AddObject(new MapLabel(Guid.NewGuid(), "Title", layer.Id, new PointD(20.0, 22.0), "VISIBLE", styleKey: "label.map-title"));
			mapWithLabel.AddLayer(layer);
			var emptyProject = new CampaignMapProject(Guid.NewGuid(), "Export Test", emptyMap.Id, new[] { emptyMap });
			var labelProject = new CampaignMapProject(Guid.NewGuid(), "Export Test", mapWithLabel.Id, new[] { mapWithLabel });

			new PngMapExporter().Export(emptyProject, emptyMap, new PngExportOptions(emptyMapPath));
			new PngMapExporter().Export(labelProject, mapWithLabel, new PngExportOptions(labelEnabledPath)
			{
				IncludeLabels = true
			});
			new PngMapExporter().Export(labelProject, mapWithLabel, new PngExportOptions(labelDisabledPath)
			{
				IncludeLabels = false
			});

			using SKBitmap empty = SKBitmap.Decode(emptyMapPath);
			using SKBitmap withLabel = SKBitmap.Decode(labelEnabledPath);
			using SKBitmap withoutLabel = SKBitmap.Decode(labelDisabledPath);
			Assert.True(BitmapsEqual(empty, withoutLabel));
			Assert.False(BitmapsEqual(empty, withLabel));
		}
		finally
		{
			DeleteIfExists(emptyMapPath);
			DeleteIfExists(labelEnabledPath);
			DeleteIfExists(labelDisabledPath);
		}
	}

	[Fact]
	public void Export_TransparentBackgroundKeepsEmptyPixelsTransparent()
	{
		string path = CreateTempPngPath();
		try
		{
			var map = CreateSmallMap();
			var project = new CampaignMapProject(Guid.NewGuid(), "Export Test", map.Id, new[] { map });

			new PngMapExporter().Export(project, map, new PngExportOptions(path)
			{
				TransparentBackground = true
			});

			using SKBitmap bitmap = SKBitmap.Decode(path);
			Assert.Equal(0, bitmap.GetPixel(10, 10).Alpha);
		}
		finally
		{
			DeleteIfExists(path);
		}
	}

	private static MapDocument CreateSmallMap()
	{
		return new MapDocument(
			Guid.NewGuid(),
			"Small Map",
			MapScaleType.City,
			new SizeD(64.0, 48.0),
			gridSettings: new GridSettings(isEnabled: true, cellSizeMeters: 8.0, showGrid: false, snapToGrid: false));
	}

	private static MapDocument CreateTextMap()
	{
		return new MapDocument(
			Guid.NewGuid(),
			"Text Map",
			MapScaleType.City,
			new SizeD(120.0, 80.0),
			gridSettings: new GridSettings(isEnabled: true, cellSizeMeters: 10.0, showGrid: false, snapToGrid: false));
	}

	private static string CreateTempPngPath()
	{
		return Path.Combine(Path.GetTempPath(), $"living-atlas-export-{Guid.NewGuid():N}.png");
	}

	private static void DeleteIfExists(string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	private static bool BitmapsEqual(SKBitmap left, SKBitmap right)
	{
		if (left.Width != right.Width || left.Height != right.Height)
		{
			return false;
		}

		for (int y = 0; y < left.Height; y++)
		{
			for (int x = 0; x < left.Width; x++)
			{
				if (left.GetPixel(x, y) != right.GetPixel(x, y))
				{
					return false;
				}
			}
		}

		return true;
	}
}
