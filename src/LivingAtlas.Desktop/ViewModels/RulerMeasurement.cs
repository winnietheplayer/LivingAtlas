using System;
using System.Globalization;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Desktop.ViewModels;

public sealed record RulerMeasurement(PointD Start, PointD End)
{
	public double DeltaX => End.X - Start.X;

	public double DeltaY => End.Y - Start.Y;

	public double DistanceLocalUnits => Math.Sqrt((DeltaX * DeltaX) + (DeltaY * DeltaY));

	public double GetDistanceFeet(double feetPerUnit)
	{
		return DistanceLocalUnits * feetPerUnit;
	}

	public double GetDeltaXFeet(double feetPerUnit)
	{
		return DeltaX * feetPerUnit;
	}

	public double GetDeltaYFeet(double feetPerUnit)
	{
		return DeltaY * feetPerUnit;
	}

	public double GetBattleSquares(double feetPerUnit)
	{
		return GetDistanceFeet(feetPerUnit) / 5.0;
	}

	public string FormatStatus(double feetPerUnit)
	{
		return string.Create(
			CultureInfo.InvariantCulture,
			$"Distance: {GetDistanceFeet(feetPerUnit):F1} ft | {GetBattleSquares(feetPerUnit):F1} battle squares | \u0394X: {GetDeltaXFeet(feetPerUnit):F1} ft | \u0394Y: {GetDeltaYFeet(feetPerUnit):F1} ft");
	}
}
