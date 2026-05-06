using LivingAtlas.Assets;
using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Tools;

namespace LivingAtlas.Tests;

public sealed class RulerToolTests
{
	[Fact]
	public void RulerMeasurement_CalculatesDistanceAndDeltas()
	{
		RulerMeasurement measurement = new RulerMeasurement(new PointD(10.0, 20.0), new PointD(13.0, 24.0));

		Assert.Equal(3.0, measurement.DeltaX);
		Assert.Equal(4.0, measurement.DeltaY);
		Assert.Equal(5.0, measurement.DistanceLocalUnits);
		Assert.Equal(50.0, measurement.GetDistanceFeet(10.0));
		Assert.Equal(10.0, measurement.GetBattleSquares(10.0));
		Assert.Contains("Distance: 50.0 ft", measurement.FormatStatus(10.0));
	}

	[Fact]
	public void RulerTool_SnapsStartAndEnd_WhenGridSnapEnabled()
	{
		MapViewportViewModel viewModel = CreateViewport(snapToGrid: true);
		viewModel.SetActiveTool(EditorToolType.Ruler);

		viewModel.AddRulerPointAtScreenPoint(new PointD(12.0, 16.0));
		viewModel.AddRulerPointAtScreenPoint(new PointD(26.0, 24.0));

		Assert.Equal(new PointD(10.0, 20.0), viewModel.RulerStartPoint);
		Assert.Equal(new PointD(30.0, 20.0), viewModel.RulerEndPoint);
		RulerMeasurement? measurement = viewModel.CurrentRulerMeasurement;
		Assert.NotNull(measurement);
		Assert.Equal(20.0, measurement.DistanceLocalUnits);
		Assert.Equal(2000.0, measurement.GetDistanceFeet(viewModel.Map.FeetPerUnit));
	}

	[Fact]
	public void RulerTool_UsesRawCoordinates_WhenGridSnapDisabled()
	{
		MapViewportViewModel viewModel = CreateViewport(snapToGrid: false);
		viewModel.SetActiveTool(EditorToolType.Ruler);

		viewModel.AddRulerPointAtScreenPoint(new PointD(12.0, 16.0));
		viewModel.AddRulerPointAtScreenPoint(new PointD(26.0, 24.0));

		Assert.Equal(new PointD(12.0, 16.0), viewModel.RulerStartPoint);
		Assert.Equal(new PointD(26.0, 24.0), viewModel.RulerEndPoint);
	}

	[Fact]
	public void RulerTool_ThirdClickStartsNewMeasurement()
	{
		MapViewportViewModel viewModel = CreateViewport(snapToGrid: true);
		viewModel.SetActiveTool(EditorToolType.Ruler);

		viewModel.AddRulerPointAtScreenPoint(new PointD(12.0, 16.0));
		viewModel.AddRulerPointAtScreenPoint(new PointD(26.0, 24.0));
		viewModel.AddRulerPointAtScreenPoint(new PointD(44.0, 45.0));

		Assert.Equal(new PointD(40.0, 40.0), viewModel.RulerStartPoint);
		Assert.Null(viewModel.RulerEndPoint);
		Assert.Null(viewModel.CurrentRulerMeasurement);
	}

	[Fact]
	public void RulerTool_EscapeClearRemovesOverlay()
	{
		MapViewportViewModel viewModel = CreateViewport(snapToGrid: true);
		viewModel.SetActiveTool(EditorToolType.Ruler);
		viewModel.AddRulerPointAtScreenPoint(new PointD(12.0, 16.0));
		viewModel.AddRulerPointAtScreenPoint(new PointD(26.0, 24.0));

		Assert.True(viewModel.ClearRulerMeasurement());

		Assert.False(viewModel.HasRulerOverlay);
		Assert.Null(viewModel.RulerStartPoint);
		Assert.Null(viewModel.RulerEndPoint);
		Assert.Null(viewModel.CurrentRulerMeasurement);
	}

	[Fact]
	public void RulerTool_DoesNotMutateProjectOrCreateHistoryCommand()
	{
		MapViewportViewModel viewModel = CreateViewport(snapToGrid: true);
		bool projectMutated = false;
		viewModel.ProjectMutated += (_, _) => projectMutated = true;
		viewModel.SetActiveTool(EditorToolType.Ruler);

		viewModel.AddRulerPointAtScreenPoint(new PointD(12.0, 16.0));
		viewModel.UpdatePointerPosition(new PointD(26.0, 24.0));
		viewModel.AddRulerPointAtScreenPoint(new PointD(26.0, 24.0));
		viewModel.ClearRulerMeasurement();

		Assert.False(projectMutated);
		Assert.False(viewModel.History.CanUndo);
	}

	[Fact]
	public void MainWindowViewModel_OpenMapClearsRulerState()
	{
		MapDocument rootMap = CreateMap("Root", parentMapId: null, snapToGrid: true);
		MapDocument childMap = CreateMap("Child", parentMapId: rootMap.Id, snapToGrid: true);
		rootMap.AddChildMapId(childMap.Id);
		CampaignMapProject project = new CampaignMapProject(Guid.NewGuid(), "Ruler Project", rootMap.Id, new[] { rootMap, childMap });
		MainWindowViewModel viewModel = new MainWindowViewModel(project, TextureAssetCatalog.Empty);
		viewModel.MapViewport.SetActiveTool(EditorToolType.Ruler);
		viewModel.MapViewport.AddRulerPointAtScreenPoint(new PointD(12.0, 16.0));
		viewModel.MapViewport.AddRulerPointAtScreenPoint(new PointD(26.0, 24.0));

		Assert.True(viewModel.OpenMap(childMap.Id));
		Assert.False(viewModel.MapViewport.HasRulerOverlay);
		Assert.False(viewModel.IsDirty);

		Assert.True(viewModel.OpenMap(rootMap.Id));
		Assert.False(viewModel.MapViewport.HasRulerOverlay);
		Assert.False(viewModel.IsDirty);
	}

	private static MapViewportViewModel CreateViewport(bool snapToGrid)
	{
		MapDocument map = CreateMap("Map", parentMapId: null, snapToGrid);
		MapViewportViewModel viewModel = new MapViewportViewModel(map);
		viewModel.Camera.SetView(0.0, 0.0, 1.0);
		return viewModel;
	}

	private static MapDocument CreateMap(string name, Guid? parentMapId, bool snapToGrid)
	{
		return new MapDocument(
			Guid.NewGuid(),
			name,
			MapScaleType.City,
			new SizeD(100.0, 100.0),
			parentMapId,
			new GridSettings(isEnabled: true, cellSizeMeters: 10.0, showGrid: false, snapToGrid));
	}
}
