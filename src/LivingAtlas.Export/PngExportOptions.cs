namespace LivingAtlas.Export;

public sealed record PngExportOptions(string OutputPath)
{
	public const int MaxDimensionPixels = 8192;

	public int ResolutionScale { get; init; } = 1;

	public bool IncludeGrid { get; init; }

	public bool IncludeChildMapPreviews { get; init; }

	public bool TransparentBackground { get; init; }

	public bool IncludeLabels { get; init; } = true;

	public bool IncludePointsOfInterest { get; init; } = true;
}

public readonly record struct PngExportImageSize(int Width, int Height);
