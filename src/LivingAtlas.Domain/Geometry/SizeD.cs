using System;

namespace LivingAtlas.Domain.Geometry;

public readonly record struct SizeD
{
	public double Width { get; }

	public double Height { get; }

	public SizeD(double width, double height)
	{
		if (width < 0.0)
		{
			throw new ArgumentOutOfRangeException("width", width, "Width cannot be negative.");
		}
		if (height < 0.0)
		{
			throw new ArgumentOutOfRangeException("height", height, "Height cannot be negative.");
		}
		Width = width;
		Height = height;
	}
}
