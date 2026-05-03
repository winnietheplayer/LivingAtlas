using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Maps;

namespace LivingAtlas.Domain.Projects;

public sealed class CampaignMapProject
{
	private readonly List<MapDocument> _maps = new List<MapDocument>();

	public Guid Id { get; }

	public string Name { get; }

	public Guid RootMapId { get; }

	public IReadOnlyList<MapDocument> Maps => _maps;

	public MapDocument RootMap => FindMap(RootMapId) ?? throw new InvalidOperationException($"Root map '{RootMapId}' is not present in project '{Id}'.");

	public CampaignMapProject(Guid id, string name, Guid rootMapId, IEnumerable<MapDocument>? maps = null)
	{
		if (id == Guid.Empty)
		{
			throw new ArgumentException("Project id cannot be empty.", "id");
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Project name cannot be empty.", "name");
		}
		if (rootMapId == Guid.Empty)
		{
			throw new ArgumentException("Root map id cannot be empty.", "rootMapId");
		}
		Id = id;
		Name = name;
		RootMapId = rootMapId;
		if (maps != null)
		{
			foreach (MapDocument map in maps)
			{
				AddMap(map);
			}
		}
		if (_maps.Count > 0 && _maps.All((MapDocument map) => map.Id != rootMapId))
		{
			throw new ArgumentException("Root map must be included in the project maps.", "rootMapId");
		}
	}

	public MapDocument? FindMap(Guid mapId)
	{
		return _maps.FirstOrDefault((MapDocument map) => map.Id == mapId);
	}

	public void AddMap(MapDocument map)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (_maps.Any((MapDocument existingMap) => existingMap.Id == map.Id))
		{
			throw new InvalidOperationException($"Map '{map.Id}' already exists in project '{Id}'.");
		}
		_maps.Add(map);
	}

	public MapDocument? RemoveMap(Guid mapId)
	{
		if (mapId == Guid.Empty)
		{
			throw new ArgumentException("Map id cannot be empty.", "mapId");
		}
		if (mapId == RootMapId)
		{
			throw new InvalidOperationException("Root map cannot be removed from the project.");
		}
		int num = _maps.FindIndex((MapDocument map) => map.Id == mapId);
		if (num < 0)
		{
			return null;
		}
		MapDocument result = _maps[num];
		_maps.RemoveAt(num);
		return result;
	}
}
