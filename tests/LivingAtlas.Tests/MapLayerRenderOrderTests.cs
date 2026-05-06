using LivingAtlas.Domain.Maps;
using LivingAtlas.Rendering;

namespace LivingAtlas.Tests;

public sealed class MapLayerRenderOrderTests
{
	[Theory]
	[InlineData(MapLayerType.Background)]
	[InlineData(MapLayerType.Terrain)]
	[InlineData(MapLayerType.Districts)]
	public void BaseFillPass_IncludesBaseLayers(MapLayerType layerType)
	{
		Assert.True(MapLayerRenderOrder.ShouldRenderInPass(layerType, MapLayerRenderPass.BaseFills));
		Assert.False(MapLayerRenderOrder.ShouldRenderInPass(layerType, MapLayerRenderPass.EditableObjects));
	}

	[Theory]
	[InlineData(MapLayerType.Streets)]
	[InlineData(MapLayerType.Buildings)]
	[InlineData(MapLayerType.PointsOfInterest)]
	[InlineData(MapLayerType.Labels)]
	[InlineData(MapLayerType.Notes)]
	public void EditablePass_IncludesNonBaseLayers(MapLayerType layerType)
	{
		Assert.False(MapLayerRenderOrder.ShouldRenderInPass(layerType, MapLayerRenderPass.BaseFills));
		Assert.True(MapLayerRenderOrder.ShouldRenderInPass(layerType, MapLayerRenderPass.EditableObjects));
	}
}
