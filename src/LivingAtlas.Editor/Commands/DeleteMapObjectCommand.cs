using System;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class DeleteMapObjectCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private readonly Guid _layerId;

	private readonly MapObject _removedObject;

	private readonly int _originalIndex;

	public string Description => "Delete " + _removedObject.Name;

	public MapObject MapObject => _removedObject;

	public Guid LayerId => _layerId;

	public int OriginalIndex => _originalIndex;

	public DeleteMapObjectCommand(MapDocument map, Guid objectId)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		_map = map;
		_objectId = objectId;
		(MapLayer, MapObject, int) tuple = ResolveObjectWithLayer();
		_layerId = tuple.Item1.Id;
		_removedObject = tuple.Item2;
		_originalIndex = tuple.Item3;
	}

	public void Execute()
	{
		MapLayer mapLayer = ResolveLayer();
		MapObject mapObject = mapLayer.RemoveObject(_objectId);
		if (mapObject == null)
		{
			throw new InvalidOperationException($"Map object '{_objectId}' was not found in layer '{_layerId}'.");
		}
	}

	public void Undo()
	{
		MapLayer mapLayer = ResolveLayer();
		mapLayer.InsertObject(_originalIndex, _removedObject);
	}

	private MapLayer ResolveLayer()
	{
		return _map.Layers.FirstOrDefault((MapLayer layer) => layer.Id == _layerId) ?? throw new InvalidOperationException($"Layer '{_layerId}' was not found in map '{_map.Id}'.");
	}

	private (MapLayer Layer, MapObject MapObject, int Index) ResolveObjectWithLayer()
	{
		foreach (MapLayer layer in _map.Layers)
		{
			for (int i = 0; i < layer.Objects.Count; i++)
			{
				MapObject mapObject = layer.Objects[i];
				if (mapObject.Id == _objectId)
				{
					return (Layer: layer, MapObject: mapObject, Index: i);
				}
			}
		}
		throw new InvalidOperationException($"Map object '{_objectId}' was not found in map '{_map.Id}'.");
	}
}
