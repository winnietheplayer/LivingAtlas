using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LivingAtlas.Assets;

namespace LivingAtlas.Desktop.Services;

public static class TextureAssetCatalogLocator
{
	private const string AssetsDirectoryName = "assets";
	private const string PacksDirectoryName = "packs";

	public static TextureAssetCatalog LoadDefaultCatalog(out IReadOnlyList<string> warnings)
	{
		string? packsRoot = FindDefaultPacksRoot();
		if (packsRoot == null)
		{
			warnings = Array.Empty<string>();
			return TextureAssetCatalog.Empty;
		}

		var loader = new TextureAssetManifestLoader();
		var assets = new List<TextureAssetDefinition>();
		var warningList = new List<string>();
		var ids = new HashSet<string>(StringComparer.Ordinal);

		foreach (string packDirectory in Directory.EnumerateDirectories(packsRoot).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
		{
			string manifestPath = Path.Combine(packDirectory, "manifest.json");
			if (!File.Exists(manifestPath))
			{
				continue;
			}

			try
			{
				TextureAssetCatalog packCatalog = loader.LoadPack(packDirectory);
				foreach (TextureAssetDefinition asset in packCatalog.Assets)
				{
					if (!ids.Add(asset.Id))
					{
						warningList.Add($"Texture asset id '{asset.Id}' is duplicated and was skipped.");
						continue;
					}
					assets.Add(asset);
				}
				warningList.AddRange(packCatalog.Warnings);
			}
			catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
			{
				warningList.Add($"Texture pack '{Path.GetFileName(packDirectory)}' was skipped: {ex.Message}");
			}
		}

		warnings = warningList;
		return new TextureAssetCatalog(assets, warningList);
	}

	private static string? FindDefaultPacksRoot()
	{
		foreach (string startDirectory in GetProbeStartDirectories())
		{
			string? packsRoot = FindPacksRootUpward(startDirectory);
			if (packsRoot != null)
			{
				return packsRoot;
			}
		}

		return null;
	}

	private static IEnumerable<string> GetProbeStartDirectories()
	{
		var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string candidate in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
		{
			if (string.IsNullOrWhiteSpace(candidate))
			{
				continue;
			}

			string fullPath = Path.GetFullPath(candidate);
			if (yielded.Add(fullPath))
			{
				yield return fullPath;
			}
		}
	}

	private static string? FindPacksRootUpward(string startDirectory)
	{
		DirectoryInfo? directory = new DirectoryInfo(startDirectory);
		while (directory != null)
		{
			string candidate = Path.Combine(directory.FullName, AssetsDirectoryName, PacksDirectoryName);
			if (Directory.Exists(candidate))
			{
				return candidate;
			}
			directory = directory.Parent;
		}

		return null;
	}
}
