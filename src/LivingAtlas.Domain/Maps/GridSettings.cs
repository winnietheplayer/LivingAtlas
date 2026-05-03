using System;

namespace LivingAtlas.Domain.Maps;

public readonly record struct GridSettings
{
	public static GridSettings Disabled { get; } = new GridSettings(isEnabled: false, 1.0, showGrid: false, snapToGrid: false);

	public bool IsEnabled { get; }

	public double CellSizeMeters { get; }

	public bool ShowGrid { get; }

	public bool SnapToGrid { get; }

	public GridSettings(bool isEnabled, double cellSizeMeters, bool showGrid, bool snapToGrid)
	{
		if (cellSizeMeters <= 0.0)
		{
			throw new ArgumentOutOfRangeException("cellSizeMeters", cellSizeMeters, "Grid cell size must be positive.");
		}
		IsEnabled = isEnabled;
		CellSizeMeters = cellSizeMeters;
		ShowGrid = showGrid;
		SnapToGrid = snapToGrid;
	}
}
