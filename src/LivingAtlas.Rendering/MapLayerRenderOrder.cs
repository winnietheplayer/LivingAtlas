using LivingAtlas.Domain.Maps;

namespace LivingAtlas.Rendering;

public enum MapLayerRenderPass
{
	BaseFills,
	EditableObjects
}

public static class MapLayerRenderOrder
{
	public static bool ShouldRenderInPass(MapLayerType layerType, MapLayerRenderPass renderPass)
	{
		bool isBaseLayer = IsBaseFillLayer(layerType);
		return renderPass switch
		{
			MapLayerRenderPass.BaseFills => isBaseLayer,
			MapLayerRenderPass.EditableObjects => !isBaseLayer,
			_ => throw new ArgumentOutOfRangeException(nameof(renderPass), renderPass, "Unknown map layer render pass.")
		};
	}

	public static bool IsBaseFillLayer(MapLayerType layerType)
	{
		return layerType is MapLayerType.Background or MapLayerType.Terrain or MapLayerType.Districts;
	}
}
