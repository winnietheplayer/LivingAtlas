using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Commands;

namespace LivingAtlas.Editor.Creation;

public static class MapObjectCreationService
{
	private const string DefaultPoiIconKey = "poi";

	private const string DistrictBaseName = "District";

	private const string DistrictLayerName = "Districts";

	private const string LabelBaseName = "Label";

	private const string LabelLayerName = "Labels";

	private const string PoiBaseName = "Point of Interest";

	private const string PoiLayerName = "Points of Interest";

	private const string RoadBaseName = "Road";

	private const string RoadLayerName = "Roads";

	public static AddMapObjectCommand CreatePointOfInterestCommand(MapDocument map, PointD position, Guid? activeTargetLayerId = null)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		MapLayer? mapLayer = null;
		if (activeTargetLayerId.HasValue)
		{
			mapLayer = map.Layers.FirstOrDefault(l => l.Id == activeTargetLayerId.Value && l.LayerType == MapLayerType.PointsOfInterest && !l.IsLocked && l.IsVisible);
		}
		if (mapLayer == null)
		{
			mapLayer = map.Layers.FirstOrDefault((MapLayer candidate) => candidate.LayerType == MapLayerType.PointsOfInterest && !candidate.IsLocked && candidate.IsVisible);
		}
		bool createsLayer = mapLayer == null;
		if (mapLayer == null)
		{
			mapLayer = new MapLayer(Guid.NewGuid(), "Points of Interest", MapLayerType.PointsOfInterest);
		}
		PointOfInterest mapObject = new PointOfInterest(Guid.NewGuid(), MapObjectNameService.GenerateUniqueName(map, "Point of Interest"), mapLayer.Id, position, "poi");
		return new AddMapObjectCommand(map, mapLayer, mapObject, createsLayer);
	}

	public static AddMapObjectCommand CreateLabelCommand(MapDocument map, PointD position, Guid? activeTargetLayerId = null)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		MapLayer? mapLayer = null;
		if (activeTargetLayerId.HasValue)
		{
			mapLayer = map.Layers.FirstOrDefault(l => l.Id == activeTargetLayerId.Value && l.LayerType == MapLayerType.Labels && !l.IsLocked && l.IsVisible);
		}
		if (mapLayer == null)
		{
			mapLayer = map.Layers.FirstOrDefault((MapLayer candidate) => candidate.LayerType == MapLayerType.Labels && !candidate.IsLocked && candidate.IsVisible);
		}
		bool createsLayer = mapLayer == null;
		if (mapLayer == null)
		{
			mapLayer = new MapLayer(Guid.NewGuid(), "Labels", MapLayerType.Labels);
		}
		string text = MapObjectNameService.GenerateUniqueName(map, "Label");
		MapLabel mapObject = new MapLabel(Guid.NewGuid(), text, mapLayer.Id, position, text);
		return new AddMapObjectCommand(map, mapLayer, mapObject, createsLayer);
	}

	public static AddMapObjectCommand CreateRoadLineCommand(MapDocument map, IEnumerable<PointD> points, Guid? activeTargetLayerId = null)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		ArgumentNullException.ThrowIfNull(points, "points");
		MapLayer? mapLayer = null;
		if (activeTargetLayerId.HasValue)
		{
			mapLayer = map.Layers.FirstOrDefault(l => l.Id == activeTargetLayerId.Value && l.LayerType == MapLayerType.Streets && !l.IsLocked && l.IsVisible);
		}
		if (mapLayer == null)
		{
			mapLayer = map.Layers.FirstOrDefault((MapLayer candidate) => candidate.LayerType == MapLayerType.Streets && !candidate.IsLocked && candidate.IsVisible);
		}
		bool createsLayer = mapLayer == null;
		if (mapLayer == null)
		{
			mapLayer = new MapLayer(Guid.NewGuid(), "Roads", MapLayerType.Streets);
		}
		RoadLine mapObject = new RoadLine(Guid.NewGuid(), MapObjectNameService.GenerateUniqueName(map, "Road"), mapLayer.Id, points);
		return new AddMapObjectCommand(map, mapLayer, mapObject, createsLayer);
	}

	public static AddMapObjectCommand CreateDistrictShapeCommand(MapDocument map, IEnumerable<PointD> polygonPoints, Guid? activeTargetLayerId = null)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		ArgumentNullException.ThrowIfNull(polygonPoints, "polygonPoints");
		MapLayer? mapLayer = null;
		if (activeTargetLayerId.HasValue)
		{
			mapLayer = map.Layers.FirstOrDefault(l => l.Id == activeTargetLayerId.Value && l.LayerType == MapLayerType.Districts && !l.IsLocked && l.IsVisible);
		}
		if (mapLayer == null)
		{
			mapLayer = map.Layers.FirstOrDefault((MapLayer candidate) => candidate.LayerType == MapLayerType.Districts && !candidate.IsLocked && candidate.IsVisible);
		}
		bool createsLayer = mapLayer == null;
		if (mapLayer == null)
		{
			mapLayer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
		}
		DistrictShape mapObject = new DistrictShape(Guid.NewGuid(), MapObjectNameService.GenerateUniqueName(map, "District"), mapLayer.Id, polygonPoints);
		return new AddMapObjectCommand(map, mapLayer, mapObject, createsLayer);
	}
}
