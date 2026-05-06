using System;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
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

    [Fact]
    public void CreateRoadArea_WithNoLayers_CreatesNewStreetsLayer()
    {
        var map = CreateEmptyMap();

        var command = MapObjectCreationService.CreateRoadAreaCommand(map, RoadAreaPoints());

        Assert.True(command.CreatesLayer);
        Assert.Equal(MapLayerType.Streets, command.Layer.LayerType);
        Assert.Equal("Roads", command.Layer.Name);
        RoadArea roadArea = Assert.IsType<RoadArea>(command.MapObject);
        Assert.Equal("road.area.secondary", roadArea.StyleKey);
    }

    [Fact]
    public void CreateRoadArea_WithActiveStreetsLayer_UsesTargetLayer()
    {
        var map = CreateEmptyMap();
        var targetLayer = new MapLayer(Guid.NewGuid(), "Main Streets", MapLayerType.Streets);
        map.AddLayer(targetLayer);

        var command = MapObjectCreationService.CreateRoadAreaCommand(map, RoadAreaPoints(), activeTargetLayerId: targetLayer.Id);

        Assert.False(command.CreatesLayer);
        Assert.Equal(targetLayer.Id, command.Layer.Id);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CreateRoadArea_WithUnavailableActiveTargetLayer_IgnoresTarget(bool hidden, bool locked)
    {
        var map = CreateEmptyMap();
        var targetLayer = new MapLayer(Guid.NewGuid(), "Unavailable Streets", MapLayerType.Streets);
        if (hidden)
        {
            targetLayer.SetVisibility(false);
        }
        if (locked)
        {
            targetLayer.SetLocked(true);
        }
        var fallbackLayer = new MapLayer(Guid.NewGuid(), "Fallback Streets", MapLayerType.Streets);
        map.AddLayer(targetLayer);
        map.AddLayer(fallbackLayer);

        var command = MapObjectCreationService.CreateRoadAreaCommand(map, RoadAreaPoints(), activeTargetLayerId: targetLayer.Id);

        Assert.False(command.CreatesLayer);
        Assert.Equal(fallbackLayer.Id, command.Layer.Id);
    }

    [Fact]
    public void CreateRoadArea_WithWrongActiveTargetLayer_UsesVisibleUnlockedStreetsLayer()
    {
        var map = CreateEmptyMap();
        var targetLayer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
        var fallbackLayer = new MapLayer(Guid.NewGuid(), "Fallback Streets", MapLayerType.Streets);
        map.AddLayer(targetLayer);
        map.AddLayer(fallbackLayer);

        var command = MapObjectCreationService.CreateRoadAreaCommand(map, RoadAreaPoints(), activeTargetLayerId: targetLayer.Id);

        Assert.False(command.CreatesLayer);
        Assert.Equal(fallbackLayer.Id, command.Layer.Id);
    }

    private static PointD[] RoadAreaPoints()
    {
        return new[]
        {
            new PointD(0.0, 0.0),
            new PointD(100.0, 0.0),
            new PointD(100.0, 40.0),
            new PointD(0.0, 40.0)
        };
    }
}
