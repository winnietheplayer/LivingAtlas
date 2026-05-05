using System;
using System.Collections.Generic;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Creation;

public static class MapObjectStylePresets
{
    private static readonly Dictionary<MapObjectType, IReadOnlyList<string>> Presets = new()
    {
        {
            MapObjectType.DistrictShape, new[]
            {
                "district.default",
                "district.old",
                "district.boundary",
                "district.industrial",
                "district.slums"
            }
        },
        {
            MapObjectType.RoadLine, new[]
            {
                "road.primary",
                "road.secondary",
                "road.alley"
            }
        },
        {
            MapObjectType.PointOfInterest, new[]
            {
                "poi.default",
                "poi.gate",
                "poi.landmark",
                "poi.danger"
            }
        },
        {
            MapObjectType.MapLabel, new[]
            {
                "label.city",
                "label.district",
                "label.map-title",
                "label.note"
            }
        }
    };

    public static IReadOnlyList<string> GetPresetsForType(MapObjectType type)
    {
        if (Presets.TryGetValue(type, out var presets))
        {
            return presets;
        }
        return Array.Empty<string>();
    }
}
