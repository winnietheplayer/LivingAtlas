using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Commands;
using Xunit;

namespace LivingAtlas.Tests;

public class ObjectDuplicationTests
{
    private readonly MapDocument _map;
    private readonly MapLayer _layer;

    public ObjectDuplicationTests()
    {
        _map = new MapDocument(Guid.NewGuid(), "Test Map", MapScaleType.City, new SizeD(1000, 1000));
        _layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.Districts);
        _map.AddLayer(_layer);
    }

    [Fact]
    public void DuplicatePointOfInterest_CreatesNewObjectWithShiftedPosition()
    {
        var original = new PointOfInterest(Guid.NewGuid(), "Original POI", _layer.Id, new PointD(100, 100), "icon");
        _layer.AddObject(original);

        var command = new DuplicateMapObjectCommand(_map, original);
        command.Execute();

        var duplicate = (PointOfInterest)command.Duplicate;
        Assert.NotEqual(original.Id, duplicate.Id);
        Assert.Equal("Original POI Copy", duplicate.Name);
        Assert.Equal(original.LayerId, duplicate.LayerId);
        Assert.Equal(new PointD(120, 120), duplicate.Position); // Default offset is 20, 20
    }

    [Fact]
    public void DuplicateMapLabel_CopiesTextAndShiftsPosition()
    {
        var original = new MapLabel(Guid.NewGuid(), "Original Label", _layer.Id, new PointD(100, 100), "Hello");
        _layer.AddObject(original);

        var command = new DuplicateMapObjectCommand(_map, original);
        command.Execute();

        var duplicate = (MapLabel)command.Duplicate;
        Assert.Equal("Hello", duplicate.Text);
        Assert.Equal(new PointD(120, 120), duplicate.Position);
    }

    [Fact]
    public void DuplicateRoadLine_CopiesAndShiftsAllPoints()
    {
        var originalPoints = new[] { new PointD(10, 10), new PointD(20, 20) };
        var original = new RoadLine(Guid.NewGuid(), "Original Road", _layer.Id, originalPoints);
        _layer.AddObject(original);

        var command = new DuplicateMapObjectCommand(_map, original);
        command.Execute();

        var duplicate = (RoadLine)command.Duplicate;
        Assert.Equal(2, duplicate.Points.Count);
        Assert.Equal(new PointD(30, 30), duplicate.Points[0]);
        Assert.Equal(new PointD(40, 40), duplicate.Points[1]);
    }

    [Fact]
    public void DuplicateDistrictShape_CopiesPointsAndDoesNotCopyChildMapId()
    {
        var originalPoints = new[] { new PointD(0, 0), new PointD(10, 0), new PointD(0, 10) };
        var original = new DistrictShape(Guid.NewGuid(), "Original District", _layer.Id, originalPoints, childMapId: Guid.NewGuid());
        _layer.AddObject(original);

        var command = new DuplicateMapObjectCommand(_map, original);
        command.Execute();

        var duplicate = (DistrictShape)command.Duplicate;
        Assert.Null(duplicate.ChildMapId);
        Assert.Equal(new PointD(20, 20), duplicate.PolygonPoints[0]);
    }

    [Fact]
    public void Duplicate_UsesGridOffsetIfSnappingEnabled()
    {
        _map.SetGridSettings(new GridSettings(isEnabled: true, cellSizeMeters: 50.0, showGrid: true, snapToGrid: true));
        var original = new PointOfInterest(Guid.NewGuid(), "Original POI", _layer.Id, new PointD(100, 100), "icon");
        
        var command = new DuplicateMapObjectCommand(_map, original);
        command.Execute();

        var duplicate = (PointOfInterest)command.Duplicate;
        Assert.Equal(new PointD(150, 150), duplicate.Position); // 100 + 50
    }

    [Fact]
    public void UndoRedo_WorksCorrectly()
    {
        var original = new PointOfInterest(Guid.NewGuid(), "Original POI", _layer.Id, new PointD(100, 100), "icon");
        _layer.AddObject(original);

        var command = new DuplicateMapObjectCommand(_map, original);
        
        command.Execute();
        Assert.Equal(2, _layer.Objects.Count);
        
        command.Undo();
        Assert.Single(_layer.Objects);
        Assert.DoesNotContain(_layer.Objects, o => o.Id == command.Duplicate.Id);

        command.Execute();
        Assert.Equal(2, _layer.Objects.Count);
        Assert.Contains(_layer.Objects, o => o.Id == command.Duplicate.Id);
    }

    [Fact]
    public void Duplicate_ThrowsIfLayerLocked()
    {
        _layer.SetLocked(true);
        var original = new PointOfInterest(Guid.NewGuid(), "Original POI", _layer.Id, new PointD(100, 100), "icon");
        
        Assert.Throws<InvalidOperationException>(() => new DuplicateMapObjectCommand(_map, original));
    }

    [Fact]
    public void Duplicate_ThrowsIfLayerHidden()
    {
        _layer.SetVisibility(false);
        var original = new PointOfInterest(Guid.NewGuid(), "Original POI", _layer.Id, new PointD(100, 100), "icon");
        
        Assert.Throws<InvalidOperationException>(() => new DuplicateMapObjectCommand(_map, original));
    }
}
