using System;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Transforms;

namespace LivingAtlas.Editor.Commands;

public sealed class MoveMapObjectCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private readonly PointD _delta;

	private readonly string _objectName;

	public string Description => "Move " + _objectName;

	public MoveMapObjectCommand(MapDocument map, Guid objectId, PointD delta)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		_map = map;
		_objectId = objectId;
		_delta = delta;
		_objectName = ResolveObject().Name;
	}

	public void Execute()
	{
		MapObjectMover.MoveBy(ResolveObject(), _delta);
	}

	public void Undo()
	{
		MapObjectMover.MoveBy(ResolveObject(), new PointD(0.0 - _delta.X, 0.0 - _delta.Y));
	}

	private MapObject ResolveObject()
	{
		foreach (MapLayer layer in _map.Layers)
		{
			MapObject? mapObject = layer.Objects.FirstOrDefault((MapObject candidate) => candidate.Id == _objectId);
			if (mapObject != null)
			{
				return mapObject;
			}
		}
		throw new InvalidOperationException($"Map object '{_objectId}' was not found in map '{_map.Id}'.");
	}
}
