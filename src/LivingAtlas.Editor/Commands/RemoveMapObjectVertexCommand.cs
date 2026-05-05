using System;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class RemoveMapObjectVertexCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private readonly int _removeIndex;

	private readonly PointD _removedPoint;

	private readonly string _objectName;

	public string Description => "Remove vertex " + (_removeIndex + 1) + " from " + _objectName;

	public RemoveMapObjectVertexCommand(MapDocument map, Guid objectId, int removeIndex)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		if (removeIndex < 0)
		{
			throw new ArgumentOutOfRangeException("removeIndex", removeIndex, "Remove index cannot be negative.");
		}
		_map = map;
		_objectId = objectId;
		_removeIndex = removeIndex;
		MapObject mapObject = ResolveObject();
		_objectName = mapObject.Name;
		_removedPoint = GetPoint(mapObject, removeIndex);
	}

	public void Execute()
	{
		MapObject mapObject = ResolveObject();
		if (mapObject is RoadLine road)
		{
			road.RemovePoint(_removeIndex);
			return;
		}
		if (mapObject is DistrictShape district)
		{
			district.RemovePoint(_removeIndex);
			return;
		}
		throw new NotSupportedException("Vertex editing is not supported for object type '" + mapObject.GetType().Name + "'.");
	}

	public void Undo()
	{
		MapObject mapObject = ResolveObject();
		if (mapObject is RoadLine road)
		{
			road.InsertPoint(_removeIndex, _removedPoint);
			return;
		}
		if (mapObject is DistrictShape district)
		{
			district.InsertPoint(_removeIndex, _removedPoint);
			return;
		}
		throw new NotSupportedException("Vertex editing is not supported for object type '" + mapObject.GetType().Name + "'.");
	}

	private static PointD GetPoint(MapObject mapObject, int index)
	{
		if (mapObject is RoadLine road)
		{
			if (index < 0 || index >= road.Points.Count)
			{
				throw new ArgumentOutOfRangeException("index", index, "Point index is outside the road point bounds.");
			}
			return road.Points[index];
		}
		if (mapObject is DistrictShape district)
		{
			if (index < 0 || index >= district.PolygonPoints.Count)
			{
				throw new ArgumentOutOfRangeException("index", index, "Point index is outside the district polygon point bounds.");
			}
			return district.PolygonPoints[index];
		}
		throw new NotSupportedException("Vertex editing is not supported for object type '" + mapObject.GetType().Name + "'.");
	}

	private MapObject ResolveObject()
	{
		foreach (MapLayer layer in _map.Layers)
		{
			MapObject? mapObject = layer.Objects.FirstOrDefault(candidate => candidate.Id == _objectId);
			if (mapObject != null)
			{
				return mapObject;
			}
		}
		throw new InvalidOperationException($"Map object '{_objectId}' was not found in map '{_map.Id}'.");
	}
}
