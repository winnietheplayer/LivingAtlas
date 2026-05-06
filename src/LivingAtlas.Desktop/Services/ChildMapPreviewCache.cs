using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using LivingAtlas.Assets;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Export;

namespace LivingAtlas.Desktop.Services;

public sealed class ChildMapPreviewCache : IDisposable
{
	public const int DefaultMaxPreviewLongSidePixels = 512;

	private readonly Dictionary<Guid, ChildMapPreviewCacheEntry> _entries = new Dictionary<Guid, ChildMapPreviewCacheEntry>();

	private readonly PngMapExporter _exporter = new PngMapExporter();

	public ChildMapPreviewCacheEntry? GetOrCreate(
		CampaignMapProject project,
		Guid childMapId,
		TextureAssetCatalog textureAssetCatalog)
	{
		ArgumentNullException.ThrowIfNull(project);
		if (_entries.TryGetValue(childMapId, out ChildMapPreviewCacheEntry? existingEntry))
		{
			return existingEntry;
		}

		MapDocument? childMap = project.FindMap(childMapId);
		if (childMap == null)
		{
			return null;
		}

		try
		{
			byte[] pngBytes = _exporter.RenderPreviewPng(
				project,
				childMap,
				DefaultMaxPreviewLongSidePixels,
				textureAssetCatalog);
			using var stream = new MemoryStream(pngBytes);
			Bitmap? bitmap = TryCreateBitmap(stream);
			PixelSize pixelSize = CalculatePreviewPixelSize(childMap.RealSizeMeters);
			var entry = new ChildMapPreviewCacheEntry(
				childMap.Id,
				pngBytes,
				pixelSize,
				bitmap,
				childMap.RealSizeMeters,
				DateTimeOffset.UtcNow);
			_entries.Add(childMap.Id, entry);
			return entry;
		}
		catch
		{
			return null;
		}
	}

	public void Invalidate(Guid childMapId)
	{
		if (_entries.Remove(childMapId, out ChildMapPreviewCacheEntry? entry))
		{
			entry.Dispose();
		}
	}

	public void Clear()
	{
		foreach (ChildMapPreviewCacheEntry entry in _entries.Values)
		{
			entry.Dispose();
		}
		_entries.Clear();
	}

	public void Dispose()
	{
		Clear();
	}

	private static Bitmap? TryCreateBitmap(Stream stream)
	{
		try
		{
			return new Bitmap(stream);
		}
		catch
		{
			return null;
		}
	}

	private static PixelSize CalculatePreviewPixelSize(SizeD mapSize)
	{
		double longestSide = Math.Max(mapSize.Width, mapSize.Height);
		double scale = Math.Min(1.0, DefaultMaxPreviewLongSidePixels / longestSide);
		return new PixelSize(
			Math.Max(1, (int)Math.Ceiling(mapSize.Width * scale)),
			Math.Max(1, (int)Math.Ceiling(mapSize.Height * scale)));
	}
}

public sealed class ChildMapPreviewCacheEntry : IDisposable
{
	public ChildMapPreviewCacheEntry(Guid childMapId, byte[] pngBytes, PixelSize pixelSize, Bitmap? bitmap, SizeD childMapSize, DateTimeOffset generatedAt)
	{
		ChildMapId = childMapId;
		PngBytes = pngBytes;
		PixelSize = pixelSize;
		Bitmap = bitmap;
		ChildMapSize = childMapSize;
		GeneratedAt = generatedAt;
	}

	public Guid ChildMapId { get; }

	public byte[] PngBytes { get; }

	public PixelSize PixelSize { get; }

	public Bitmap? Bitmap { get; }

	public SizeD ChildMapSize { get; }

	public DateTimeOffset GeneratedAt { get; }

	public void Dispose()
	{
		Bitmap?.Dispose();
	}
}
