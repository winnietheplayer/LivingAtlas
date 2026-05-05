using System;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using Xunit;

namespace LivingAtlas.Tests;

public class LayerCreateDeleteTests
{
    private static MapDocument CreateEmptyMap()
    {
        return new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(1000, 1000));
    }

    [Fact]
    public void MapDocument_AddLayer_AddsLayerToList()
    {
        var map = CreateEmptyMap();
        var layer = new MapLayer(Guid.NewGuid(), "New Layer", MapLayerType.Labels);

        map.AddLayer(layer);

        Assert.Single(map.Layers);
        Assert.Equal(layer, map.Layers[0]);
    }

    [Fact]
    public void MapDocument_AddLayer_ThrowsIfLayerAlreadyExists()
    {
        var map = CreateEmptyMap();
        var layer = new MapLayer(Guid.NewGuid(), "New Layer", MapLayerType.Labels);

        map.AddLayer(layer);

        Assert.Throws<InvalidOperationException>(() => map.AddLayer(layer));
    }

    [Fact]
    public void MapDocument_RemoveLayer_RemovesLayerAndReturnsIt()
    {
        var map = CreateEmptyMap();
        var layer = new MapLayer(Guid.NewGuid(), "New Layer", MapLayerType.Labels);
        map.AddLayer(layer);

        var removedLayer = map.RemoveLayer(layer.Id);

        Assert.Empty(map.Layers);
        Assert.Equal(layer, removedLayer);
    }

    [Fact]
    public void MapDocument_RemoveLayer_ReturnsNullForUnknownId()
    {
        var map = CreateEmptyMap();
        var layer = new MapLayer(Guid.NewGuid(), "New Layer", MapLayerType.Labels);
        map.AddLayer(layer);

        var removedLayer = map.RemoveLayer(Guid.NewGuid());

        Assert.Single(map.Layers);
        Assert.Null(removedLayer);
    }
}
