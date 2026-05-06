using LivingAtlas.Rendering;

namespace LivingAtlas.Tests;

public sealed class ChildPreviewLodTests
{
	[Fact]
	public void ShouldRenderPreview_ReturnsTrueAboveThreshold()
	{
		Assert.True(ChildPreviewLod.ShouldRenderPreview(48.0, 24.0));
	}

	[Theory]
	[InlineData(23.9, 64.0)]
	[InlineData(64.0, 23.9)]
	public void ShouldRenderPreview_ReturnsFalseBelowThreshold(double width, double height)
	{
		Assert.False(ChildPreviewLod.ShouldRenderPreview(width, height));
	}

	[Fact]
	public void ShouldRenderPreview_ReturnsFalseWhenDisabled()
	{
		Assert.False(ChildPreviewLod.ShouldRenderPreview(128.0, 128.0, previewsEnabled: false));
	}
}
