namespace LivingAtlas.Desktop.ViewModels;

public sealed record TextureAssetOptionViewModel(
	string DisplayName,
	string? AssetId,
	double DefaultTileSizeMeters);
