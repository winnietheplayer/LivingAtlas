using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Selection;
using LivingAtlas.Editor.Creation;
using Xunit;

namespace LivingAtlas.Tests;

public class LayerLockTests
{
    [Fact]
    public void MapLayer_DefaultIsLockedFalse()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.PointsOfInterest);
        Assert.False(layer.IsLocked);
    }

    [Fact]
    public void MapLayer_SetLocked_UpdatesValue()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.PointsOfInterest);
        layer.SetLocked(true);
        Assert.True(layer.IsLocked);
        layer.SetLocked(false);
        Assert.False(layer.IsLocked);
    }

    [Fact]
    public void HitTester_IgnoresObjectsFromLockedLayers()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.Region, new SizeD(1000, 1000));
        var layerId = Guid.NewGuid();
        var layer = new MapLayer(layerId, "Test Layer", MapLayerType.PointsOfInterest);
        map.AddLayer(layer);

        var poi = new PointOfInterest(Guid.NewGuid(), "Test POI", layerId, new PointD(100, 100), "icon");
        layer.AddObject(poi);

        // Unlocked - should hit
        var hit = MapObjectHitTester.HitTest(map, new PointD(100, 100), 5.0);
        Assert.NotNull(hit);
        Assert.Equal(poi.Id, hit.Id);

        // Locked - should NOT hit
        layer.SetLocked(true);
        hit = MapObjectHitTester.HitTest(map, new PointD(100, 100), 5.0);
        Assert.Null(hit);
    }

    [Fact]
    public void CreationService_SkipsLockedLayer()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.Region, new SizeD(1000, 1000));
        var lockedLayer = new MapLayer(Guid.NewGuid(), "Locked Layer", MapLayerType.PointsOfInterest, isLocked: true);
        map.AddLayer(lockedLayer);

        var command = MapObjectCreationService.CreatePointOfInterestCommand(map, new PointD(0, 0));
        
        Assert.NotEqual(lockedLayer.Id, command.Layer.Id);
        Assert.True(command.CreatesLayer);
        Assert.False(command.Layer.IsLocked);
    }
}
