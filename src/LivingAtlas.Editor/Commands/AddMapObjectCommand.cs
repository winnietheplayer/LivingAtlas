using System;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class AddMapObjectCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly MapLayer _layer;

	private readonly MapObject _mapObject;

	private readonly bool _createsLayer;

	public string Description => "Create " + _mapObject.Name;

	public MapObject MapObject => _mapObject;

	public MapLayer Layer => _layer;

	public bool CreatesLayer => _createsLayer;

	public AddMapObjectCommand(MapDocument map, MapLayer layer, MapObject mapObject, bool createsLayer = false)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		ArgumentNullException.ThrowIfNull(layer, "layer");
		ArgumentNullException.ThrowIfNull(mapObject, "mapObject");
		if (mapObject.LayerId != layer.Id)
		{
			throw new ArgumentException("Map object belongs to a different layer.", "mapObject");
		}
		_map = map;
		_layer = layer;
		_mapObject = mapObject;
		_createsLayer = createsLayer;
	}

	public void Execute()
	{
		MapLayer mapLayer = EnsureLayerForExecute();
		mapLayer.AddObject(_mapObject);
	}

	public void Undo()
	{
		MapLayer mapLayer = FindLayer() ?? throw new InvalidOperationException($"Layer '{_layer.Id}' was not found in map '{_map.Id}'.");
		MapObject mapObject = mapLayer.RemoveObject(_mapObject.Id);
		if (mapObject == null)
		{
			throw new InvalidOperationException($"Map object '{_mapObject.Id}' was not found in layer '{mapLayer.Id}'.");
		}
		if (_createsLayer && mapLayer.Objects.Count == 0)
		{
			_map.RemoveLayer(mapLayer.Id);
		}
	}

	private MapLayer EnsureLayerForExecute()
	{
		MapLayer mapLayer = FindLayer();
		if (mapLayer != null)
		{
			return mapLayer;
		}
		if (!_createsLayer)
		{
			throw new InvalidOperationException($"Layer '{_layer.Id}' was not found in map '{_map.Id}'.");
		}
		_map.AddLayer(_layer);
		return _layer;
	}

	private MapLayer? FindLayer()
	{
		return _map.Layers.FirstOrDefault((MapLayer layer) => layer.Id == _layer.Id);
	}
}
