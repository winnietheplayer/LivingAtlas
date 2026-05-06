using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Selection;
using Xunit;

namespace LivingAtlas.Tests;

public class LayerVisibilityTests
{
    [Fact]
    public void MapLayer_DefaultIsVisibleTrue()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.PointsOfInterest);
        Assert.True(layer.IsVisible);
    }

    [Fact]
    public void MapLayer_SetVisibility_UpdatesValue()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.PointsOfInterest);
        layer.SetVisibility(false);
        Assert.False(layer.IsVisible);
        layer.SetVisibility(true);
        Assert.True(layer.IsVisible);
    }

    [Fact]
    public void HitTester_IgnoresObjectsFromHiddenLayers()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.Region, new SizeD(1000, 1000));
        var layerId = Guid.NewGuid();
        var layer = new MapLayer(layerId, "Test Layer", MapLayerType.PointsOfInterest);
        map.AddLayer(layer);

        var poi = new PointOfInterest(Guid.NewGuid(), "Test POI", layerId, new PointD(100, 100), "icon");
        layer.AddObject(poi);

        // Visible - should hit
        var hit = MapObjectHitTester.HitTest(map, new PointD(100, 100), 5.0);
        Assert.NotNull(hit);
        Assert.Equal(poi.Id, hit.Id);

        // Hidden - should NOT hit
        layer.SetVisibility(false);
        hit = MapObjectHitTester.HitTest(map, new PointD(100, 100), 5.0);
        Assert.Null(hit);
    }

    [Fact]
    public void HitTester_CanHitRoadAreaAndIgnoresHiddenRoadAreaLayer()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.Region, new SizeD(1000, 1000));
        var layer = new MapLayer(Guid.NewGuid(), "Streets", MapLayerType.Streets);
        map.AddLayer(layer);
        RoadArea roadArea = TestData.CreateRoadArea(layer.Id);
        layer.AddObject(roadArea);

        MapObject? hit = MapObjectHitTester.HitTest(map, new PointD(30.0, 30.0), 5.0);

        Assert.NotNull(hit);
        Assert.Equal(roadArea.Id, hit.Id);

        layer.SetVisibility(false);
        hit = MapObjectHitTester.HitTest(map, new PointD(30.0, 30.0), 5.0);

        Assert.Null(hit);
    }
}
