using LivingAtlas.Assets;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Export;
using LivingAtlas.Rendering;
using SkiaSharp;

namespace LivingAtlas.Tests;

public sealed class TextureImageCacheTests
{
	[Fact]
	public void SkiaTextureImageCache_ResolvesNonPowerOfTwoTexturePath()
	{
		string tempDirectory = CreateTempDirectory();
		try
		{
			string texturePath = CreateTestTexture(tempDirectory);
			TextureAssetCatalog catalog = CreateCatalog(texturePath);
			using var cache = new SkiaTextureImageCache();

			SKBitmap? bitmap = cache.Get(catalog, "ground.test.01");

			Assert.NotNull(bitmap);
			Assert.Equal(3, bitmap.Width);
			Assert.Equal(5, bitmap.Height);
		}
		finally
		{
			DeleteDirectory(tempDirectory);
		}
	}

	[Fact]
	public void SkiaTextureImageCache_MissingTextureReturnsNull()
	{
		TextureAssetCatalog catalog = CreateCatalog(Path.Combine(Path.GetTempPath(), "missing-texture.png"), fileExists: false);
		using var cache = new SkiaTextureImageCache();

		Assert.Null(cache.Get(catalog, "ground.test.01"));
		Assert.Null(cache.Get(catalog, "ground.unknown"));
	}

	[Fact]
	public void PngExport_WithNonPowerOfTwoTexturedDistrictCreatesValidPng()
	{
		string tempDirectory = CreateTempDirectory();
		string exportPath = Path.Combine(tempDirectory, "textured-district.png");
		try
		{
			string texturePath = CreateTestTexture(tempDirectory);
			TextureAssetCatalog catalog = CreateCatalog(texturePath);
			MapDocument map = CreateTexturedDistrictMap();
			CampaignMapProject project = new CampaignMapProject(Guid.NewGuid(), "Textured Export", map.Id, new[] { map });

			new PngMapExporter().Export(project, map, new PngExportOptions(exportPath), catalog);

			Assert.True(File.Exists(exportPath));
			using SKBitmap bitmap = SKBitmap.Decode(exportPath);
			Assert.Equal(32, bitmap.Width);
			Assert.Equal(32, bitmap.Height);
		}
		finally
		{
			DeleteDirectory(tempDirectory);
		}
	}

	private static MapDocument CreateTexturedDistrictMap()
	{
		var map = new MapDocument(
			Guid.NewGuid(),
			"Texture Map",
			MapScaleType.City,
			new SizeD(32.0, 32.0),
			gridSettings: new GridSettings(isEnabled: true, cellSizeMeters: 8.0, showGrid: false, snapToGrid: false));
		var layer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
		var district = new DistrictShape(
			Guid.NewGuid(),
			"Textured District",
			layer.Id,
			new[]
			{
				new PointD(4.0, 4.0),
				new PointD(28.0, 4.0),
				new PointD(28.0, 28.0),
				new PointD(4.0, 28.0)
			},
			styleKey: "district.default");
		district.SetTextureFill("ground.test.01", 4.0);
		layer.AddObject(district);
		map.AddLayer(layer);
		return map;
	}

	private static TextureAssetCatalog CreateCatalog(string texturePath, bool fileExists = true)
	{
		return new TextureAssetCatalog(new[]
		{
			new TextureAssetDefinition(
				"test_pack",
				"ground.test.01",
				"Test Ground",
				"texture",
				"ground",
				"textures/ground/test.png",
				texturePath,
				IsTileable: true,
				DefaultTileSizeMeters: 4.0,
				Tags: Array.Empty<string>(),
				FileExists: fileExists)
		});
	}

	private static string CreateTestTexture(string directory)
	{
		string path = Path.Combine(directory, "test-texture.png");
		using var bitmap = new SKBitmap(3, 5);
		for (int y = 0; y < bitmap.Height; y++)
		{
			for (int x = 0; x < bitmap.Width; x++)
			{
				bitmap.SetPixel(x, y, (x + y) % 2 == 0 ? SKColors.Red : SKColors.Green);
			}
		}
		using SKImage image = SKImage.FromBitmap(bitmap);
		using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
		using FileStream stream = File.Create(path);
		data.SaveTo(stream);
		return path;
	}

	private static string CreateTempDirectory()
	{
		string path = Path.Combine(Path.GetTempPath(), "living-atlas-textures-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private static void DeleteDirectory(string path)
	{
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
	}
}
