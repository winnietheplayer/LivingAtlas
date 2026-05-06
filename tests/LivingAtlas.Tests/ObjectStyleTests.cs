using System;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Commands;
using LivingAtlas.Editor.Creation;
using Xunit;

namespace LivingAtlas.Tests;

public class ObjectStyleTests
{
    private readonly MapDocument _map;

    public ObjectStyleTests()
    {
        _map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(1000, 1000));
    }

    [Fact]
    public void StylePresetList_ReturnsPresetsForObjectType()
    {
        var districtPresets = MapObjectStylePresets.GetPresetsForType(MapObjectType.DistrictShape);
        Assert.Contains("district.industrial", districtPresets);
        Assert.DoesNotContain("road.primary", districtPresets);

        var roadPresets = MapObjectStylePresets.GetPresetsForType(MapObjectType.RoadLine);
        Assert.Contains("road.primary", roadPresets);

        var roadAreaPresets = MapObjectStylePresets.GetPresetsForType(MapObjectType.RoadArea);
        Assert.Contains("road.area.secondary", roadAreaPresets);
        Assert.DoesNotContain("road.primary", roadAreaPresets);
    }

    [Fact]
    public void StyleUpdateCommand_ChangesStyleKey()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Layer", MapLayerType.Districts);
        _map.AddLayer(layer);
        var original = new PointOfInterest(Guid.NewGuid(), "POI", layer.Id, new PointD(0, 0), "icon", styleKey: "poi.default");
        layer.AddObject(original);

        var command = new UpdateMapObjectPropertiesCommand(_map, original.Id, original.Name, null, "poi.danger");
        command.Execute();

        Assert.Equal("poi.danger", original.StyleKey);
    }

    [Fact]
    public void UndoRedo_RestoresPreviousStyleKey()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Layer", MapLayerType.Districts);
        _map.AddLayer(layer);
        var original = new PointOfInterest(Guid.NewGuid(), "POI", layer.Id, new PointD(0, 0), "icon", styleKey: "poi.default");
        layer.AddObject(original);

        var command = new UpdateMapObjectPropertiesCommand(_map, original.Id, original.Name, null, "poi.danger");
        
        command.Execute();
        Assert.Equal("poi.danger", original.StyleKey);

        command.Undo();
        Assert.Equal("poi.default", original.StyleKey);

        command.Execute();
        Assert.Equal("poi.danger", original.StyleKey);
    }
}
