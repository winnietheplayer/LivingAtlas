using LivingAtlas.Assets;
using SkiaSharp;

namespace LivingAtlas.Rendering;

public sealed class SkiaTextureImageCache : IDisposable
{
	private readonly Dictionary<string, SKBitmap?> _bitmapsByPath = new(StringComparer.OrdinalIgnoreCase);

	public SKBitmap? Get(TextureAssetCatalog catalog, string? assetId)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		return catalog.TryGetById(assetId, out TextureAssetDefinition asset) ? Get(asset) : null;
	}

	public SKBitmap? Get(TextureAssetDefinition asset)
	{
		ArgumentNullException.ThrowIfNull(asset);
		if (!asset.FileExists || string.IsNullOrWhiteSpace(asset.ResolvedPath) || !File.Exists(asset.ResolvedPath))
		{
			return null;
		}

		if (_bitmapsByPath.TryGetValue(asset.ResolvedPath, out SKBitmap? cachedBitmap))
		{
			return cachedBitmap;
		}

		SKBitmap? bitmap = null;
		try
		{
			bitmap = SKBitmap.Decode(asset.ResolvedPath);
		}
		catch
		{
			bitmap = null;
		}

		_bitmapsByPath[asset.ResolvedPath] = bitmap;
		return bitmap;
	}

	public void Dispose()
	{
		foreach (SKBitmap? bitmap in _bitmapsByPath.Values)
		{
			bitmap?.Dispose();
		}
		_bitmapsByPath.Clear();
	}
}
