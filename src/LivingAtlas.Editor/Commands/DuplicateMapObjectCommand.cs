using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class DuplicateMapObjectCommand : IEditorCommand
{
    private readonly MapDocument _map;
    private readonly MapLayer _layer;
    private readonly MapObject _duplicate;

    public string Description => "Duplicate " + _duplicate.Name;

    public MapObject Duplicate => _duplicate;

    public DuplicateMapObjectCommand(MapDocument map, MapObject original)
    {
        ArgumentNullException.ThrowIfNull(map, nameof(map));
        ArgumentNullException.ThrowIfNull(original, nameof(original));

        _map = map;
        _layer = map.Layers.FirstOrDefault(l => l.Id == original.LayerId)
                 ?? throw new InvalidOperationException($"Layer '{original.LayerId}' was not found in map '{map.Id}'.");

        if (_layer.IsLocked)
        {
            throw new InvalidOperationException("Cannot duplicate object on a locked layer.");
        }

        if (!_layer.IsVisible)
        {
            throw new InvalidOperationException("Cannot duplicate object on a hidden layer.");
        }

        PointD offset = CalculateOffset(map.GridSettings);
        _duplicate = CloneAndShift(original, offset);
    }

    public void Execute()
    {
        _layer.AddObject(_duplicate);
    }

    public void Undo()
    {
        _layer.RemoveObject(_duplicate.Id);
    }

    private static PointD CalculateOffset(GridSettings grid)
    {
        if (grid.IsEnabled && grid.SnapToGrid && grid.CellSizeMeters > 0)
        {
            return new PointD(grid.CellSizeMeters, grid.CellSizeMeters);
        }
        return new PointD(20.0, 20.0);
    }

    private static MapObject CloneAndShift(MapObject original, PointD offset)
    {
        Guid newId = Guid.NewGuid();
        string newName = original.Name + " Copy";

        if (original is PointOfInterest poi)
        {
            PointD shiftedPos = new PointD(poi.Position.X + offset.X, poi.Position.Y + offset.Y);
            return new PointOfInterest(newId, newName, poi.LayerId, shiftedPos, poi.IconKey, poi.Tags, poi.StyleKey);
        }
        
        if (original is MapLabel label)
        {
            PointD shiftedPos = new PointD(label.Position.X + offset.X, label.Position.Y + offset.Y);
            return new MapLabel(newId, newName, label.LayerId, shiftedPos, label.Text, label.Tags, label.StyleKey);
        }
        
        if (original is RoadLine road)
        {
            IEnumerable<PointD> shiftedPoints = road.Points.Select(p => new PointD(p.X + offset.X, p.Y + offset.Y)).ToList();
            return new RoadLine(newId, newName, road.LayerId, shiftedPoints, road.Tags, road.StyleKey);
        }
        
        if (original is DistrictShape district)
        {
            IEnumerable<PointD> shiftedPoints = district.PolygonPoints.Select(p => new PointD(p.X + offset.X, p.Y + offset.Y)).ToList();
            // ChildMapId MUST NOT be copied
            return new DistrictShape(newId, newName, district.LayerId, shiftedPoints, district.Tags, district.StyleKey, null);
        }

        throw new NotSupportedException($"Duplication is not supported for object type '{original.GetType().Name}'.");
    }
}
