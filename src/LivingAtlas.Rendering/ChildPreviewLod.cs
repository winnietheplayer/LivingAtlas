namespace LivingAtlas.Rendering;

public static class ChildPreviewLod
{
	public const double MinimumPreviewScreenPixels = 24.0;

	public static bool ShouldRenderPreview(double screenWidth, double screenHeight, bool previewsEnabled = true)
	{
		if (!previewsEnabled)
		{
			return false;
		}
		if (double.IsNaN(screenWidth) || double.IsNaN(screenHeight) || double.IsInfinity(screenWidth) || double.IsInfinity(screenHeight))
		{
			return false;
		}

		return Math.Abs(screenWidth) >= MinimumPreviewScreenPixels
			&& Math.Abs(screenHeight) >= MinimumPreviewScreenPixels;
	}
}
