using System;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class AddMapObjectVertexCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private readonly int _insertIndex;

	private readonly PointD _point;

	private readonly string _objectName;

	public string Description => "Add vertex " + (_insertIndex + 1) + " to " + _objectName;

	public AddMapObjectVertexCommand(MapDocument map, Guid objectId, int insertIndex, PointD point)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		if (insertIndex < 0)
		{
			throw new ArgumentOutOfRangeException("insertIndex", insertIndex, "Insert index cannot be negative.");
		}
		_map = map;
		_objectId = objectId;
		_insertIndex = insertIndex;
		_point = point;
		_objectName = ResolveObject().Name;
	}

	public void Execute()
	{
		MapObject mapObject = ResolveObject();
		if (mapObject is RoadLine road)
		{
			road.InsertPoint(_insertIndex, _point);
			return;
		}
		if (mapObject is RoadArea roadArea)
		{
			roadArea.InsertPoint(_insertIndex, _point);
			return;
		}
		if (mapObject is DistrictShape district)
		{
			district.InsertPoint(_insertIndex, _point);
			return;
		}
		throw new NotSupportedException("Vertex editing is not supported for object type '" + mapObject.GetType().Name + "'.");
	}

	public void Undo()
	{
		MapObject mapObject = ResolveObject();
		if (mapObject is RoadLine road)
		{
			road.RemovePoint(_insertIndex);
			return;
		}
		if (mapObject is RoadArea roadArea)
		{
			roadArea.RemovePoint(_insertIndex);
			return;
		}
		if (mapObject is DistrictShape district)
		{
			district.RemovePoint(_insertIndex);
			return;
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
