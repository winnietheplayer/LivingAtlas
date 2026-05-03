using System;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Transforms;

namespace LivingAtlas.Editor.Commands;

public sealed class MoveMapObjectDragSession
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private PointD _totalDelta;

	public PointD TotalDelta => _totalDelta;

	public bool HasMovement => !IsZeroDelta(_totalDelta);

	public MoveMapObjectDragSession(MapDocument map, Guid objectId)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		_map = map;
		_objectId = objectId;
	}

	public void PreviewMoveBy(PointD delta)
	{
		if (!IsZeroDelta(delta))
		{
			MapObjectMover.MoveBy(ResolveObject(), delta);
			_totalDelta = new PointD(_totalDelta.X + delta.X, _totalDelta.Y + delta.Y);
		}
	}

	public MoveMapObjectCommand? CreateCommandFromPreview()
	{
		if (!HasMovement)
		{
			return null;
		}
		MapObjectMover.MoveBy(ResolveObject(), new PointD(0.0 - _totalDelta.X, 0.0 - _totalDelta.Y));
		return new MoveMapObjectCommand(_map, _objectId, _totalDelta);
	}

	private MapObject ResolveObject()
	{
		foreach (MapLayer layer in _map.Layers)
		{
			MapObject mapObject = layer.Objects.FirstOrDefault((MapObject candidate) => candidate.Id == _objectId);
			if (mapObject != null)
			{
				return mapObject;
			}
		}
		throw new InvalidOperationException($"Map object '{_objectId}' was not found in map '{_map.Id}'.");
	}

	private static bool IsZeroDelta(PointD delta)
	{
		return Math.Abs(delta.X) < 1E-06 && Math.Abs(delta.Y) < 1E-06;
	}
}
