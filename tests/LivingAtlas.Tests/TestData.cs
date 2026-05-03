using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Tests;

internal static class TestData
{
    public static MapDocument CreateCityMap(Guid? id = null, Guid? parentMapId = null)
    {
        return new MapDocument(
            id ?? Guid.NewGuid(),
            "Test City",
            MapScaleType.City,
            new SizeD(2600.0, 1800.0),
            parentMapId,
            new GridSettings(
                isEnabled: true,
                cellSizeMeters: 10.0,
                showGrid: true,
                snapToGrid: false));
    }

    public static MapDocument CreateDistrictMap(Guid? id = null, Guid? parentMapId = null)
    {
        return new MapDocument(
            id ?? Guid.NewGuid(),
            "Test District",
            MapScaleType.District,
            new SizeD(400.0, 300.0),
            parentMapId);
    }

    public static CampaignMapProject CreateProject(MapDocument rootMap)
    {
        return new CampaignMapProject(
            Guid.NewGuid(),
            "Test Project",
            rootMap.Id,
            new[] { rootMap });
    }

    public static MapLayer CreateLayer(
        Guid? id = null,
        string name = "Test Layer",
        MapLayerType layerType = MapLayerType.PointsOfInterest)
    {
        return new MapLayer(id ?? Guid.NewGuid(), name, layerType);
    }

    public static PointOfInterest CreatePointOfInterest(Guid layerId, Guid? id = null, string name = "Gate")
    {
        return new PointOfInterest(
            id ?? Guid.NewGuid(),
            name,
            layerId,
            new PointD(100.0, 200.0),
            "gate",
            new[] { "poi" },
            "poi.gate");
    }

    public static MapLabel CreateLabel(Guid layerId, Guid? id = null, string name = "City Label")
    {
        return new MapLabel(
            id ?? Guid.NewGuid(),
            name,
            layerId,
            new PointD(120.0, 220.0),
            "Original text",
            new[] { "label" },
            "label.city");
    }

    public static RoadLine CreateRoad(Guid layerId, Guid? id = null)
    {
        return new RoadLine(
            id ?? Guid.NewGuid(),
            "Main Road",
            layerId,
            new[]
            {
                new PointD(10.0, 20.0),
                new PointD(30.0, 40.0)
            },
            new[] { "road" },
            "road.primary");
    }

    public static DistrictShape CreateDistrict(Guid layerId, Guid? id = null, Guid? childMapId = null)
    {
        return new DistrictShape(
            id ?? Guid.NewGuid(),
            "Old District",
            layerId,
            new[]
            {
                new PointD(100.0, 100.0),
                new PointD(500.0, 120.0),
                new PointD(520.0, 420.0),
                new PointD(90.0, 400.0)
            },
            new[] { "district" },
            "district.old",
            childMapId);
    }
}
