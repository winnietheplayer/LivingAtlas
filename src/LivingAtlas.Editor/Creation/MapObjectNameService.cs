using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Creation;

public static class MapObjectNameService
{
	public static string GenerateUniqueName(MapDocument map, string baseName)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (string.IsNullOrWhiteSpace(baseName))
		{
			throw new ArgumentException("Base name cannot be empty.", "baseName");
		}
		string value = baseName.Trim();
		HashSet<string> hashSet = (from mapObject in map.Layers.SelectMany((MapLayer layer) => layer.Objects)
			select mapObject.Name).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		int num = 1;
		string text;
		while (true)
		{
			text = $"{value} {num}";
			if (!hashSet.Contains(text))
			{
				break;
			}
			num++;
		}
		return text;
	}
}
