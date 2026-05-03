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
}
