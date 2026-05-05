using System;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Editor.Creation;
using Xunit;

namespace LivingAtlas.Tests;

public class ActiveTargetLayerTests
{
    private static MapDocument CreateEmptyMap()
    {
        return new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(1000, 1000));
    }

    [Fact]
    public void CreatePoi_WithNoLayers_CreatesNewVisibleUnlockedLayer()
    {
        var map = CreateEmptyMap();
        var command = MapObjectCreationService.CreatePointOfInterestCommand(map, new PointD(10, 10));

        Assert.True(command.CreatesLayer);
        Assert.Equal(MapLayerType.PointsOfInterest, command.Layer.LayerType);
        Assert.True(command.Layer.IsVisible);
        Assert.False(command.Layer.IsLocked);
    }

    [Fact]
    public void CreatePoi_WithActiveTargetLayer_UsesTargetLayer()
    {
        var map = CreateEmptyMap();
        var targetLayer = new MapLayer(Guid.NewGuid(), "Custom POI Layer", MapLayerType.PointsOfInterest);
        map.AddLayer(targetLayer);

        var command = MapObjectCreationService.CreatePointOfInterestCommand(map, new PointD(10, 10), activeTargetLayerId: targetLayer.Id);

        Assert.False(command.CreatesLayer);
        Assert.Equal(targetLayer.Id, command.Layer.Id);
    }

    [Fact]
    public void CreatePoi_WithLockedActiveTargetLayer_FallsBackToNewOrOtherLayer()
    {
        var map = CreateEmptyMap();
        var targetLayer = new MapLayer(Guid.NewGuid(), "Locked POI Layer", MapLayerType.PointsOfInterest);
        targetLayer.SetLocked(true);
        map.AddLayer(targetLayer);

        var command = MapObjectCreationService.CreatePointOfInterestCommand(map, new PointD(10, 10), activeTargetLayerId: targetLayer.Id);

        Assert.True(command.CreatesLayer);
        Assert.NotEqual(targetLayer.Id, command.Layer.Id);
        Assert.False(command.Layer.IsLocked);
    }

    [Fact]
    public void CreatePoi_WithHiddenActiveTargetLayer_FallsBackToNewOrOtherLayer()
    {
        var map = CreateEmptyMap();
        var targetLayer = new MapLayer(Guid.NewGuid(), "Hidden POI Layer", MapLayerType.PointsOfInterest);
        targetLayer.SetVisibility(false);
        map.AddLayer(targetLayer);

        var command = MapObjectCreationService.CreatePointOfInterestCommand(map, new PointD(10, 10), activeTargetLayerId: targetLayer.Id);

        Assert.True(command.CreatesLayer);
        Assert.NotEqual(targetLayer.Id, command.Layer.Id);
        Assert.True(command.Layer.IsVisible);
    }

    [Fact]
    public void CreatePoi_WithWrongTypeActiveTargetLayer_FallsBackToCorrectTypeLayer()
    {
        var map = CreateEmptyMap();
        var targetLayer = new MapLayer(Guid.NewGuid(), "Streets Layer", MapLayerType.Streets);
        var fallbackLayer = new MapLayer(Guid.NewGuid(), "Valid POI Layer", MapLayerType.PointsOfInterest);
        map.AddLayer(targetLayer);
        map.AddLayer(fallbackLayer);

        var command = MapObjectCreationService.CreatePointOfInterestCommand(map, new PointD(10, 10), activeTargetLayerId: targetLayer.Id);

        Assert.False(command.CreatesLayer);
        Assert.Equal(fallbackLayer.Id, command.Layer.Id);
    }
}
