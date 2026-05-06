using LivingAtlas.Assets;
using Xunit;

namespace LivingAtlas.Tests;

public sealed class TextureAssetManifestLoaderTests : IDisposable
{
	private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "living-atlas-assets-tests-" + Guid.NewGuid().ToString("N"));

	public TextureAssetManifestLoaderTests()
	{
		Directory.CreateDirectory(_tempRoot);
	}

	public void Dispose()
	{
		if (Directory.Exists(_tempRoot))
		{
			Directory.Delete(_tempRoot, recursive: true);
		}
	}

	[Fact]
	public void LoadPack_ParsesManifestAndResolvesRelativePath()
	{
		string packDirectory = CreatePack(
			"base_terrain_01",
			"""
			{
			  "id": "base_terrain_01",
			  "name": "Base Terrain Pack 01",
			  "version": "1.0.0",
			  "assets": [
			    {
			      "id": "ground.dirt.01",
			      "name": "Dirt 01",
			      "kind": "texture",
			      "category": "ground",
			      "file": "textures/ground/ground_dirt_01.png",
			      "isTileable": true,
			      "defaultTileSizeMeters": 10.0,
			      "tags": ["ground", "dirt", "outdoor"]
			    }
			  ]
			}
			""");
		string texturePath = Path.Combine(packDirectory, "textures", "ground", "ground_dirt_01.png");
		Directory.CreateDirectory(Path.GetDirectoryName(texturePath)!);
		File.WriteAllBytes(texturePath, new byte[] { 1, 2, 3 });

		TextureAssetCatalog catalog = new TextureAssetManifestLoader().LoadPack(packDirectory);

		TextureAssetDefinition asset = Assert.Single(catalog.Assets);
		Assert.Equal("base_terrain_01", asset.PackId);
		Assert.Equal("ground.dirt.01", asset.Id);
		Assert.Equal("Dirt 01", asset.Name);
		Assert.Equal("texture", asset.Kind);
		Assert.Equal("ground", asset.Category);
		Assert.Equal(Path.GetFullPath(texturePath), asset.ResolvedPath);
		Assert.True(asset.FileExists);
		Assert.True(asset.IsTileable);
		Assert.Equal(10.0, asset.DefaultTileSizeMeters);
		Assert.Contains("dirt", asset.Tags);
		Assert.Empty(catalog.Warnings);
	}

	[Fact]
	public void LoadCatalog_RejectsDuplicateAssetIdsAcrossPacks()
	{
		string packsRoot = Path.Combine(_tempRoot, "packs");
		CreatePack("pack_a", CreateManifest("pack_a", "ground.dirt.01", "textures/ground/a.png"), packsRoot);
		CreatePack("pack_b", CreateManifest("pack_b", "ground.dirt.01", "textures/ground/b.png"), packsRoot);

		Assert.Throws<InvalidDataException>(() => new TextureAssetManifestLoader().LoadCatalog(packsRoot));
	}

	[Fact]
	public void LoadPack_RejectsRelativePathsThatEscapePackDirectory()
	{
		string packDirectory = CreatePack("pack_escape", CreateManifest("pack_escape", "bad.texture", "../escape.png"));

		Assert.Throws<InvalidDataException>(() => new TextureAssetManifestLoader().LoadPack(packDirectory));
	}

	[Fact]
	public void LoadPack_ReportsMissingTextureFilesAsWarnings()
	{
		string packDirectory = CreatePack("pack_missing", CreateManifest("pack_missing", "missing.texture", "textures/missing.png"));

		TextureAssetCatalog catalog = new TextureAssetManifestLoader().LoadPack(packDirectory);

		TextureAssetDefinition asset = Assert.Single(catalog.Assets);
		Assert.False(asset.FileExists);
		Assert.Single(catalog.Warnings);
		Assert.Contains("missing.texture", catalog.Warnings[0]);
	}

	[Fact]
	public void LoadCatalog_MissingRootReturnsEmptyCatalog()
	{
		string missingRoot = Path.Combine(_tempRoot, "missing-packs");

		TextureAssetCatalog catalog = new TextureAssetManifestLoader().LoadCatalog(missingRoot);

		Assert.Empty(catalog.Assets);
		Assert.Empty(catalog.Textures);
		Assert.Empty(catalog.Warnings);
	}

	private string CreatePack(string packId, string manifestJson, string? packsRoot = null)
	{
		string root = packsRoot ?? _tempRoot;
		string packDirectory = Path.Combine(root, packId);
		Directory.CreateDirectory(packDirectory);
		File.WriteAllText(Path.Combine(packDirectory, "manifest.json"), manifestJson);
		return packDirectory;
	}

	private static string CreateManifest(string packId, string assetId, string file)
	{
		return $$"""
		{
		  "id": "{{packId}}",
		  "name": "{{packId}}",
		  "version": "1.0.0",
		  "assets": [
		    {
		      "id": "{{assetId}}",
		      "name": "{{assetId}}",
		      "kind": "texture",
		      "category": "ground",
		      "file": "{{file}}",
		      "isTileable": true,
		      "defaultTileSizeMeters": 10.0,
		      "tags": []
		    }
		  ]
		}
		""";
	}
}
