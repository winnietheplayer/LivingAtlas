namespace LivingAtlas.Assets;

public sealed class TextureAssetManifest
{
	public string Id { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;

	public string Version { get; init; } = string.Empty;

	public IReadOnlyList<TextureAssetManifestEntry> Assets { get; init; } = Array.Empty<TextureAssetManifestEntry>();
}

public sealed class TextureAssetManifestEntry
{
	public string Id { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;

	public string Kind { get; init; } = string.Empty;

	public string Category { get; init; } = string.Empty;

	public string File { get; init; } = string.Empty;

	public bool IsTileable { get; init; }

	public double DefaultTileSizeMeters { get; init; } = 10.0;

	public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
