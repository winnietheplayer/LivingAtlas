using System;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;

namespace LivingAtlas.Editor.Tools;

/// <summary>
/// Provides snapping logic for aligning points to a grid.
/// </summary>
public static class GridSnapper
{
    /// <summary>
    /// Snaps a point to the nearest grid line if grid snapping is enabled.
    /// Otherwise, returns the original point.
    /// </summary>
    public static PointD Snap(PointD point, GridSettings grid)
    {
        if (!grid.IsEnabled || !grid.SnapToGrid || grid.CellSizeMeters <= 0.0)
        {
            return point;
        }

        double snappedX = Math.Round(point.X / grid.CellSizeMeters) * grid.CellSizeMeters;
        double snappedY = Math.Round(point.Y / grid.CellSizeMeters) * grid.CellSizeMeters;

        return new PointD(snappedX, snappedY);
    }
}
