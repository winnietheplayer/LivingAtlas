using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Commands;

namespace LivingAtlas.Tests;

public sealed class EditorCommandTests
{
    [Fact]
    public void AddMapObjectCommand_AddsObjectAndUndoRedoRoundTrips()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer();
        PointOfInterest poi = TestData.CreatePointOfInterest(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        history.Execute(new AddMapObjectCommand(map, layer, poi));

        Assert.Same(poi, Assert.Single(layer.Objects));

        Assert.NotNull(history.Undo());
        Assert.Empty(layer.Objects);

        history.Redo();

        Assert.Same(poi, Assert.Single(layer.Objects));
    }

    [Fact]
    public void DeleteMapObjectCommand_RemovesObjectAndUndoRestoresSameIndex()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer();
        PointOfInterest first = TestData.CreatePointOfInterest(layer.Id, name: "First");
        PointOfInterest second = TestData.CreatePointOfInterest(layer.Id, name: "Second");
        DeleteMapObjectCommand command;

        map.AddLayer(layer);
        layer.AddObject(first);
        layer.AddObject(second);

        command = new DeleteMapObjectCommand(map, first.Id);
        command.Execute();

        Assert.Same(second, Assert.Single(layer.Objects));

        command.Undo();

        Assert.Collection(
            layer.Objects,
            item => Assert.Same(first, item),
            item => Assert.Same(second, item));
    }

    [Fact]
    public void MoveMapObjectCommand_MovesPointOfInterestAndUndoRestoresPosition()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer();
        PointOfInterest poi = TestData.CreatePointOfInterest(layer.Id);
        MoveMapObjectCommand command;

        map.AddLayer(layer);
        layer.AddObject(poi);

        command = new MoveMapObjectCommand(map, poi.Id, new PointD(15.0, -25.0));
        command.Execute();

        Assert.Equal(new PointD(115.0, 175.0), poi.Position);

        command.Undo();

        Assert.Equal(new PointD(100.0, 200.0), poi.Position);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesNameAndTextWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Labels);
        MapLabel label = TestData.CreateLabel(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(label);

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            label.Id,
            "Updated Label",
            "Updated text"));

        Assert.Equal("Updated Label", label.Name);
        Assert.Equal("Updated text", label.Text);

        history.Undo();

        Assert.Equal("City Label", label.Name);
        Assert.Equal("Original text", label.Text);

        history.Redo();

        Assert.Equal("Updated Label", label.Name);
        Assert.Equal("Updated text", label.Text);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesDescriptionWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer();
        PointOfInterest poi = TestData.CreatePointOfInterest(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(poi);
        poi.SetDescription("Old notes");

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            poi.Id,
            poi.Name,
            newDescription: "Line one\r\nLine two  "));

        Assert.Equal("Line one\r\nLine two  ", poi.Description);

        history.Undo();

        Assert.Equal("Old notes", poi.Description);

        history.Redo();

        Assert.Equal("Line one\r\nLine two  ", poi.Description);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesPointOfInterestCategoryWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer();
        PointOfInterest poi = TestData.CreatePointOfInterest(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(poi);
        poi.SetCategory("gate");

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            poi.Id,
            poi.Name,
            newCategory: "landmark"));

        Assert.Equal("landmark", poi.Category);

        history.Undo();
        Assert.Equal("gate", poi.Category);

        history.Redo();
        Assert.Equal("landmark", poi.Category);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesRoadKindWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
        RoadLine road = TestData.CreateRoad(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(road);

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            road.Id,
            road.Name,
            newRoadKind: "primary"));

        Assert.Equal("primary", road.RoadKind);

        history.Undo();
        Assert.Equal(RoadLine.DefaultRoadKind, road.RoadKind);

        history.Redo();
        Assert.Equal("primary", road.RoadKind);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesRoadAreaRoadKindAndTextureFillWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
        RoadArea roadArea = TestData.CreateRoadArea(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(roadArea);

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            roadArea.Id,
            roadArea.Name,
            newRoadKind: "primary",
            updateTextureFill: true,
            newFillTextureAssetId: "road.cobble.01",
            newTextureTileSizeMeters: 6.0));

        Assert.Equal("primary", roadArea.RoadKind);
        Assert.Equal("road.cobble.01", roadArea.FillTextureAssetId);
        Assert.Equal(6.0, roadArea.TextureTileSizeMeters);

        history.Undo();
        Assert.Equal(RoadArea.DefaultRoadKind, roadArea.RoadKind);
        Assert.Null(roadArea.FillTextureAssetId);
        Assert.Equal(RoadArea.DefaultTextureTileSizeMeters, roadArea.TextureTileSizeMeters);

        history.Redo();
        Assert.Equal("primary", roadArea.RoadKind);
        Assert.Equal("road.cobble.01", roadArea.FillTextureAssetId);
        Assert.Equal(6.0, roadArea.TextureTileSizeMeters);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesDistrictKindWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
        DistrictShape district = TestData.CreateDistrict(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(district);

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            district.Id,
            district.Name,
            newDistrictKind: "market"));

        Assert.Equal("market", district.DistrictKind);

        history.Undo();
        Assert.Equal(DistrictShape.DefaultDistrictKind, district.DistrictKind);

        history.Redo();
        Assert.Equal("market", district.DistrictKind);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesDistrictTextureFillWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
        DistrictShape district = TestData.CreateDistrict(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(district);

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            district.Id,
            district.Name,
            updateTextureFill: true,
            newFillTextureAssetId: "ground.dirt.01",
            newTextureTileSizeMeters: 15.0));

        Assert.Equal("ground.dirt.01", district.FillTextureAssetId);
        Assert.Equal(15.0, district.TextureTileSizeMeters);

        history.Undo();
        Assert.Null(district.FillTextureAssetId);
        Assert.Equal(DistrictShape.DefaultTextureTileSizeMeters, district.TextureTileSizeMeters);

        history.Redo();
        Assert.Equal("ground.dirt.01", district.FillTextureAssetId);
        Assert.Equal(15.0, district.TextureTileSizeMeters);
    }

    [Fact]
    public void UpdateMapObjectPropertiesCommand_UpdatesLabelKindWithUndoRedo()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Labels);
        MapLabel label = TestData.CreateLabel(layer.Id);
        HistoryService history = new HistoryService();

        map.AddLayer(layer);
        layer.AddObject(label);

        history.Execute(new UpdateMapObjectPropertiesCommand(
            map,
            label.Id,
            label.Name,
            newLabelKind: "city"));

        Assert.Equal("city", label.LabelKind);

        history.Undo();
        Assert.Equal(MapLabel.DefaultLabelKind, label.LabelKind);

        history.Redo();
        Assert.Equal("city", label.LabelKind);
    }
}
