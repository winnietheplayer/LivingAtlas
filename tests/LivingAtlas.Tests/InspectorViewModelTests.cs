using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Tests;

public sealed class InspectorViewModelTests
{
	[Fact]
	public void SetSelection_ExposesOnlyCurrentTypeFields()
	{
		InspectorViewModel inspector = new InspectorViewModel();
		RoadLine road = TestData.CreateRoad(Guid.NewGuid());
		MapLabel label = TestData.CreateLabel(Guid.NewGuid());

		inspector.SetSelection(road);

		Assert.True(inspector.IsRoadLineSelected);
		Assert.False(inspector.IsMapLabelSelected);
		Assert.False(inspector.IsPointOfInterestSelected);
		Assert.False(inspector.IsDistrictShapeSelected);
		Assert.Equal(RoadLine.DefaultRoadKind, inspector.EditableRoadKind);

		inspector.SetSelection(label);

		Assert.True(inspector.IsMapLabelSelected);
		Assert.False(inspector.IsRoadLineSelected);
		Assert.Equal(MapLabel.DefaultLabelKind, inspector.EditableLabelKind);

		inspector.SetSelection(null);

		Assert.False(inspector.HasSelection);
		Assert.Equal("No selection", inspector.ObjectTypeText);
		Assert.Equal("No selection", inspector.SelectionDetails);
	}

	[Fact]
	public void ApplyInspectorChanges_NoChangesDoesNotCreateCommandOrDirtyProject()
	{
		(MainWindowViewModel viewModel, PointOfInterest poi) = CreateMainWindowWithSelectedPoi();

		Assert.Same(poi, viewModel.MapViewport.SelectedObject);

		Assert.False(viewModel.ApplyInspectorChanges());

		Assert.False(viewModel.MapViewport.History.CanUndo);
		Assert.False(viewModel.IsDirty);
		Assert.Equal("No inspector changes", viewModel.StatusBar.Message);
	}

	[Fact]
	public void ApplyInspectorChanges_InvalidEmptyNameIsRejected()
	{
		(MainWindowViewModel viewModel, PointOfInterest poi) = CreateMainWindowWithSelectedPoi();

		viewModel.Inspector.EditableName = " ";

		Assert.False(viewModel.ApplyInspectorChanges());

		Assert.Equal("Gate", poi.Name);
		Assert.False(viewModel.MapViewport.History.CanUndo);
		Assert.False(viewModel.IsDirty);
		Assert.StartsWith("Inspector apply failed:", viewModel.StatusBar.Message);
	}

	private static (MainWindowViewModel ViewModel, PointOfInterest Poi) CreateMainWindowWithSelectedPoi()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.PointsOfInterest);
		PointOfInterest poi = TestData.CreatePointOfInterest(layer.Id);
		layer.AddObject(poi);
		map.AddLayer(layer);
		CampaignMapProject project = TestData.CreateProject(map);
		MainWindowViewModel viewModel = new MainWindowViewModel(project);

		viewModel.MapViewport.Camera.SetView(0.0, 0.0, 1.0);
		viewModel.MapViewport.SelectAtScreenPoint(new PointD(100.0, 200.0), 20.0);

		return (viewModel, poi);
	}
}
