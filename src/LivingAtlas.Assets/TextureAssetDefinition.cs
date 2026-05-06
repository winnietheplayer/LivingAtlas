namespace LivingAtlas.Assets;

public sealed record TextureAssetDefinition(
	string PackId,
	string Id,
	string Name,
	string Kind,
	string Category,
	string RelativePath,
	string ResolvedPath,
	bool IsTileable,
	double DefaultTileSizeMeters,
	IReadOnlyList<string> Tags,
	bool FileExists);
