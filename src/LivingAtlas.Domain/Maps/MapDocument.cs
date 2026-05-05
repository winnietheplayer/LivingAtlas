using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Domain.Maps;

public sealed class MapDocument
{
	private readonly List<MapLayer> _layers = new List<MapLayer>();

	private readonly List<Guid> _childrenMapIds = new List<Guid>();

	public Guid Id { get; }

	public string Name { get; private set; }

	public MapScaleType ScaleType { get; private set; }

	public SizeD RealSizeMeters { get; private set; }

	public IReadOnlyList<MapLayer> Layers => _layers;

	public Guid? ParentMapId { get; }

	public IReadOnlyList<Guid> ChildrenMapIds => _childrenMapIds;

	public GridSettings GridSettings { get; private set; }

	public MapDocument(Guid id, string name, MapScaleType scaleType, SizeD realSizeMeters, Guid? parentMapId = null, GridSettings? gridSettings = null)
	{
		if (id == Guid.Empty)
		{
			throw new ArgumentException("Map id cannot be empty.", "id");
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Map name cannot be empty.", "name");
		}
		if (parentMapId == Guid.Empty)
		{
			throw new ArgumentException("Parent map id cannot be empty.", "parentMapId");
		}
		Id = id;
		Name = name;
		ScaleType = scaleType;
		RealSizeMeters = realSizeMeters;
		ParentMapId = parentMapId;
		GridSettings = gridSettings ?? GridSettings.Disabled;
	}

	public void Rename(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Map name cannot be empty.", "name");
		}
		Name = name.Trim();
	}

	public void SetScaleType(MapScaleType scaleType)
	{
		ScaleType = scaleType;
	}

	public void SetRealSize(SizeD realSize)
	{
		if (realSize.Width <= 0 || realSize.Height <= 0)
		{
			throw new ArgumentException("Map size must be positive.");
		}
		RealSizeMeters = realSize;
	}

	public void SetGridSettings(GridSettings gridSettings)
	{
		GridSettings = gridSettings;
	}

	public void AddLayer(MapLayer layer)
	{
		ArgumentNullException.ThrowIfNull(layer, "layer");
		if (_layers.Any((MapLayer existingLayer) => existingLayer.Id == layer.Id))
		{
			throw new InvalidOperationException($"Layer '{layer.Id}' already exists in map '{Id}'.");
		}
		_layers.Add(layer);
	}

	public MapLayer? RemoveLayer(Guid layerId)
	{
		if (layerId == Guid.Empty)
		{
			throw new ArgumentException("Layer id cannot be empty.", "layerId");
		}
		int num = _layers.FindIndex((MapLayer layer) => layer.Id == layerId);
		if (num < 0)
		{
			return null;
		}
		MapLayer result = _layers[num];
		_layers.RemoveAt(num);
		return result;
	}

	public bool MoveLayerUp(Guid layerId)
	{
		int index = _layers.FindIndex(l => l.Id == layerId);
		if (index < 0 || index >= _layers.Count - 1)
		{
			return false;
		}

		MapLayer layer = _layers[index];
		_layers.RemoveAt(index);
		_layers.Insert(index + 1, layer);
		return true;
	}

	public bool MoveLayerDown(Guid layerId)
	{
		int index = _layers.FindIndex(l => l.Id == layerId);
		if (index <= 0)
		{
			return false;
		}

		MapLayer layer = _layers[index];
		_layers.RemoveAt(index);
		_layers.Insert(index - 1, layer);
		return true;
	}

	public void AddChildMapId(Guid childMapId)
	{
		if (childMapId == Guid.Empty)
		{
			throw new ArgumentException("Child map id cannot be empty.", "childMapId");
		}
		if (!_childrenMapIds.Contains(childMapId))
		{
			_childrenMapIds.Add(childMapId);
		}
	}

	public bool RemoveChildMapId(Guid childMapId)
	{
		if (childMapId == Guid.Empty)
		{
			throw new ArgumentException("Child map id cannot be empty.", "childMapId");
		}
		return _childrenMapIds.Remove(childMapId);
	}
}
