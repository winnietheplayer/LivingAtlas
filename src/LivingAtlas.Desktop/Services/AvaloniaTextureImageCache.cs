using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using LivingAtlas.Assets;

namespace LivingAtlas.Desktop.Services;

public sealed class AvaloniaTextureImageCache : IDisposable
{
	private readonly Dictionary<string, Bitmap?> _bitmapsByPath = new(StringComparer.OrdinalIgnoreCase);

	public Bitmap? Get(TextureAssetCatalog catalog, string? assetId)
	{
		ArgumentNullException.ThrowIfNull(catalog);
		return catalog.TryGetById(assetId, out TextureAssetDefinition asset) ? Get(asset) : null;
	}

	public Bitmap? Get(TextureAssetDefinition asset)
	{
		ArgumentNullException.ThrowIfNull(asset);
		if (!asset.FileExists || string.IsNullOrWhiteSpace(asset.ResolvedPath) || !File.Exists(asset.ResolvedPath))
		{
			return null;
		}

		if (_bitmapsByPath.TryGetValue(asset.ResolvedPath, out Bitmap? cachedBitmap))
		{
			return cachedBitmap;
		}

		Bitmap? bitmap = null;
		try
		{
			bitmap = new Bitmap(asset.ResolvedPath);
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
		foreach (Bitmap? bitmap in _bitmapsByPath.Values)
		{
			bitmap?.Dispose();
		}
		_bitmapsByPath.Clear();
	}
}
