using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Tests;

public sealed class GeometryEditingViewModelTests
{
	[Fact]
	public void EndMoveSelectedVertex_CommitsOneCommandAndKeepsSelection()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);
		MapViewportViewModel viewModel = new MapViewportViewModel(map);

		map.AddLayer(layer);
		layer.AddObject(road);
		viewModel.SelectAtScreenPoint(new PointD(30.0, 40.0), 10.0);

		Assert.True(viewModel.BeginMoveSelectedVertex(1, new PointD(30.0, 40.0)));
		viewModel.MoveSelectedVertexToScreenPoint(new PointD(80.0, 90.0));

		Assert.True(viewModel.EndMoveSelectedVertex());

		Assert.Same(road, viewModel.SelectedObject);
		Assert.Equal(1, viewModel.SelectedVertexIndex);
		Assert.Equal(new PointD(80.0, 90.0), road.Points[1]);
		Assert.True(viewModel.History.CanUndo);

		Assert.True(viewModel.Undo());
		Assert.Equal(new PointD(30.0, 40.0), road.Points[1]);
		Assert.False(viewModel.History.CanUndo);

		Assert.True(viewModel.Redo());
		Assert.Equal(new PointD(80.0, 90.0), road.Points[1]);
	}

	[Fact]
	public void EndMoveSelectedVertex_DoesNotCreateCommandWhenPointDidNotChange()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);
		MapViewportViewModel viewModel = new MapViewportViewModel(map);

		map.AddLayer(layer);
		layer.AddObject(road);
		viewModel.SelectAtScreenPoint(new PointD(30.0, 40.0), 10.0);

		Assert.True(viewModel.BeginMoveSelectedVertex(1, new PointD(30.0, 40.0)));

		Assert.False(viewModel.EndMoveSelectedVertex());

		Assert.Equal(new PointD(30.0, 40.0), road.Points[1]);
		Assert.False(viewModel.History.CanUndo);
	}
}
