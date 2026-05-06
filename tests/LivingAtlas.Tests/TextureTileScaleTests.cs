using LivingAtlas.Rendering;

namespace LivingAtlas.Tests;

public sealed class TextureTileScaleTests
{
	[Fact]
	public void ToLocalUnits_ConvertsPhysicalFeetUsingMapFeetPerUnit()
	{
		Assert.Equal(2.0, TextureTileScale.ToLocalUnits(textureTileSizeFeet: 20.0, feetPerUnit: 10.0));
		Assert.Equal(0.2, TextureTileScale.ToLocalUnits(textureTileSizeFeet: 20.0, feetPerUnit: 100.0), precision: 9);
		Assert.Equal(4.0, TextureTileScale.ToLocalUnits(textureTileSizeFeet: 20.0, feetPerUnit: 5.0));
	}

	[Fact]
	public void TryGetRenderableLocalUnits_ReturnsFalseBelowThreshold()
	{
		bool renderable = TextureTileScale.TryGetRenderableLocalUnits(
			textureTileSizeFeet: 20.0,
			feetPerUnit: 100.0,
			out double tileLocalUnits);

		Assert.False(renderable);
		Assert.Equal(0.2, tileLocalUnits, precision: 9);
	}

	[Fact]
	public void TryGetRenderableLocalUnits_ReturnsTrueAtThreshold()
	{
		bool renderable = TextureTileScale.TryGetRenderableLocalUnits(
			textureTileSizeFeet: 25.0,
			feetPerUnit: 100.0,
			out double tileLocalUnits);

		Assert.True(renderable);
		Assert.Equal(TextureTileScale.MinimumTiledLocalUnits, tileLocalUnits);
	}

	[Theory]
	[InlineData(0.0, 10.0)]
	[InlineData(10.0, 0.0)]
	[InlineData(double.NaN, 10.0)]
	[InlineData(10.0, double.PositiveInfinity)]
	public void ToLocalUnits_ThrowsForInvalidValues(double textureTileSizeFeet, double feetPerUnit)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => TextureTileScale.ToLocalUnits(textureTileSizeFeet, feetPerUnit));
	}
}
