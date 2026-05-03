using System;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class BreadcrumbItemViewModel
{
	public string Name { get; }

	public Guid? MapId { get; }

	public bool IsActiveMap { get; }

	public bool CanOpen => MapId.HasValue;

	public string DisplayText { get; }

	public BreadcrumbItemViewModel(string name, Guid? mapId, bool isActiveMap, int index)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Breadcrumb name cannot be empty.", "name");
		}
		if (mapId == Guid.Empty)
		{
			throw new ArgumentException("Map id cannot be empty.", "mapId");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", index, "Breadcrumb index cannot be negative.");
		}
		Name = name;
		MapId = mapId;
		IsActiveMap = isActiveMap;
		DisplayText = ((index == 0) ? name : ("> " + name));
	}
}
