using System.Text.Json;

namespace LivingAtlas.Assets;

public sealed class TextureAssetManifestLoader
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public TextureAssetCatalog LoadCatalog(string packsRootPath)
	{
		if (string.IsNullOrWhiteSpace(packsRootPath))
		{
			throw new ArgumentException("Packs root path cannot be empty.", nameof(packsRootPath));
		}

		string fullRootPath = Path.GetFullPath(packsRootPath);
		if (!Directory.Exists(fullRootPath))
		{
			return TextureAssetCatalog.Empty;
		}

		var assets = new List<TextureAssetDefinition>();
		var warnings = new List<string>();
		var ids = new HashSet<string>(StringComparer.Ordinal);
		foreach (string packDirectory in Directory.EnumerateDirectories(fullRootPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
		{
			string manifestPath = Path.Combine(packDirectory, "manifest.json");
			if (!File.Exists(manifestPath))
			{
				continue;
			}

			TextureAssetCatalog packCatalog = LoadPack(packDirectory);
			foreach (TextureAssetDefinition asset in packCatalog.Assets)
			{
				if (!ids.Add(asset.Id))
				{
					throw new InvalidDataException($"Texture asset id '{asset.Id}' is duplicated.");
				}
				assets.Add(asset);
			}
			warnings.AddRange(packCatalog.Warnings);
		}

		return new TextureAssetCatalog(assets, warnings);
	}

	public TextureAssetCatalog LoadPack(string packDirectory)
	{
		if (string.IsNullOrWhiteSpace(packDirectory))
		{
			throw new ArgumentException("Pack directory cannot be empty.", nameof(packDirectory));
		}

		string fullPackDirectory = Path.GetFullPath(packDirectory);
		string manifestPath = Path.Combine(fullPackDirectory, "manifest.json");
		if (!File.Exists(manifestPath))
		{
			throw new FileNotFoundException("Texture asset manifest was not found.", manifestPath);
		}

		using FileStream stream = File.OpenRead(manifestPath);
		TextureAssetManifest manifest = JsonSerializer.Deserialize<TextureAssetManifest>(stream, JsonOptions)
			?? throw new InvalidDataException($"Texture asset manifest '{manifestPath}' is empty or invalid.");
		return CreateCatalog(fullPackDirectory, manifest);
	}

	public TextureAssetCatalog LoadManifest(string manifestPath)
	{
		if (string.IsNullOrWhiteSpace(manifestPath))
		{
			throw new ArgumentException("Manifest path cannot be empty.", nameof(manifestPath));
		}

		string fullManifestPath = Path.GetFullPath(manifestPath);
		string packDirectory = Path.GetDirectoryName(fullManifestPath)
			?? throw new InvalidDataException($"Texture asset manifest '{manifestPath}' has no parent directory.");
		using FileStream stream = File.OpenRead(fullManifestPath);
		TextureAssetManifest manifest = JsonSerializer.Deserialize<TextureAssetManifest>(stream, JsonOptions)
			?? throw new InvalidDataException($"Texture asset manifest '{manifestPath}' is empty or invalid.");
		return CreateCatalog(packDirectory, manifest);
	}

	private static TextureAssetCatalog CreateCatalog(string packDirectory, TextureAssetManifest manifest)
	{
		if (string.IsNullOrWhiteSpace(manifest.Id))
		{
			throw new InvalidDataException("Texture asset manifest pack id cannot be empty.");
		}

		var assets = new List<TextureAssetDefinition>();
		var warnings = new List<string>();
		var ids = new HashSet<string>(StringComparer.Ordinal);
		foreach (TextureAssetManifestEntry entry in manifest.Assets)
		{
			if (string.IsNullOrWhiteSpace(entry.Id))
			{
				throw new InvalidDataException($"Texture asset manifest '{manifest.Id}' contains an asset with an empty id.");
			}
			if (!ids.Add(entry.Id))
			{
				throw new InvalidDataException($"Texture asset id '{entry.Id}' is duplicated.");
			}
			if (string.IsNullOrWhiteSpace(entry.File))
			{
				throw new InvalidDataException($"Texture asset '{entry.Id}' file cannot be empty.");
			}

			string relativePath = NormalizeRelativePath(entry.File);
			string resolvedPath = ResolveAssetPath(packDirectory, relativePath);
			bool fileExists = File.Exists(resolvedPath);
			if (!fileExists)
			{
				warnings.Add($"Texture asset '{entry.Id}' file was not found: {relativePath}");
			}

			assets.Add(new TextureAssetDefinition(
				manifest.Id.Trim(),
				entry.Id.Trim(),
				string.IsNullOrWhiteSpace(entry.Name) ? entry.Id.Trim() : entry.Name.Trim(),
				string.IsNullOrWhiteSpace(entry.Kind) ? "texture" : entry.Kind.Trim(),
				entry.Category?.Trim() ?? string.Empty,
				relativePath,
				resolvedPath,
				entry.IsTileable,
				entry.DefaultTileSizeMeters,
				NormalizeTags(entry.Tags),
				fileExists));
		}

		return new TextureAssetCatalog(assets, warnings);
	}

	private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
	{
		if (tags == null)
		{
			return Array.Empty<string>();
		}

		return tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList();
	}

	private static string NormalizeRelativePath(string relativePath)
	{
		string trimmed = relativePath.Trim().Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
		if (Path.IsPathRooted(trimmed))
		{
			throw new InvalidDataException($"Texture asset path '{relativePath}' must be relative.");
		}

		return trimmed;
	}

	private static string ResolveAssetPath(string packDirectory, string relativePath)
	{
		string fullPackDirectory = Path.GetFullPath(packDirectory);
		string resolvedPath = Path.GetFullPath(Path.Combine(fullPackDirectory, relativePath));
		string requiredPrefix = fullPackDirectory.EndsWith(Path.DirectorySeparatorChar)
			? fullPackDirectory
			: fullPackDirectory + Path.DirectorySeparatorChar;
		if (!resolvedPath.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException($"Texture asset path '{relativePath}' must not escape the pack directory.");
		}

		return resolvedPath;
	}
}
