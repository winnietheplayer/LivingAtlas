using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LivingAtlas.Desktop.Services;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Commands;
using LivingAtlas.Editor.Viewport;
using LivingAtlas.Editor.Navigation;
using LivingAtlas.ProjectSystem;

namespace LivingAtlas.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	private CampaignMapProject _project = null!;

	private ProjectTreeViewModel _projectTree = null!;

	private IReadOnlyList<BreadcrumbItemViewModel> _breadcrumbs = Array.Empty<BreadcrumbItemViewModel>();

	private MapViewportViewModel _mapViewport = null!;

	private InspectorViewModel _inspector = null!;

	private string? _currentProjectPath;

	private bool _isDirty;

	private readonly Dictionary<Guid, MapViewportViewModel> _mapViewportsByMapId = new Dictionary<Guid, MapViewportViewModel>();

	private readonly CameraStateCache _cameraStateCache = new CameraStateCache();

	public CampaignMapProject Project
	{
		get
		{
			return _project;
		}
		private set
		{
			if (SetProperty(ref _project, value, "Project"))
			{
				OnPropertyChanged("WindowTitle");
			}
		}
	}

	public ProjectTreeViewModel ProjectTree
	{
		get
		{
			return _projectTree;
		}
		private set
		{
			if (_projectTree != null)
			{
				_projectTree.LayerVisibilityChanged -= OnLayerVisibilityChanged;
			}
			if (SetProperty(ref _projectTree, value, "ProjectTree"))
			{
				if (_projectTree != null)
				{
					_projectTree.LayerVisibilityChanged += OnLayerVisibilityChanged;
				}
			}
		}
	}

	public IReadOnlyList<BreadcrumbItemViewModel> Breadcrumbs
	{
		get
		{
			return _breadcrumbs;
		}
		private set
		{
			SetProperty(ref _breadcrumbs, value, "Breadcrumbs");
		}
	}

	public MapViewportViewModel MapViewport
	{
		get
		{
			return _mapViewport;
		}
		private set
		{
			SetProperty(ref _mapViewport, value, "MapViewport");
		}
	}

	public InspectorViewModel Inspector
	{
		get
		{
			return _inspector;
		}
		private set
		{
			SetProperty(ref _inspector, value, "Inspector");
		}
	}

	public StatusBarViewModel StatusBar { get; }

	public string? CurrentProjectPath
	{
		get
		{
			return _currentProjectPath;
		}
		private set
		{
			if (SetProperty(ref _currentProjectPath, value, "CurrentProjectPath"))
			{
				OnPropertyChanged("HasProjectPath");
			}
		}
	}

	public bool HasProjectPath => !string.IsNullOrWhiteSpace(CurrentProjectPath);

	public bool IsDirty
	{
		get
		{
			return _isDirty;
		}
		private set
		{
			if (SetProperty(ref _isDirty, value, "IsDirty"))
			{
				OnPropertyChanged("WindowTitle");
			}
		}
	}

	public string WindowTitle => "Living Atlas - " + Project.Name + (IsDirty ? "*" : string.Empty);

	public bool IsSnapToGridEnabled => MapViewport?.Map?.GridSettings.SnapToGrid ?? false;

	public bool CanUseSelectionChildMapAction => MapViewport.SelectedObject is DistrictShape;

	public bool CanCreateChildMapFromSelection => MapViewport.SelectedObject is DistrictShape { ChildMapId: var childMapId } && !childMapId.HasValue;

	public bool CanOpenSelectedChildMap => MapViewport.SelectedObject is DistrictShape { ChildMapId: var childMapId } && childMapId.HasValue;

	public bool CanOpenParentMap => MapViewport.Map.ParentMapId.HasValue;

	public bool CanOpenRootMap => MapViewport.Map.Id != Project.RootMapId;

	public string SelectionChildMapActionText => (MapViewport.SelectedObject is DistrictShape { ChildMapId: not null }) ? "Open Child Map" : "Create Child Map From Selection";

	public MainWindowViewModel()
		: this(DefaultProjectFactory.Create())
	{
	}

	public MainWindowViewModel(CampaignMapProject project)
	{
		ArgumentNullException.ThrowIfNull(project, "project");
		StatusBar = new StatusBarViewModel();
		ApplyProject(project, null);
	}

	public void NewProject()
	{
		ApplyProject(DefaultProjectFactory.Create(), null);
		StatusBar.SetMessage("New project created");
	}

	public async Task<bool> OpenProjectAsync(string path)
	{
		try
		{
			ApplyProject(await ProjectJsonSerializer.LoadAsync(path).ConfigureAwait(continueOnCapturedContext: true), Path.GetFullPath(path));
			StatusBar.SetMessage("Opened: " + Path.GetFileName(path));
			return true;
		}
		catch (Exception ex)
		{
			Exception exception = ex;
			StatusBar.SetMessage("Open failed: " + exception.Message);
			return false;
		}
	}

	public async Task<bool> SaveProjectAsync()
	{
		if (string.IsNullOrWhiteSpace(CurrentProjectPath))
		{
			StatusBar.SetMessage("Save failed: project path is not set");
			return false;
		}
		return await SaveProjectAsAsync(CurrentProjectPath).ConfigureAwait(continueOnCapturedContext: true);
	}

	public async Task<bool> SaveProjectAsAsync(string path)
	{
		try
		{
			string projectPath = EnsureJsonExtension(path);
			await ProjectJsonSerializer.SaveAsync(Project, projectPath).ConfigureAwait(continueOnCapturedContext: true);
			CurrentProjectPath = Path.GetFullPath(projectPath);
			IsDirty = false;
			StatusBar.SetMessage("Saved");
			return true;
		}
		catch (Exception ex)
		{
			Exception exception = ex;
			StatusBar.SetMessage("Save failed: " + exception.Message);
			return false;
		}
	}

	public void SetStatusMessage(string message)
	{
		StatusBar.SetMessage(message);
	}

	public bool CreateOrOpenChildMapFromSelection()
	{
		if (!(MapViewport.SelectedObject is DistrictShape { ChildMapId: var childMapId }))
		{
			StatusBar.SetMessage("Select a district to create a child map");
			return false;
		}
		if (childMapId.HasValue)
		{
			Guid valueOrDefault = childMapId.GetValueOrDefault();
			if (true)
			{
				return OpenMap(valueOrDefault);
			}
		}
		return CreateChildMapFromSelection();
	}

	public bool CreateChildMapFromSelection()
	{
		if (!(MapViewport.SelectedObject is DistrictShape { ChildMapId: var childMapId } districtShape))
		{
			StatusBar.SetMessage("Select a district to create a child map");
			return false;
		}
		if (childMapId.HasValue)
		{
			StatusBar.SetMessage("District '" + districtShape.Name + "' already has a child map");
			return false;
		}
		try
		{
			CreateChildMapCommand createChildMapCommand = new CreateChildMapCommand(Project, MapViewport.Map, districtShape);
			MapViewport.ExecuteCommand(createChildMapCommand, "Created child map: " + createChildMapCommand.ChildMap.Name);
			ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id);
			RefreshBreadcrumbs();
			NotifyChildMapNavigationStateChanged();
			return true;
		}
		catch (Exception ex)
		{
			StatusBar.SetMessage("Create child map failed: " + ex.Message);
			return false;
		}
	}

	public bool OpenSelectedChildMap()
	{
		if (!(MapViewport.SelectedObject is DistrictShape { ChildMapId: var childMapId } districtShape))
		{
			StatusBar.SetMessage("Select a district with a child map");
			return false;
		}
		if (childMapId.HasValue)
		{
			Guid valueOrDefault = childMapId.GetValueOrDefault();
			if (true)
			{
				return OpenMap(valueOrDefault);
			}
		}
		StatusBar.SetMessage("District '" + districtShape.Name + "' has no child map");
		return false;
	}

	public bool OpenParentMap()
	{
		Guid? parentMapId = MapViewport.Map.ParentMapId;
		if (parentMapId.HasValue)
		{
			Guid valueOrDefault = parentMapId.GetValueOrDefault();
			if (true)
			{
				return OpenMap(valueOrDefault);
			}
		}
		StatusBar.SetMessage("Active map has no parent");
		return false;
	}

	public bool OpenRootMap()
	{
		return OpenMap(Project.RootMapId);
	}

	public bool ApplyInspectorChanges()
	{
			MapObject? selectedObject = MapViewport.SelectedObject;
		if (selectedObject == null)
		{
			StatusBar.SetMessage("Inspector apply failed: no selection");
			return false;
		}
		try
		{
			string text = Inspector.EditableName.Trim();
			string? text2 = ((selectedObject is MapLabel) ? Inspector.EditableLabelText.Trim() : null);
			bool flag = !string.Equals(text, selectedObject.Name, StringComparison.Ordinal);
			bool flag2 = selectedObject is MapLabel mapLabel && !string.Equals(text2, mapLabel.Text, StringComparison.Ordinal);
			if (!flag && !flag2)
			{
				StatusBar.SetMessage("No inspector changes");
				return false;
			}
			UpdateMapObjectPropertiesCommand command = new UpdateMapObjectPropertiesCommand(MapViewport.Map, selectedObject.Id, text, flag2 ? text2 : null);
			MapViewport.ExecuteCommand(command, "Updated: " + text);
			Inspector.SetSelection(MapViewport.SelectedObject);
			return true;
		}
		catch (Exception ex)
		{
			StatusBar.SetMessage("Inspector apply failed: " + ex.Message);
			return false;
		}
	}

	public bool OpenMap(Guid mapId)
	{
		MapDocument? mapDocument = Project.FindMap(mapId);
		if (mapDocument == null)
		{
			StatusBar.SetMessage($"Open map failed: map '{mapId}' was not found");
			return false;
		}
		SetActiveMap(mapDocument, "Opened map: " + mapDocument.Name);
		return true;
	}

	private void OnMapViewportPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "StatusText")
		{
			StatusBar.SetMessage(MapViewport.StatusText);
		}
		if (e.PropertyName == "SelectedObject")
		{
			Inspector.SetSelection(MapViewport.SelectedObject);
			NotifyChildMapNavigationStateChanged();
		}
	}

	private void ApplyProject(CampaignMapProject project, string? currentProjectPath)
	{
		DetachMapViewportEvents();
		_mapViewportsByMapId.Clear();
		_cameraStateCache.Clear();
		Project = project;
		CurrentProjectPath = currentProjectPath;
		ProjectTree = new ProjectTreeViewModel(project, project.RootMapId);
		SetActiveMap(project.RootMap, null);
		IsDirty = false;
		OnPropertyChanged("WindowTitle");
		OnPropertyChanged(nameof(IsSnapToGridEnabled));
		StatusBar.SetMessage(MapViewport.StatusText);
	}

	private void OnMapViewportProjectMutated(object? sender, EventArgs e)
	{
		RemoveStaleMapViewports();
		ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id);
		RefreshBreadcrumbs();
		NotifyChildMapNavigationStateChanged();
		MarkDirty();
	}

	private void MarkDirty()
	{
		IsDirty = true;
	}

	private static string EnsureJsonExtension(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			throw new ArgumentException("Project path cannot be empty.", "path");
		}
		return string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase) ? path : (path + ".json");
	}

	private void SetActiveMap(MapDocument map, string? statusMessage)
	{
		SaveCameraStateForCurrentMap();
		DetachMapViewportEvents();
		MapViewport = GetOrCreateMapViewport(map);
		if (!_cameraStateCache.TryRestore(map.Id, MapViewport.Camera))
		{
			MapViewport.ResetCameraFit();
		}
		Inspector = new InspectorViewModel();
		Inspector.SetSelection(MapViewport.SelectedObject);
		MapViewport.PropertyChanged += OnMapViewportPropertyChanged;
		MapViewport.ProjectMutated += OnMapViewportProjectMutated;
		ProjectTree = new ProjectTreeViewModel(Project, map.Id);
		RefreshBreadcrumbs();
		StatusBar.SetMessage(statusMessage ?? MapViewport.StatusText);
		NotifyChildMapNavigationStateChanged();
		OnPropertyChanged(nameof(IsSnapToGridEnabled));
	}

	private void SaveCameraStateForCurrentMap()
	{
		if (_mapViewport != null)
		{
			_cameraStateCache.Save(_mapViewport.Map.Id, _mapViewport.Camera);
		}
	}

	public void ToggleSnapToGrid()
	{
		if (MapViewport == null) return;
		var map = MapViewport.Map;
		var current = map.GridSettings;
		map.SetGridSettings(new GridSettings(current.IsEnabled, current.CellSizeMeters, current.ShowGrid, !current.SnapToGrid));
		MarkDirty();
		OnPropertyChanged(nameof(IsSnapToGridEnabled));
		StatusBar.SetMessage($"Snap to Grid: {(map.GridSettings.SnapToGrid ? "On" : "Off")}");
	}

	private void OnLayerVisibilityChanged(object? sender, (Guid MapId, Guid LayerId, bool IsVisible) e)
	{
		var map = Project.FindMap(e.MapId);
		var layer = map?.Layers.FirstOrDefault(l => l.Id == e.LayerId);
		if (layer != null && layer.IsVisible != e.IsVisible)
		{
			layer.SetVisibility(e.IsVisible);
			MarkDirty();
			
			if (MapViewport.Map.Id == e.MapId)
			{
				MapViewport.RequestViewportRedraw();
			}

			if (MapViewport.Map.Id == e.MapId && !e.IsVisible && MapViewport.SelectedObject != null)
			{
				if (MapViewport.SelectedObject.LayerId == e.LayerId)
				{
					MapViewport.ClearSelection();
				}
			}
		}
	}

	private MapViewportViewModel GetOrCreateMapViewport(MapDocument map)
	{
		if (_mapViewportsByMapId.TryGetValue(map.Id, out MapViewportViewModel? value))
		{
			return value!;
		}
		value = new MapViewportViewModel(map, Project);
		_mapViewportsByMapId.Add(map.Id, value);
		return value;
	}

	private void DetachMapViewportEvents()
	{
		if (_mapViewport != null)
		{
			_mapViewport.PropertyChanged -= OnMapViewportPropertyChanged;
			_mapViewport.ProjectMutated -= OnMapViewportProjectMutated;
		}
	}

	private void RemoveStaleMapViewports()
	{
		List<Guid> list = _mapViewportsByMapId.Keys.Where((Guid mapId) => Project.FindMap(mapId) == null).ToList();
		foreach (Guid item in list)
		{
			_mapViewportsByMapId.Remove(item);
		}
	}

	private void NotifyChildMapNavigationStateChanged()
	{
		OnPropertyChanged("CanUseSelectionChildMapAction");
		OnPropertyChanged("CanCreateChildMapFromSelection");
		OnPropertyChanged("CanOpenSelectedChildMap");
		OnPropertyChanged("CanOpenParentMap");
		OnPropertyChanged("CanOpenRootMap");
		OnPropertyChanged("SelectionChildMapActionText");
	}

	private void RefreshBreadcrumbs()
	{
		IReadOnlyList<MapBreadcrumb> readOnlyList = MapBreadcrumbService.BuildBreadcrumbs(Project, MapViewport.Map.Id);
		List<BreadcrumbItemViewModel> list = new List<BreadcrumbItemViewModel>(readOnlyList.Count + 1)
		{
			new BreadcrumbItemViewModel(Project.Name, null, isActiveMap: false, 0)
		};
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			MapBreadcrumb mapBreadcrumb = readOnlyList[i];
			list.Add(new BreadcrumbItemViewModel(mapBreadcrumb.Name, mapBreadcrumb.MapId, mapBreadcrumb.MapId == MapViewport.Map.Id, i + 1));
		}
		Breadcrumbs = list;
	}
}
