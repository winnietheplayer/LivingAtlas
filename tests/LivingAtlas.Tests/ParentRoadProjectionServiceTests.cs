using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Hierarchy;
using LivingAtlas.Export;
using SkiaSharp;

namespace LivingAtlas.Tests;

public sealed class ParentRoadProjectionServiceTests
{
	[Fact]
	public void GetProjectedRoadAreas_ReturnsProjectedParentRoadAreaForChildMap()
	{
		(CampaignMapProject project, MapDocument childMap, RoadArea roadArea, _, _) = CreateProjectionScenario();

		IReadOnlyList<ParentRoadOverlay> overlays = ParentRoadProjectionService.GetProjectedRoadAreas(project, childMap.Id);

		ParentRoadOverlay overlay = Assert.Single(overlays);
		Assert.Equal(roadArea.Id, overlay.SourceRoadAreaId);
		Assert.Equal("Parent Road", overlay.Name);
		Assert.Equal("road.area.primary", overlay.StyleKey);
		Assert.Equal("primary", overlay.RoadKind);
		Assert.Equal("road.cobble.01", overlay.FillTextureAssetId);
		Assert.Equal(6.0, overlay.TextureTileSizeMeters);
		Assert.Equal(new PointD(100.0, 50.0), overlay.ProjectedPolygonPoints[0]);
		Assert.Equal(new PointD(200.0, 50.0), overlay.ProjectedPolygonPoints[1]);
		Assert.Equal(new PointD(200.0, 80.0), overlay.ProjectedPolygonPoints[2]);
		Assert.Equal(new PointD(100.0, 80.0), overlay.ProjectedPolygonPoints[3]);
	}

	[Fact]
	public void GetProjectedRoadAreas_SkipsRoadAreaOutsideLinkedDistrict()
	{
		(CampaignMapProject project, MapDocument childMap, _, MapLayer roadLayer, _) = CreateProjectionScenario();
		roadLayer.AddObject(new RoadArea(
			Guid.NewGuid(),
			"Outside Road",
			roadLayer.Id,
			new[]
			{
				new PointD(700.0, 700.0),
				new PointD(800.0, 700.0),
				new PointD(800.0, 740.0),
				new PointD(700.0, 740.0)
			}));

		IReadOnlyList<ParentRoadOverlay> overlays = ParentRoadProjectionService.GetProjectedRoadAreas(project, childMap.Id);

		Assert.Single(overlays);
		Assert.DoesNotContain(overlays, overlay => overlay.Name == "Outside Road");
	}

	[Fact]
	public void GetProjectedRoadAreas_SkipsHiddenParentRoadLayer()
	{
		(CampaignMapProject project, MapDocument childMap, _, MapLayer roadLayer, _) = CreateProjectionScenario();
		roadLayer.SetVisibility(false);

		IReadOnlyList<ParentRoadOverlay> overlays = ParentRoadProjectionService.GetProjectedRoadAreas(project, childMap.Id);

		Assert.Empty(overlays);
	}

	[Fact]
	public void GetProjectedRoadAreas_IncludesLockedVisibleParentRoadLayer()
	{
		(CampaignMapProject project, MapDocument childMap, RoadArea roadArea, MapLayer roadLayer, _) = CreateProjectionScenario();
		roadLayer.SetLocked(true);

		IReadOnlyList<ParentRoadOverlay> overlays = ParentRoadProjectionService.GetProjectedRoadAreas(project, childMap.Id);

		Assert.Equal(roadArea.Id, Assert.Single(overlays).SourceRoadAreaId);
	}

	[Fact]
	public void GetProjectedRoadAreas_UsesLinkedDistrictEvenWhenDistrictLayerHidden()
	{
		(CampaignMapProject project, MapDocument childMap, RoadArea roadArea, _, MapLayer districtLayer) = CreateProjectionScenario();
		districtLayer.SetVisibility(false);

		IReadOnlyList<ParentRoadOverlay> overlays = ParentRoadProjectionService.GetProjectedRoadAreas(project, childMap.Id);

		Assert.Equal(roadArea.Id, Assert.Single(overlays).SourceRoadAreaId);
	}

	[Fact]
	public void GetProjectedRoadAreas_DoesNotAddOverlayToChildLayers()
	{
		(CampaignMapProject project, MapDocument childMap, _, _, _) = CreateProjectionScenario();

		_ = ParentRoadProjectionService.GetProjectedRoadAreas(project, childMap.Id);

		Assert.Empty(childMap.Layers);
	}

	[Fact]
	public void PngExport_ChildMapIncludesParentRoadOverlayWhenChildPreviewsEnabled()
	{
		(CampaignMapProject project, MapDocument childMap, _, _, _) = CreateProjectionScenario();
		string tempFileName = Path.ChangeExtension(Path.GetTempFileName(), ".png");
		try
		{
			new PngMapExporter().Export(
				project,
				childMap,
				new PngExportOptions(tempFileName)
				{
					IncludeGrid = false,
					IncludeChildMapPreviews = true,
					TransparentBackground = true
				});

			using SKBitmap bitmap = SKBitmap.Decode(tempFileName);
			Assert.True(bitmap.GetPixel(120, 60).Alpha > 0);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	private static (CampaignMapProject Project, MapDocument ChildMap, RoadArea RoadArea, MapLayer RoadLayer, MapLayer DistrictLayer) CreateProjectionScenario()
	{
		MapDocument parentMap = new MapDocument(Guid.NewGuid(), "Parent", MapScaleType.City, new SizeD(1000.0, 1000.0));
		MapDocument childMap = new MapDocument(Guid.NewGuid(), "Child", MapScaleType.District, new SizeD(400.0, 300.0), parentMap.Id);
		MapLayer districtLayer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
		MapLayer roadLayer = new MapLayer(Guid.NewGuid(), "Roads", MapLayerType.Streets);
		DistrictShape district = new DistrictShape(
			Guid.NewGuid(),
			"Linked District",
			districtLayer.Id,
			new[]
			{
				new PointD(100.0, 100.0),
				new PointD(500.0, 100.0),
				new PointD(500.0, 400.0),
				new PointD(100.0, 400.0)
			},
			childMapId: childMap.Id);
		RoadArea roadArea = new RoadArea(
			Guid.NewGuid(),
			"Parent Road",
			roadLayer.Id,
			new[]
			{
				new PointD(200.0, 150.0),
				new PointD(300.0, 150.0),
				new PointD(300.0, 180.0),
				new PointD(200.0, 180.0)
			},
			styleKey: "road.area.primary",
			roadKind: "primary");
		roadArea.SetTextureFill("road.cobble.01", 6.0);
		districtLayer.AddObject(district);
		roadLayer.AddObject(roadArea);
		parentMap.AddLayer(districtLayer);
		parentMap.AddLayer(roadLayer);
		parentMap.AddChildMapId(childMap.Id);
		CampaignMapProject project = new CampaignMapProject(Guid.NewGuid(), "Projection Project", parentMap.Id, new[] { parentMap, childMap });
		return (project, childMap, roadArea, roadLayer, districtLayer);
	}
}
