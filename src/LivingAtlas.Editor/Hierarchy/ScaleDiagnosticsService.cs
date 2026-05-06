using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Editor.Hierarchy;

public static class ScaleDiagnosticsService
{
	public const double DefaultSizeTolerance = 0.01;

	public static MapScaleDiagnostics GetMapDiagnostics(MapDocument map)
	{
		ArgumentNullException.ThrowIfNull(map);
		return new MapScaleDiagnostics(
			map.ScaleType,
			map.RealSizeMeters,
			map.FeetPerUnit,
			new SizeD(map.RealSizeMeters.Width * map.FeetPerUnit, map.RealSizeMeters.Height * map.FeetPerUnit),
			map.GridSettings.CellSizeMeters,
			map.GridSettings.CellSizeMeters * map.FeetPerUnit);
	}

	public static ChildMapScaleDiagnostics? GetChildMapDiagnostics(CampaignMapProject project, MapDocument childMap, double tolerance = DefaultSizeTolerance)
	{
		ArgumentNullException.ThrowIfNull(project);
		ArgumentNullException.ThrowIfNull(childMap);
		if (!childMap.ParentMapId.HasValue)
		{
			return null;
		}

		MapDocument? parentMap = project.FindMap(childMap.ParentMapId.Value);
		if (parentMap == null)
		{
			return null;
		}

		DistrictShape? linkedDistrict = FindLinkedDistrict(parentMap, childMap.Id);
		if (linkedDistrict == null)
		{
			return null;
		}

		RectD parentBounds = GetBoundingBox(linkedDistrict.PolygonPoints);
		SizeD parentFootprintLocalSize = parentBounds.Size;
		SizeD parentFootprintPhysicalSize = ToPhysicalFeet(parentFootprintLocalSize, parentMap.FeetPerUnit);
		SizeD expectedChildLocalSize = ToLocalUnits(parentFootprintPhysicalSize, childMap.FeetPerUnit);
		bool hasMismatch = HasSizeMismatch(childMap.RealSizeMeters, expectedChildLocalSize, tolerance);
		string? warning = hasMismatch
			? $"Child map scale mismatch: expected {expectedChildLocalSize.Width:0.##}x{expectedChildLocalSize.Height:0.##} units, actual {childMap.RealSizeMeters.Width:0.##}x{childMap.RealSizeMeters.Height:0.##}."
			: null;

		return new ChildMapScaleDiagnostics(
			parentMap.Id,
			parentMap.Name,
			parentMap.ScaleType,
			parentMap.FeetPerUnit,
			parentFootprintLocalSize,
			parentFootprintPhysicalSize,
			childMap.FeetPerUnit,
			parentMap.FeetPerUnit / childMap.FeetPerUnit,
			expectedChildLocalSize,
			childMap.RealSizeMeters,
			hasMismatch,
			warning);
	}

	public static SizeD ToPhysicalFeet(SizeD localSize, double feetPerUnit)
	{
		ValidateFeetPerUnit(feetPerUnit);
		return new SizeD(localSize.Width * feetPerUnit, localSize.Height * feetPerUnit);
	}

	public static SizeD ToLocalUnits(SizeD physicalSizeFeet, double feetPerUnit)
	{
		ValidateFeetPerUnit(feetPerUnit);
		return new SizeD(physicalSizeFeet.Width / feetPerUnit, physicalSizeFeet.Height / feetPerUnit);
	}

	public static bool HasSizeMismatch(SizeD actualSize, SizeD expectedSize, double tolerance = DefaultSizeTolerance)
	{
		if (tolerance < 0.0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
		{
			throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "Tolerance cannot be negative.");
		}

		return Math.Abs(actualSize.Width - expectedSize.Width) > tolerance
			|| Math.Abs(actualSize.Height - expectedSize.Height) > tolerance;
	}

	private static DistrictShape? FindLinkedDistrict(MapDocument parentMap, Guid childMapId)
	{
		foreach (MapLayer layer in parentMap.Layers)
		{
			foreach (MapObject mapObject in layer.Objects)
			{
				if (mapObject is DistrictShape district && district.ChildMapId == childMapId)
				{
					return district;
				}
			}
		}

		return null;
	}

	private static RectD GetBoundingBox(IReadOnlyList<PointD> points)
	{
		double minX = points[0].X;
		double maxX = points[0].X;
		double minY = points[0].Y;
		double maxY = points[0].Y;
		for (int i = 1; i < points.Count; i++)
		{
			PointD point = points[i];
			minX = Math.Min(minX, point.X);
			maxX = Math.Max(maxX, point.X);
			minY = Math.Min(minY, point.Y);
			maxY = Math.Max(maxY, point.Y);
		}

		return new RectD(minX, minY, maxX - minX, maxY - minY);
	}

	private static void ValidateFeetPerUnit(double feetPerUnit)
	{
		if (feetPerUnit <= 0.0 || double.IsNaN(feetPerUnit) || double.IsInfinity(feetPerUnit))
		{
			throw new ArgumentOutOfRangeException(nameof(feetPerUnit), feetPerUnit, "Feet per unit must be positive.");
		}
	}
}

public sealed record MapScaleDiagnostics(
	MapScaleType ScaleType,
	SizeD LocalSize,
	double FeetPerUnit,
	SizeD RepresentedPhysicalSizeFeet,
	double GridCellLocalUnits,
	double GridCellFeet);

public sealed record ChildMapScaleDiagnostics(
	Guid ParentMapId,
	string ParentMapName,
	MapScaleType ParentScaleType,
	double ParentFeetPerUnit,
	SizeD ParentFootprintLocalSize,
	SizeD ParentFootprintPhysicalSizeFeet,
	double ChildFeetPerUnit,
	double ScaleRatio,
	SizeD ExpectedChildLocalSize,
	SizeD ActualChildLocalSize,
	bool HasMismatch,
	string? Warning);
