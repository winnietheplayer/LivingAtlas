using System;

namespace LivingAtlas.Rendering;

public static class TextureTileScale
{
	public const double MinimumTiledLocalUnits = 0.25;

	public static double ToLocalUnits(double textureTileSizeFeet, double feetPerUnit)
	{
		if (textureTileSizeFeet <= 0.0 || double.IsNaN(textureTileSizeFeet) || double.IsInfinity(textureTileSizeFeet))
		{
			throw new ArgumentOutOfRangeException(nameof(textureTileSizeFeet), textureTileSizeFeet, "Texture tile size must be positive.");
		}
		if (feetPerUnit <= 0.0 || double.IsNaN(feetPerUnit) || double.IsInfinity(feetPerUnit))
		{
			throw new ArgumentOutOfRangeException(nameof(feetPerUnit), feetPerUnit, "Feet per unit must be positive.");
		}

		return textureTileSizeFeet / feetPerUnit;
	}

	public static bool TryGetRenderableLocalUnits(double textureTileSizeFeet, double feetPerUnit, out double tileLocalUnits)
	{
		tileLocalUnits = 0.0;
		if (textureTileSizeFeet <= 0.0
			|| double.IsNaN(textureTileSizeFeet)
			|| double.IsInfinity(textureTileSizeFeet)
			|| feetPerUnit <= 0.0
			|| double.IsNaN(feetPerUnit)
			|| double.IsInfinity(feetPerUnit))
		{
			return false;
		}

		tileLocalUnits = textureTileSizeFeet / feetPerUnit;
		return tileLocalUnits >= MinimumTiledLocalUnits;
	}
}
