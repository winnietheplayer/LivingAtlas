using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Commands;
using Xunit;

namespace LivingAtlas.Tests;

public class ChildMapScalingTests
{
    private readonly CampaignMapProject _project;
    private readonly MapDocument _parentMap;
    private readonly MapLayer _layer;

    public ChildMapScalingTests()
    {
        _parentMap = new MapDocument(Guid.NewGuid(), "Parent", MapScaleType.City, new SizeD(1000, 1000));
        _project = new CampaignMapProject(Guid.NewGuid(), "Project", _parentMap.Id);
        _project.AddMap(_parentMap);
        _layer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
        _parentMap.AddLayer(_layer);
    }

    [Fact]
    public void CreateChildMap_WithCustomSize_ScalesBoundary()
    {
        // District: 100x100 to 200x200 (Size 100x100)
        var points = new[] { new PointD(100, 100), new PointD(200, 100), new PointD(200, 200), new PointD(100, 200) };
        var district = new DistrictShape(Guid.NewGuid(), "District", _layer.Id, points);
        _layer.AddObject(district);

        // Custom size: 500x500 (Scaling factor: 5.0)
        var customSize = new SizeD(500, 500);
        var command = new CreateChildMapCommand(_project, _parentMap, district, "Child", customSize);
        command.Execute();

        var childMap = command.ChildMap;
        Assert.Equal(500, childMap.RealSizeMeters.Width);
        Assert.Equal(500, childMap.RealSizeMeters.Height);

        var boundaryLayer = childMap.Layers.First(l => l.Name == "Boundaries");
        var boundary = (DistrictShape)boundaryLayer.Objects.First();
        
        // Original (100,100) -> (0,0) in child -> (0,0) scaled
        // Original (200,200) -> (100,100) in child -> (500,500) scaled
        Assert.Equal(new PointD(0, 0), boundary.PolygonPoints[0]);
        Assert.Equal(new PointD(500, 500), boundary.PolygonPoints[2]);
    }

    [Fact]
    public void CreateChildMap_FromCityToDistrict_UsesPhysicalFootprint()
    {
        var points = new[] { new PointD(100, 100), new PointD(150, 100), new PointD(150, 140), new PointD(100, 140) };
        var district = new DistrictShape(Guid.NewGuid(), "City District", _layer.Id, points);
        _layer.AddObject(district);

        var command = new CreateChildMapCommand(_project, _parentMap, district, "Child");
        command.Execute();

        var childMap = command.ChildMap;
        Assert.Equal(MapScaleType.District, childMap.ScaleType);
        Assert.Equal(10.0, childMap.FeetPerUnit);
        Assert.Equal(500.0, childMap.RealSizeMeters.Width);
        Assert.Equal(400.0, childMap.RealSizeMeters.Height);
    }

    [Fact]
    public void CreateChildMap_FromDistrictToBattle_UsesPhysicalFootprint()
    {
        MapDocument parentMap = new MapDocument(Guid.NewGuid(), "District Parent", MapScaleType.District, new SizeD(200, 200));
        CampaignMapProject project = new CampaignMapProject(Guid.NewGuid(), "Project", parentMap.Id);
        project.AddMap(parentMap);
        MapLayer layer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
        parentMap.AddLayer(layer);
        var points = new[] { new PointD(10, 20), new PointD(30, 20), new PointD(30, 30), new PointD(10, 30) };
        var district = new DistrictShape(Guid.NewGuid(), "Battle Footprint", layer.Id, points);
        layer.AddObject(district);

        var command = new CreateChildMapCommand(project, parentMap, district, "Battle Child");
        command.Execute();

        var childMap = command.ChildMap;
        Assert.Equal(MapScaleType.BattleMap, childMap.ScaleType);
        Assert.Equal(5.0, childMap.FeetPerUnit);
        Assert.Equal(40.0, childMap.RealSizeMeters.Width);
        Assert.Equal(20.0, childMap.RealSizeMeters.Height);
    }

    [Fact]
    public void CreateChildMap_CommandStoresExplicitChildFeetPerUnit()
    {
        var points = new[] { new PointD(0, 0), new PointD(60, 0), new PointD(60, 20), new PointD(0, 20) };
        var district = new DistrictShape(Guid.NewGuid(), "Custom Scale", _layer.Id, points);
        _layer.AddObject(district);

        var command = new CreateChildMapCommand(
            _project,
            _parentMap,
            district,
            "Custom Child",
            scaleType: MapScaleType.District,
            childFeetPerUnit: 20.0);
        command.Execute();

        Assert.Equal(20.0, command.ChildMap.FeetPerUnit);
        Assert.Equal(300.0, command.ChildMap.RealSizeMeters.Width);
        Assert.Equal(100.0, command.ChildMap.RealSizeMeters.Height);
    }
}
