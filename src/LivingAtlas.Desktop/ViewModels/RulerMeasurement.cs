using System;
using System.Globalization;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Desktop.ViewModels;

public sealed record RulerMeasurement(PointD Start, PointD End)
{
	public double DeltaX => End.X - Start.X;

	public double DeltaY => End.Y - Start.Y;

	public double DistanceMeters => Math.Sqrt((DeltaX * DeltaX) + (DeltaY * DeltaY));

	public string FormatStatus()
	{
		return string.Create(
			CultureInfo.InvariantCulture,
			$"Distance: {DistanceMeters:F1} m | \u0394X: {DeltaX:F1} m | \u0394Y: {DeltaY:F1} m");
	}
}
