using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Commands;

namespace LivingAtlas.Tests;

public sealed class GeometryEditingCommandTests
{
	[Fact]
	public void MoveMapObjectVertexCommand_MovesOneRoadVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(road);

		MoveMapObjectVertexCommand command = new MoveMapObjectVertexCommand(
			map,
			road.Id,
			1,
			road.Points[1],
			new PointD(75.0, 90.0));

		command.Execute();

		Assert.Equal(new PointD(10.0, 20.0), road.Points[0]);
		Assert.Equal(new PointD(75.0, 90.0), road.Points[1]);
	}

	[Fact]
	public void MoveMapObjectVertexCommand_UndoRestoresRoadVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(road);

		history.Execute(new MoveMapObjectVertexCommand(
			map,
			road.Id,
			1,
			road.Points[1],
			new PointD(75.0, 90.0)));

		history.Undo();

		Assert.Equal(new PointD(30.0, 40.0), road.Points[1]);
	}

	[Fact]
	public void MoveMapObjectVertexCommand_RedoReappliesRoadVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(road);

		history.Execute(new MoveMapObjectVertexCommand(
			map,
			road.Id,
			1,
			road.Points[1],
			new PointD(75.0, 90.0)));
		history.Undo();
		history.Redo();

		Assert.Equal(new PointD(75.0, 90.0), road.Points[1]);
	}

	[Fact]
	public void MoveMapObjectVertexCommand_MovesOneDistrictVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
		DistrictShape district = TestData.CreateDistrict(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(district);

		MoveMapObjectVertexCommand command = new MoveMapObjectVertexCommand(
			map,
			district.Id,
			2,
			district.PolygonPoints[2],
			new PointD(600.0, 450.0));

		command.Execute();

		Assert.Equal(new PointD(500.0, 120.0), district.PolygonPoints[1]);
		Assert.Equal(new PointD(600.0, 450.0), district.PolygonPoints[2]);
		Assert.Equal(new PointD(90.0, 400.0), district.PolygonPoints[3]);
	}

	[Fact]
	public void MoveMapObjectVertexCommand_UndoRedoWorksForDistrictVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
		DistrictShape district = TestData.CreateDistrict(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(district);

		history.Execute(new MoveMapObjectVertexCommand(
			map,
			district.Id,
			2,
			district.PolygonPoints[2],
			new PointD(600.0, 450.0)));

		history.Undo();

		Assert.Equal(new PointD(520.0, 420.0), district.PolygonPoints[2]);

		history.Redo();

		Assert.Equal(new PointD(600.0, 450.0), district.PolygonPoints[2]);
	}

	[Fact]
	public void MoveMapObjectVertexCommand_UndoRedoWorksForRoadAreaVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadArea roadArea = TestData.CreateRoadArea(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(roadArea);

		history.Execute(new MoveMapObjectVertexCommand(
			map,
			roadArea.Id,
			2,
			roadArea.PolygonPoints[2],
			new PointD(95.0, 75.0)));

		Assert.Equal(new PointD(95.0, 75.0), roadArea.PolygonPoints[2]);

		history.Undo();
		Assert.Equal(new PointD(80.0, 50.0), roadArea.PolygonPoints[2]);

		history.Redo();
		Assert.Equal(new PointD(95.0, 75.0), roadArea.PolygonPoints[2]);
	}

	[Fact]
	public void MoveMapObjectVertexCommand_InvalidVertexIndexThrows()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(road);

		MoveMapObjectVertexCommand command = new MoveMapObjectVertexCommand(
			map,
			road.Id,
			4,
			new PointD(0.0, 0.0),
			new PointD(10.0, 10.0));

		Assert.Throws<ArgumentOutOfRangeException>(() => command.Execute());
	}

	[Fact]
	public void AddMapObjectVertexCommand_InsertsRoadVertexAtExpectedIndex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(road);

		new AddMapObjectVertexCommand(map, road.Id, 1, new PointD(20.0, 30.0)).Execute();

		Assert.Collection(
			road.Points,
			point => Assert.Equal(new PointD(10.0, 20.0), point),
			point => Assert.Equal(new PointD(20.0, 30.0), point),
			point => Assert.Equal(new PointD(30.0, 40.0), point));
	}

	[Fact]
	public void AddMapObjectVertexCommand_UndoRemovesAddedRoadVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(road);

		history.Execute(new AddMapObjectVertexCommand(map, road.Id, 1, new PointD(20.0, 30.0)));
		history.Undo();

		Assert.Collection(
			road.Points,
			point => Assert.Equal(new PointD(10.0, 20.0), point),
			point => Assert.Equal(new PointD(30.0, 40.0), point));
	}

	[Fact]
	public void AddMapObjectVertexCommand_RedoRestoresRoadVertex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(road);

		history.Execute(new AddMapObjectVertexCommand(map, road.Id, 1, new PointD(20.0, 30.0)));
		history.Undo();
		history.Redo();

		Assert.Equal(new PointD(20.0, 30.0), road.Points[1]);
	}

	[Fact]
	public void RemoveMapObjectVertexCommand_RemovesRoadVertexAtExpectedIndex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(road);
		new AddMapObjectVertexCommand(map, road.Id, 1, new PointD(20.0, 30.0)).Execute();

		new RemoveMapObjectVertexCommand(map, road.Id, 1).Execute();

		Assert.Collection(
			road.Points,
			point => Assert.Equal(new PointD(10.0, 20.0), point),
			point => Assert.Equal(new PointD(30.0, 40.0), point));
	}

	[Fact]
	public void RemoveMapObjectVertexCommand_CannotRemoveRoadBelowTwoPoints()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadLine road = TestData.CreateRoad(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(road);

		RemoveMapObjectVertexCommand command = new RemoveMapObjectVertexCommand(map, road.Id, 0);

		Assert.Throws<InvalidOperationException>(() => command.Execute());
	}

	[Fact]
	public void AddMapObjectVertexCommand_InsertsDistrictVertexAtExpectedIndex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
		DistrictShape district = TestData.CreateDistrict(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(district);

		new AddMapObjectVertexCommand(map, district.Id, district.PolygonPoints.Count, new PointD(95.0, 250.0)).Execute();

		Assert.Equal(new PointD(95.0, 250.0), district.PolygonPoints[^1]);
	}

	[Fact]
	public void RemoveMapObjectVertexCommand_RemovesDistrictVertexAtExpectedIndex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
		DistrictShape district = TestData.CreateDistrict(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(district);

		new RemoveMapObjectVertexCommand(map, district.Id, 1).Execute();

		Assert.Collection(
			district.PolygonPoints,
			point => Assert.Equal(new PointD(100.0, 100.0), point),
			point => Assert.Equal(new PointD(520.0, 420.0), point),
			point => Assert.Equal(new PointD(90.0, 400.0), point));
	}

	[Fact]
	public void RemoveMapObjectVertexCommand_CannotRemoveDistrictBelowThreePoints()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
		DistrictShape district = TestData.CreateDistrict(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(district);
		new RemoveMapObjectVertexCommand(map, district.Id, 1).Execute();

		RemoveMapObjectVertexCommand command = new RemoveMapObjectVertexCommand(map, district.Id, 1);

		Assert.Throws<InvalidOperationException>(() => command.Execute());
	}

	[Fact]
	public void AddRemoveMapObjectVertexCommands_UndoRedoWorksForDistrict()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
		DistrictShape district = TestData.CreateDistrict(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(district);

		history.Execute(new AddMapObjectVertexCommand(map, district.Id, 1, new PointD(220.0, 110.0)));
		Assert.Equal(5, district.PolygonPoints.Count);

		history.Undo();
		Assert.Equal(4, district.PolygonPoints.Count);

		history.Redo();
		Assert.Equal(new PointD(220.0, 110.0), district.PolygonPoints[1]);

		history.Execute(new RemoveMapObjectVertexCommand(map, district.Id, 1));
		Assert.Equal(4, district.PolygonPoints.Count);

		history.Undo();
		Assert.Equal(new PointD(220.0, 110.0), district.PolygonPoints[1]);

		history.Redo();
		Assert.Equal(new PointD(500.0, 120.0), district.PolygonPoints[1]);
	}

	[Fact]
	public void AddMapObjectVertexCommand_InsertsRoadAreaVertexAtExpectedIndex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadArea roadArea = TestData.CreateRoadArea(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(roadArea);

		new AddMapObjectVertexCommand(map, roadArea.Id, 2, new PointD(85.0, 35.0)).Execute();

		Assert.Equal(new PointD(85.0, 35.0), roadArea.PolygonPoints[2]);
		Assert.Equal(5, roadArea.PolygonPoints.Count);
	}

	[Fact]
	public void RemoveMapObjectVertexCommand_RemovesRoadAreaVertexAtExpectedIndex()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadArea roadArea = TestData.CreateRoadArea(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(roadArea);

		new RemoveMapObjectVertexCommand(map, roadArea.Id, 1).Execute();

		Assert.Collection(
			roadArea.PolygonPoints,
			point => Assert.Equal(new PointD(10.0, 20.0), point),
			point => Assert.Equal(new PointD(80.0, 50.0), point),
			point => Assert.Equal(new PointD(10.0, 50.0), point));
	}

	[Fact]
	public void RemoveMapObjectVertexCommand_CannotRemoveRoadAreaBelowThreePoints()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadArea roadArea = TestData.CreateRoadArea(layer.Id);

		map.AddLayer(layer);
		layer.AddObject(roadArea);
		new RemoveMapObjectVertexCommand(map, roadArea.Id, 1).Execute();

		RemoveMapObjectVertexCommand command = new RemoveMapObjectVertexCommand(map, roadArea.Id, 1);

		Assert.Throws<InvalidOperationException>(() => command.Execute());
	}

	[Fact]
	public void AddRemoveMapObjectVertexCommands_UndoRedoWorksForRoadArea()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadArea roadArea = TestData.CreateRoadArea(layer.Id);
		HistoryService history = new HistoryService();

		map.AddLayer(layer);
		layer.AddObject(roadArea);

		history.Execute(new AddMapObjectVertexCommand(map, roadArea.Id, 1, new PointD(45.0, 20.0)));
		Assert.Equal(5, roadArea.PolygonPoints.Count);

		history.Undo();
		Assert.Equal(4, roadArea.PolygonPoints.Count);

		history.Redo();
		Assert.Equal(new PointD(45.0, 20.0), roadArea.PolygonPoints[1]);

		history.Execute(new RemoveMapObjectVertexCommand(map, roadArea.Id, 1));
		Assert.Equal(4, roadArea.PolygonPoints.Count);

		history.Undo();
		Assert.Equal(new PointD(45.0, 20.0), roadArea.PolygonPoints[1]);

		history.Redo();
		Assert.Equal(new PointD(80.0, 20.0), roadArea.PolygonPoints[1]);
	}
}
