using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingAtlas.Domain.Maps.Objects;

public abstract class MapObject
{
	private readonly IReadOnlyList<string> _tags;

	public Guid Id { get; }

	public string Name { get; private set; }

	public MapObjectType ObjectType { get; }

	public Guid LayerId { get; }

	public IReadOnlyList<string> Tags => _tags;

	public string StyleKey { get; private set; }

	public string Description { get; private set; }

	protected MapObject(Guid id, string name, MapObjectType objectType, Guid layerId, IEnumerable<string>? tags = null, string? styleKey = null, string? description = null)
	{
		if (id == Guid.Empty)
		{
			throw new ArgumentException("Map object id cannot be empty.", "id");
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Map object name cannot be empty.", "name");
		}
		if (layerId == Guid.Empty)
		{
			throw new ArgumentException("Layer id cannot be empty.", "layerId");
		}
		Id = id;
		Name = name;
		ObjectType = objectType;
		LayerId = layerId;
		StyleKey = styleKey?.Trim() ?? string.Empty;
		Description = description ?? string.Empty;
		_tags = NormalizeTags(tags);
	}

	public void Rename(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Map object name cannot be empty.", "name");
		}
		Name = name.Trim();
	}

	public void SetStyleKey(string? styleKey)
	{
		StyleKey = styleKey?.Trim() ?? string.Empty;
	}

	public void SetDescription(string? description)
	{
		Description = description ?? string.Empty;
	}

	private static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags)
	{
		if (tags == null)
		{
			return Array.Empty<string>();
		}
		return (from tag in tags
			where !string.IsNullOrWhiteSpace(tag)
			select tag.Trim()).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToArray();
	}
}
