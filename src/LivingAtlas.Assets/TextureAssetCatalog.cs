namespace LivingAtlas.Assets;

public sealed class TextureAssetCatalog
{
	private readonly Dictionary<string, TextureAssetDefinition> _assetsById;

	public static TextureAssetCatalog Empty { get; } = new TextureAssetCatalog(Array.Empty<TextureAssetDefinition>(), Array.Empty<string>());

	public IReadOnlyList<TextureAssetDefinition> Assets { get; }

	public IReadOnlyList<string> Warnings { get; }

	public IReadOnlyList<TextureAssetDefinition> Textures { get; }

	public TextureAssetCatalog(IEnumerable<TextureAssetDefinition> assets, IEnumerable<string>? warnings = null)
	{
		ArgumentNullException.ThrowIfNull(assets);
		List<TextureAssetDefinition> assetList = assets.ToList();
		_assetsById = new Dictionary<string, TextureAssetDefinition>(StringComparer.Ordinal);
		foreach (TextureAssetDefinition asset in assetList)
		{
			if (_assetsById.ContainsKey(asset.Id))
			{
				throw new InvalidDataException($"Texture asset id '{asset.Id}' is duplicated.");
			}
			_assetsById.Add(asset.Id, asset);
		}

		Assets = assetList;
		Textures = assetList
			.Where(asset => string.Equals(asset.Kind, "texture", StringComparison.OrdinalIgnoreCase))
			.ToList();
		Warnings = (warnings ?? Array.Empty<string>()).ToList();
	}

	public bool TryGetById(string? assetId, out TextureAssetDefinition asset)
	{
		if (!string.IsNullOrWhiteSpace(assetId) && _assetsById.TryGetValue(assetId, out TextureAssetDefinition? foundAsset))
		{
			asset = foundAsset;
			return true;
		}

		asset = null!;
		return false;
	}
}
