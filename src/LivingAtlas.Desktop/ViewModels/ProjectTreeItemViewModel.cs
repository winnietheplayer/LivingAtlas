using System;
using System.Collections.Generic;
using System.Linq;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class ProjectTreeItemViewModel : ViewModelBase
{
	private bool _isVisible;

	private bool _isLocked;

	public string Name { get; }

	public string DisplayName { get; }

	public Guid? MapId { get; }

	public Guid? LayerId { get; }

	public bool IsLayer => LayerId.HasValue;

	public bool IsActive { get; }

	public bool IsActiveTarget { get; }

	public bool IsVisible
	{
		get => _isVisible;
		set
		{
			if (SetProperty(ref _isVisible, value))
			{
				VisibilityChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public bool IsLocked
	{
		get => _isLocked;
		set
		{
			if (SetProperty(ref _isLocked, value))
			{
				LockChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public IReadOnlyList<ProjectTreeItemViewModel> Children { get; }

	public event EventHandler? VisibilityChanged;

	public event EventHandler? LockChanged;

	public ProjectTreeItemViewModel(
		string name, 
		Guid? mapId = null, 
		Guid? layerId = null,
		bool isActive = false, 
		bool isVisible = true,
		bool isLocked = false,
		bool isActiveTarget = false,
		IEnumerable<ProjectTreeItemViewModel>? children = null)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Tree item name cannot be empty.", "name");
		}
		if (mapId == Guid.Empty)
		{
			throw new ArgumentException("Map id cannot be empty.", "mapId");
		}
		if (layerId == Guid.Empty)
		{
			throw new ArgumentException("Layer id cannot be empty.", "layerId");
		}
		Name = name;
		MapId = mapId;
		LayerId = layerId;
		IsActive = isActive;
		IsActiveTarget = isActiveTarget;
		_isVisible = isVisible;
		_isLocked = isLocked;
		Children = children?.ToList() ?? new List<ProjectTreeItemViewModel>();
		
		if (IsLayer)
		{
			DisplayName = (isActiveTarget ? (name + " (active)") : name);
		}
		else
		{
			DisplayName = (isActive ? (name + " (active)") : name);
		}
	}
}
