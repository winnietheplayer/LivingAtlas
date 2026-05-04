using System;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using Xunit;

namespace LivingAtlas.Tests;

public class LayerReorderTests
{
    [Fact]
    public void MoveLayerUp_ChangesOrder()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(100, 100));
        var layer1 = new MapLayer(Guid.NewGuid(), "Layer 1", MapLayerType.Districts);
        var layer2 = new MapLayer(Guid.NewGuid(), "Layer 2", MapLayerType.Streets);
        map.AddLayer(layer1);
        map.AddLayer(layer2);

        // layer1 is at index 0, layer2 is at index 1
        Assert.Equal(layer1.Id, map.Layers[0].Id);
        Assert.Equal(layer2.Id, map.Layers[1].Id);

        // Move layer1 up (to index 1)
        bool success = map.MoveLayerUp(layer1.Id);

        Assert.True(success);
        Assert.Equal(layer2.Id, map.Layers[0].Id);
        Assert.Equal(layer1.Id, map.Layers[1].Id);
    }

    [Fact]
    public void MoveLayerDown_ChangesOrder()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(100, 100));
        var layer1 = new MapLayer(Guid.NewGuid(), "Layer 1", MapLayerType.Districts);
        var layer2 = new MapLayer(Guid.NewGuid(), "Layer 2", MapLayerType.Streets);
        map.AddLayer(layer1);
        map.AddLayer(layer2);

        // Move layer2 down (to index 0)
        bool success = map.MoveLayerDown(layer2.Id);

        Assert.True(success);
        Assert.Equal(layer2.Id, map.Layers[0].Id);
        Assert.Equal(layer1.Id, map.Layers[1].Id);
    }

    [Fact]
    public void MoveLayerUp_AlreadyAtTop_DoesNothing()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(100, 100));
        var layer1 = new MapLayer(Guid.NewGuid(), "Layer 1", MapLayerType.Districts);
        map.AddLayer(layer1);

        bool success = map.MoveLayerUp(layer1.Id);

        Assert.False(success);
        Assert.Single(map.Layers);
        Assert.Equal(layer1.Id, map.Layers[0].Id);
    }

    [Fact]
    public void MoveLayerDown_AlreadyAtBottom_DoesNothing()
    {
        var map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(100, 100));
        var layer1 = new MapLayer(Guid.NewGuid(), "Layer 1", MapLayerType.Districts);
        map.AddLayer(layer1);

        bool success = map.MoveLayerDown(layer1.Id);

        Assert.False(success);
        Assert.Single(map.Layers);
        Assert.Equal(layer1.Id, map.Layers[0].Id);
    }
}
