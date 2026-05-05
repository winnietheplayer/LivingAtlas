using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LivingAtlas.Desktop.Services;
using LivingAtlas.Domain.Geometry;
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

	private readonly Dictionary<Guid, Guid> _activeTargetLayers = new Dictionary<Guid, Guid>();

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
				_projectTree.LayerLockChanged -= OnLayerLockChanged;
			}
			if (SetProperty(ref _projectTree, value, "ProjectTree"))
			{
				if (_projectTree != null)
				{
					_projectTree.LayerVisibilityChanged += OnLayerVisibilityChanged;
					_projectTree.LayerLockChanged += OnLayerLockChanged;
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

	public bool CreateChildMapFromSelection(CreateChildMapViewModel? settings = null)
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
			SizeD? customSize = (settings != null && settings.UseCustomSize) ? new SizeD(settings.Width, settings.Height) : null;
			string? name = settings?.Name;
			MapScaleType? scaleType = settings?.ScaleType;

			CreateChildMapCommand command = new CreateChildMapCommand(Project, MapViewport.Map, districtShape, name, customSize, scaleType);
			MapViewport.ExecuteCommand(command, "Created child map: " + command.ChildMap.Name);
			
			MarkDirty();
			ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
			RefreshBreadcrumbs();
			NotifyChildMapNavigationStateChanged();

			if (settings?.OpenAfterCreation == true)
			{
				OpenMap(command.ChildMap.Id);
			}

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
			string styleKey = Inspector.EditableStyleKey.Trim();
			string description = Inspector.EditableDescription;

			bool flag = !string.Equals(text, selectedObject.Name, StringComparison.Ordinal);
			bool flag2 = selectedObject is MapLabel mapLabel && !string.Equals(text2, mapLabel.Text, StringComparison.Ordinal);
			bool flag3 = !string.Equals(styleKey, selectedObject.StyleKey, StringComparison.Ordinal);
			bool flag4 = !string.Equals(description, selectedObject.Description, StringComparison.Ordinal);
			string? category = null;
			string? roadKind = null;
			string? districtKind = null;
			string? labelKind = null;
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = false;
			bool flag8 = false;

			if (selectedObject is PointOfInterest pointOfInterest)
			{
				category = Inspector.EditableCategory;
				flag5 = !string.Equals(category, pointOfInterest.Category, StringComparison.Ordinal);
			}
			else if (selectedObject is RoadLine roadLine)
			{
				roadKind = NormalizeRoadKind(Inspector.EditableRoadKind);
				flag6 = !string.Equals(roadKind, roadLine.RoadKind, StringComparison.Ordinal);
			}
			else if (selectedObject is DistrictShape districtShape)
			{
				districtKind = NormalizeDistrictKind(Inspector.EditableDistrictKind);
				flag7 = !string.Equals(districtKind, districtShape.DistrictKind, StringComparison.Ordinal);
			}
			else if (selectedObject is MapLabel label)
			{
				labelKind = NormalizeLabelKind(Inspector.EditableLabelKind);
				flag8 = !string.Equals(labelKind, label.LabelKind, StringComparison.Ordinal);
			}

			if (!flag && !flag2 && !flag3 && !flag4 && !flag5 && !flag6 && !flag7 && !flag8)
			{
				StatusBar.SetMessage("No inspector changes");
				return false;
			}
			UpdateMapObjectPropertiesCommand command = new UpdateMapObjectPropertiesCommand(
				MapViewport.Map,
				selectedObject.Id,
				text,
				flag2 ? text2 : null,
				flag3 ? styleKey : null,
				flag4 ? description : null,
				newCategory: flag5 ? category : null,
				newRoadKind: flag6 ? roadKind : null,
				newDistrictKind: flag7 ? districtKind : null,
				newLabelKind: flag8 ? labelKind : null);
			MapViewport.ExecuteCommand(command, "Object updated: " + text);
			MapViewport.RequestViewportRedraw();
			Inspector.SetSelection(MapViewport.SelectedObject);
			return true;
		}
		catch (Exception ex)
		{
			StatusBar.SetMessage("Inspector apply failed: " + ex.Message);
			return false;
		}
	}

	private static string NormalizeRoadKind(string? roadKind)
	{
		return string.IsNullOrWhiteSpace(roadKind) ? RoadLine.DefaultRoadKind : roadKind.Trim();
	}

	private static string NormalizeDistrictKind(string? districtKind)
	{
		return string.IsNullOrWhiteSpace(districtKind) ? DistrictShape.DefaultDistrictKind : districtKind.Trim();
	}

	private static string NormalizeLabelKind(string? labelKind)
	{
		return string.IsNullOrWhiteSpace(labelKind) ? MapLabel.DefaultLabelKind : labelKind.Trim();
	}

	public bool DuplicateSelectedObject()
	{
		if (MapViewport == null) return false;
		return MapViewport.DuplicateSelectedObject();
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
		_activeTargetLayers.Clear();
		_cameraStateCache.Clear();
		Project = project;
		CurrentProjectPath = currentProjectPath;
		ProjectTree = new ProjectTreeViewModel(project, project.RootMapId, GetActiveTargetLayer(project.RootMapId));
		SetActiveMap(project.RootMap, null);
		IsDirty = false;
		OnPropertyChanged("WindowTitle");
		OnPropertyChanged(nameof(IsSnapToGridEnabled));
		StatusBar.SetMessage(MapViewport.StatusText);
	}

	private void OnMapViewportProjectMutated(object? sender, EventArgs e)
	{
		RemoveStaleMapViewports();
		ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
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
		MapViewport.ActiveTargetLayerId = GetActiveTargetLayer(map.Id);
		MapViewport.PropertyChanged += OnMapViewportPropertyChanged;
		MapViewport.ProjectMutated += OnMapViewportProjectMutated;
		ProjectTree = new ProjectTreeViewModel(Project, map.Id, GetActiveTargetLayer(map.Id));
		RefreshBreadcrumbs();
		StatusBar.SetMessage(statusMessage ?? MapViewport.StatusText);
		NotifyChildMapNavigationStateChanged();
		UpdateStatusBarMapInfo();
		OnPropertyChanged(nameof(IsSnapToGridEnabled));
	}

	private void UpdateStatusBarMapInfo()
	{
		StatusBar.SetMessage($"Map: {MapViewport.Map.Name} [{MapViewport.Map.ScaleType}]");
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

	private void OnLayerLockChanged(object? sender, (Guid MapId, Guid LayerId, bool IsLocked) e)
	{
		var map = Project.FindMap(e.MapId);
		var layer = map?.Layers.FirstOrDefault(l => l.Id == e.LayerId);
		if (layer != null && layer.IsLocked != e.IsLocked)
		{
			layer.SetLocked(e.IsLocked);
			MarkDirty();

			if (MapViewport.Map.Id == e.MapId && e.IsLocked && MapViewport.SelectedObject != null)
			{
				if (MapViewport.SelectedObject.LayerId == e.LayerId)
				{
					MapViewport.ClearSelection();
				}
			}
		}
	}

	public bool RenameLayer(Guid mapId, Guid layerId, string newName)
	{
		var map = Project.FindMap(mapId);
		var layer = map?.Layers.FirstOrDefault(l => l.Id == layerId);
		if (layer == null)
		{
			return false;
		}

		string oldName = layer.Name;
		string trimmedName = newName.Trim();
		if (string.Equals(oldName, trimmedName, StringComparison.Ordinal))
		{
			return false; // No change
		}

		layer.Rename(trimmedName);
		MarkDirty();
		ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
		StatusBar.SetMessage($"Layer renamed: {trimmedName}");
		return true;
	}

	public void UpdateMapSettings(Guid mapId, EditMapSettingsViewModel settings)
	{
		var map = Project.FindMap(mapId);
		if (map == null) return;

		map.Rename(settings.Name);
		map.SetScaleType(settings.ScaleType);
		map.SetRealSize(new LivingAtlas.Domain.Geometry.SizeD(settings.Width, settings.Height));
		map.SetGridSettings(new GridSettings(settings.GridEnabled, settings.GridCellSize, true, settings.SnapToGrid));

		MarkDirty();
		ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
		RefreshBreadcrumbs();
		
		if (map.Id == MapViewport.Map.Id)
		{
			MapViewport.NotifyMapPropertiesChanged();
			OnPropertyChanged(nameof(WindowTitle));
			UpdateStatusBarMapInfo();
		}
		else
		{
			StatusBar.SetMessage($"Map settings updated: {map.Name}");
		}
	}

	public bool MoveLayerUp(Guid mapId, Guid layerId)
	{
		var map = Project.FindMap(mapId);
		if (map == null || !map.MoveLayerUp(layerId))
		{
			return false;
		}

		MarkDirty();
		ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
		MapViewport.RequestViewportRedraw();
		StatusBar.SetMessage("Layer moved up");
		return true;
	}

	public bool MoveLayerDown(Guid mapId, Guid layerId)
	{
		var map = Project.FindMap(mapId);
		if (map == null || !map.MoveLayerDown(layerId))
		{
			return false;
		}

		MarkDirty();
		ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
		MapViewport.RequestViewportRedraw();
		StatusBar.SetMessage("Layer moved down");
		return true;
	}

	public void SetActiveTargetLayer(Guid mapId, Guid layerId)
	{
		_activeTargetLayers[mapId] = layerId;
		if (MapViewport != null)
		{
			if (MapViewport.Map.Id == mapId)
			{
				MapViewport.ActiveTargetLayerId = layerId;
			}
			ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
		}
		
		var map = Project.FindMap(mapId);
		var layer = map?.Layers.FirstOrDefault(l => l.Id == layerId);
		if (layer != null)
		{
			StatusBar.SetMessage($"Active layer: {layer.Name}");
		}
	}

	public Guid? GetActiveTargetLayer(Guid mapId)
	{
		if (_activeTargetLayers.TryGetValue(mapId, out var layerId))
		{
			return layerId;
		}
		return null;
	}

	public void AddLayer(Guid mapId, string name, MapLayerType type)
	{
		var map = Project.FindMap(mapId);
		if (map == null) return;

		var layer = new MapLayer(Guid.NewGuid(), name, type);
		map.AddLayer(layer);
		
		MarkDirty();
		ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
		MapViewport.RequestViewportRedraw();
		StatusBar.SetMessage($"Layer added: {name}");
	}

	public bool DeleteLayer(Guid mapId, Guid layerId)
	{
		var map = Project.FindMap(mapId);
		if (map == null) return false;

		var layer = map.Layers.FirstOrDefault(l => l.Id == layerId);
		if (layer == null) return false;

		if (map.Layers.Count <= 1)
		{
			StatusBar.SetMessage("Cannot delete the last layer.");
			return false;
		}

		if (layer.IsLocked)
		{
			StatusBar.SetMessage("Unlock layer before deleting it.");
			return false;
		}

		map.RemoveLayer(layerId);
		
		if (GetActiveTargetLayer(mapId) == layerId)
		{
			_activeTargetLayers.Remove(mapId);
			if (MapViewport != null && MapViewport.Map.Id == mapId)
			{
				MapViewport.ActiveTargetLayerId = null;
			}
		}

		if (MapViewport != null)
		{
			if (MapViewport.Map.Id == mapId && MapViewport.SelectedObject?.LayerId == layerId)
			{
				MapViewport.ClearSelection();
			}
			ProjectTree = new ProjectTreeViewModel(Project, MapViewport.Map.Id, GetActiveTargetLayer(MapViewport.Map.Id));
			MapViewport.RequestViewportRedraw();
		}

		MarkDirty();
		
		StatusBar.SetMessage(layer.Objects.Count > 0 
			? $"Deleted layer: {layer.Name} with {layer.Objects.Count} objects."
			: $"Deleted layer: {layer.Name}");
			
		return true;
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
