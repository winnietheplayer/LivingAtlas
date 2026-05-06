using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Editor.Hierarchy;
using LivingAtlas.Editor.Tools;
using LivingAtlas.Editor.Viewport;

namespace LivingAtlas.Tests;

public sealed class ParentRoadOverlaySnapperTests
{
	[Fact]
	public void SnapToNearestOverlayVertex_SnapsToNearbyProjectedVertex()
	{
		ParentRoadOverlay overlay = CreateOverlay();
		Camera2D camera = new Camera2D(0.0, 0.0, 1.0);
		PointD rawPoint = new PointD(103.0, 98.0);
		PointD gridPoint = new PointD(100.0, 90.0);

		PointD snapped = ParentRoadOverlaySnapper.SnapToNearestOverlayVertex(
			rawPoint,
			gridPoint,
			new[] { overlay },
			camera,
			12.0);

		Assert.Equal(new PointD(100.0, 100.0), snapped);
	}

	[Fact]
	public void SnapToNearestOverlayVertex_UsesGridCandidateOutsideTolerance()
	{
		ParentRoadOverlay overlay = CreateOverlay();
		Camera2D camera = new Camera2D(0.0, 0.0, 1.0);
		PointD gridPoint = new PointD(150.0, 150.0);

		PointD snapped = ParentRoadOverlaySnapper.SnapToNearestOverlayVertex(
			new PointD(150.0, 150.0),
			gridPoint,
			new[] { overlay },
			camera,
			12.0);

		Assert.Equal(gridPoint, snapped);
	}

	[Fact]
	public void SnapToNearestOverlayVertex_DoesNotMutateOverlayData()
	{
		ParentRoadOverlay overlay = CreateOverlay();
		PointD firstPoint = overlay.ProjectedPolygonPoints[0];
		Camera2D camera = new Camera2D(0.0, 0.0, 1.0);

		_ = ParentRoadOverlaySnapper.SnapToNearestOverlayVertex(
			new PointD(103.0, 98.0),
			new PointD(100.0, 90.0),
			new[] { overlay },
			camera,
			12.0);

		Assert.Equal(firstPoint, overlay.ProjectedPolygonPoints[0]);
	}

	[Fact]
	public void RoadLineCreation_SnapsToParentOverlayVertexOnlyWhenGridSnapIsEnabled()
	{
		MapViewportViewModel viewModel = CreateChildMapViewport(snapToGrid: true);
		viewModel.SetActiveTool(EditorToolType.Road);

		viewModel.AddRoadPointAtScreenPoint(new PointD(111.0, 101.0));

		AssertPointClose(new PointD(113.0, 100.0), Assert.Single(viewModel.RoadPreviewPoints));

		MapViewportViewModel snapDisabledViewModel = CreateChildMapViewport(snapToGrid: false);
		snapDisabledViewModel.SetActiveTool(EditorToolType.Road);

		snapDisabledViewModel.AddRoadPointAtScreenPoint(new PointD(111.0, 101.0));

		Assert.Equal(new PointD(111.0, 101.0), Assert.Single(snapDisabledViewModel.RoadPreviewPoints));
	}

	[Fact]
	public void RoadAreaCreation_SnapsToParentOverlayVertexWhenGridSnapIsEnabled()
	{
		MapViewportViewModel viewModel = CreateChildMapViewport(snapToGrid: true);
		viewModel.SetActiveTool(EditorToolType.RoadArea);

		viewModel.AddRoadAreaPointAtScreenPoint(new PointD(111.0, 101.0));

		AssertPointClose(new PointD(113.0, 100.0), Assert.Single(viewModel.RoadAreaPreviewPoints));
	}

	private static ParentRoadOverlay CreateOverlay()
	{
		return new ParentRoadOverlay(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Parent Road",
			new[]
			{
				new PointD(100.0, 100.0),
				new PointD(220.0, 100.0),
				new PointD(220.0, 140.0),
				new PointD(100.0, 140.0)
			},
			"road.area.secondary",
			"secondary",
			null,
			10.0);
	}

	private static void AssertPointClose(PointD expected, PointD actual)
	{
		Assert.Equal(expected.X, actual.X, precision: 9);
		Assert.Equal(expected.Y, actual.Y, precision: 9);
	}

	private static MapViewportViewModel CreateChildMapViewport(bool snapToGrid)
	{
		MapDocument parentMap = new MapDocument(Guid.NewGuid(), "Parent", MapScaleType.City, new SizeD(40.0, 30.0));
		MapDocument childMap = new MapDocument(
			Guid.NewGuid(),
			"Child",
			MapScaleType.District,
			new SizeD(400.0, 300.0),
			parentMap.Id,
			new GridSettings(isEnabled: true, cellSizeMeters: 50.0, showGrid: false, snapToGrid));
		MapLayer districtLayer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
		MapLayer roadLayer = new MapLayer(Guid.NewGuid(), "Roads", MapLayerType.Streets);
		DistrictShape district = new DistrictShape(
			Guid.NewGuid(),
			"Linked District",
			districtLayer.Id,
			new[]
			{
				new PointD(0.0, 0.0),
				new PointD(40.0, 0.0),
				new PointD(40.0, 30.0),
				new PointD(0.0, 30.0)
			},
			childMapId: childMap.Id);
		RoadArea roadArea = new RoadArea(
			Guid.NewGuid(),
			"Parent Road",
			roadLayer.Id,
			new[]
			{
				new PointD(11.3, 10.0),
				new PointD(18.0, 10.0),
				new PointD(18.0, 13.0),
				new PointD(11.3, 13.0)
			});
		districtLayer.AddObject(district);
		roadLayer.AddObject(roadArea);
		parentMap.AddLayer(districtLayer);
		parentMap.AddLayer(roadLayer);
		parentMap.AddChildMapId(childMap.Id);
		CampaignMapProject project = new CampaignMapProject(Guid.NewGuid(), "Snap Project", parentMap.Id, new[] { parentMap, childMap });
		MapViewportViewModel viewModel = new MapViewportViewModel(childMap, project);
		viewModel.Camera.SetView(0.0, 0.0, 1.0);
		return viewModel;
	}
}
