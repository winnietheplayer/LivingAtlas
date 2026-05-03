using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class ProjectTreeItemViewModel
{
	public string Name { get; }

	public string DisplayName { get; }

	public Guid? MapId { get; }

	public bool IsActive { get; }

	public IReadOnlyList<ProjectTreeItemViewModel> Children { get; }

	public ProjectTreeItemViewModel(string name, Guid? mapId, bool isActive = false, IEnumerable<ProjectTreeItemViewModel>? children = null)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Tree item name cannot be empty.", "name");
		}
		if (mapId == Guid.Empty)
		{
			throw new ArgumentException("Map id cannot be empty.", "mapId");
		}
		Name = name;
		MapId = mapId;
		IsActive = isActive;
		Children = children?.ToList() ?? new List<ProjectTreeItemViewModel>();
		DisplayName = (isActive ? (name + " (active)") : name);
	}
}
