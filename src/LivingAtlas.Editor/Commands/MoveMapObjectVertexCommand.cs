using System;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class MoveMapObjectVertexCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private readonly int _vertexIndex;

	private readonly PointD _oldPoint;

	private readonly PointD _newPoint;

	private readonly string _objectName;

	public string Description => "Move vertex " + (_vertexIndex + 1) + " of " + _objectName;

	public MoveMapObjectVertexCommand(MapDocument map, Guid objectId, int vertexIndex, PointD oldPoint, PointD newPoint)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		if (vertexIndex < 0)
		{
			throw new ArgumentOutOfRangeException("vertexIndex", vertexIndex, "Vertex index cannot be negative.");
		}
		_map = map;
		_objectId = objectId;
		_vertexIndex = vertexIndex;
		_oldPoint = oldPoint;
		_newPoint = newPoint;
		_objectName = ResolveObject().Name;
	}

	public void Execute()
	{
		SetPoint(_newPoint);
	}

	public void Undo()
	{
		SetPoint(_oldPoint);
	}

	private void SetPoint(PointD point)
	{
		MapObject mapObject = ResolveObject();
		if (mapObject is RoadLine road)
		{
			road.SetPoint(_vertexIndex, point);
			return;
		}
		if (mapObject is RoadArea roadArea)
		{
			roadArea.SetPoint(_vertexIndex, point);
			return;
		}
		if (mapObject is DistrictShape district)
		{
			district.SetPoint(_vertexIndex, point);
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
