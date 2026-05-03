using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Domain.Maps;

public sealed class MapLayer
{
	private readonly List<MapObject> _objects = new List<MapObject>();

	public Guid Id { get; }

	public string Name { get; }

	public MapLayerType LayerType { get; }

	public bool IsVisible { get; private set; }

	public IReadOnlyList<MapObject> Objects => _objects;

	public MapLayer(Guid id, string name, MapLayerType layerType, bool isVisible = true)
	{
		if (id == Guid.Empty)
		{
			throw new ArgumentException("Layer id cannot be empty.", "id");
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Layer name cannot be empty.", "name");
		}
		Id = id;
		Name = name;
		LayerType = layerType;
		IsVisible = isVisible;
	}

	public void SetVisibility(bool isVisible)
	{
		IsVisible = isVisible;
	}

	public void AddObject(MapObject mapObject)
	{
		InsertObject(_objects.Count, mapObject);
	}

	public void InsertObject(int index, MapObject mapObject)
	{
		ArgumentNullException.ThrowIfNull(mapObject, "mapObject");
		if (index < 0 || index > _objects.Count)
		{
			throw new ArgumentOutOfRangeException("index", index, "Object index is outside the layer bounds.");
		}
		if (mapObject.LayerId != Id)
		{
			throw new ArgumentException("Map object belongs to a different layer.", "mapObject");
		}
		if (_objects.Any((MapObject existingObject) => existingObject.Id == mapObject.Id))
		{
			throw new InvalidOperationException($"Map object '{mapObject.Id}' already exists in layer '{Id}'.");
		}
		_objects.Insert(index, mapObject);
	}

	public MapObject? RemoveObject(Guid objectId)
	{
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Map object id cannot be empty.", "objectId");
		}
		int num = _objects.FindIndex((MapObject mapObject) => mapObject.Id == objectId);
		if (num < 0)
		{
			return null;
		}
		MapObject result = _objects[num];
		_objects.RemoveAt(num);
		return result;
	}
}
