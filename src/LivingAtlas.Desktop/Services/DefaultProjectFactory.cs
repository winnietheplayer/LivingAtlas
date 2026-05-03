using System;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Desktop.Services;

internal static class DefaultProjectFactory
{
    public static CampaignMapProject Create()
    {
        Guid rootMapId = Guid.NewGuid();
        SizeD realSizeMeters = new SizeD(2600.0, 1800.0);
        GridSettings gridSettings = new GridSettings(
            isEnabled: true,
            cellSizeMeters: 10.0,
            showGrid: true,
            snapToGrid: false);

        MapDocument rootMap = new MapDocument(
            rootMapId,
            "Monferr",
            MapScaleType.City,
            realSizeMeters,
            parentMapId: null,
            gridSettings);

        Guid districtsLayerId = Guid.NewGuid();
        Guid roadsLayerId = Guid.NewGuid();
        Guid poiLayerId = Guid.NewGuid();
        Guid labelsLayerId = Guid.NewGuid();

        MapLayer districtsLayer = new MapLayer(
            districtsLayerId,
            "Districts",
            MapLayerType.Districts);

        districtsLayer.AddObject(new DistrictShape(
            Guid.NewGuid(),
            "Old Monferr",
            districtsLayerId,
            new[]
            {
                new PointD(420.0, 340.0),
                new PointD(1260.0, 320.0),
                new PointD(1320.0, 980.0),
                new PointD(470.0, 1020.0)
            },
            new[] { "district", "old-city" },
            "district.old"));

        MapLayer roadsLayer = new MapLayer(
            roadsLayerId,
            "Roads",
            MapLayerType.Streets);

        roadsLayer.AddObject(new RoadLine(
            Guid.NewGuid(),
            "Central Street",
            roadsLayerId,
            new[]
            {
                new PointD(220.0, 900.0),
                new PointD(880.0, 860.0),
                new PointD(1600.0, 930.0),
                new PointD(2380.0, 880.0)
            },
            new[] { "road", "main" },
            "road.primary"));

        MapLayer poiLayer = new MapLayer(
            poiLayerId,
            "Points of Interest",
            MapLayerType.PointsOfInterest);

        poiLayer.AddObject(new PointOfInterest(
            Guid.NewGuid(),
            "Main Gate",
            poiLayerId,
            new PointD(180.0, 900.0),
            "gate",
            new[] { "gate", "entrance" },
            "poi.gate"));

        MapLayer labelsLayer = new MapLayer(
            labelsLayerId,
            "Labels",
            MapLayerType.Labels);

        labelsLayer.AddObject(new MapLabel(
            Guid.NewGuid(),
            "Monferr Label",
            labelsLayerId,
            new PointD(1300.0, 250.0),
            "Monferr",
            new[] { "city-label" },
            "label.city"));

        rootMap.AddLayer(districtsLayer);
        rootMap.AddLayer(roadsLayer);
        rootMap.AddLayer(poiLayer);
        rootMap.AddLayer(labelsLayer);

        return new CampaignMapProject(
            Guid.NewGuid(),
            "Living Atlas Project",
            rootMapId,
            new[] { rootMap });
    }
}