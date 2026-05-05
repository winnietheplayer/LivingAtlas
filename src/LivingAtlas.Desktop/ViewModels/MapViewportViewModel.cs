using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Commands;
using LivingAtlas.Editor.Creation;
using LivingAtlas.Editor.Selection;
using LivingAtlas.Editor.Tools;
using LivingAtlas.Editor.Viewport;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class MapViewportViewModel : ViewModelBase
{
	private PointD? _lastScreenPoint;

	private MoveMapObjectDragSession? _activeMoveSession;
	private PointD? _dragStartAnchor;
	private PointD _dragAccumulatedRawDelta;

	private readonly DistrictDrawingSession _districtDrawingSession = new DistrictDrawingSession();

	private readonly RoadDrawingSession _roadDrawingSession = new RoadDrawingSession();

	private bool _hasInitialCameraFit;

	private bool _isMovingSelectedObject;

	private MapObject? _selectedObject;

	private string _statusText = FormatCoordinates(new PointD(0.0, 0.0), 1.0);

	public Camera2D Camera { get; } = new Camera2D();

	public HistoryService History { get; } = new HistoryService();

	public EditorToolService Tools { get; } = new EditorToolService();

	public MapDocument Map { get; }

	public CampaignMapProject? Project { get; }

	public Guid? ActiveTargetLayerId { get; set; }

	public double GridStepMeters => Map.GridSettings.CellSizeMeters;

	public EditorToolType ActiveTool => Tools.ActiveTool;

	public string ActiveToolText => FormatToolName(ActiveTool);

	public bool IsSelectMoveToolActive => ActiveTool == EditorToolType.SelectMove;

	public bool IsPanToolActive => ActiveTool == EditorToolType.Pan;

	public bool IsDrawingToolActive => Tools.IsDrawingTool;

	public IReadOnlyList<PointD> DistrictPreviewPoints => _districtDrawingSession.Points;

	public PointD? DistrictPreviewPoint => _districtDrawingSession.PreviewPoint;

	public bool IsDrawingDistrict => _districtDrawingSession.IsDrawing;

	public IReadOnlyList<PointD> RoadPreviewPoints => _roadDrawingSession.Points;

	public PointD? RoadPreviewPoint => _roadDrawingSession.PreviewPoint;

	public bool IsDrawingRoad => _roadDrawingSession.IsDrawing;

	public MapObject? SelectedObject
	{
		get
		{
			return _selectedObject;
		}
		private set
		{
			if (SetProperty(ref _selectedObject, value, "SelectedObject"))
			{
				OnPropertyChanged("SelectedObjectId");
			}
		}
	}

	public Guid? SelectedObjectId => SelectedObject?.Id;

	public string StatusText
	{
		get
		{
			return _statusText;
		}
		private set
		{
			SetProperty(ref _statusText, value, "StatusText");
		}
	}

	public event EventHandler? ProjectMutated;

	public event EventHandler? RedrawRequested;

	public MapViewportViewModel(MapDocument map, CampaignMapProject? project = null)
	{
		Map = map ?? throw new ArgumentNullException("map");
		Project = project;
		RefreshStatus();
	}

	public void RequestViewportRedraw()
	{
		RedrawRequested?.Invoke(this, EventArgs.Empty);
	}

	public void SetActiveTool(EditorToolType activeTool)
	{
		if (ActiveTool != activeTool)
		{
			if (ActiveTool == EditorToolType.Road && activeTool != EditorToolType.Road)
			{
				CancelRoadDrawingCore();
			}
			if (ActiveTool == EditorToolType.District && activeTool != EditorToolType.District)
			{
				CancelDistrictDrawingCore();
			}
			Tools.SetActiveTool(activeTool);
			OnPropertyChanged("ActiveTool");
			OnPropertyChanged("ActiveToolText");
			OnPropertyChanged("IsSelectMoveToolActive");
			OnPropertyChanged("IsPanToolActive");
			OnPropertyChanged("IsDrawingToolActive");
			RefreshStatus();
		}
	}

	public void EnsureInitialCameraFit(SizeD viewportSize)
	{
		if (!_hasInitialCameraFit)
		{
			ViewportCameraHelper.FitToView(Camera, Map.RealSizeMeters, viewportSize);
			_hasInitialCameraFit = true;
			OnPropertyChanged("Camera");
		}
	}

	public void ResetCameraFit()
	{
		_hasInitialCameraFit = false;
		OnPropertyChanged("Camera");
	}

	public MapObject? SelectAtScreenPoint(PointD screenPoint, double screenTolerancePixels)
	{
		if (screenTolerancePixels < 0.0)
		{
			throw new ArgumentOutOfRangeException("screenTolerancePixels", screenTolerancePixels, "Tolerance must not be negative.");
		}
		_lastScreenPoint = screenPoint;
		PointD worldPoint = Camera.ScreenToWorld(screenPoint);
		double worldTolerance = screenTolerancePixels / Camera.Zoom;
		SelectedObject = MapObjectHitTester.HitTest(Map, worldPoint, worldTolerance);
		RefreshStatus();
		return SelectedObject;
	}

	public PointOfInterest CreatePointOfInterestAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		PointD position = GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings);
		AddMapObjectCommand addMapObjectCommand = MapObjectCreationService.CreatePointOfInterestCommand(Map, position, ActiveTargetLayerId);
		History.Execute(addMapObjectCommand);
		PointOfInterest pointOfInterest = (PointOfInterest)(SelectedObject = (PointOfInterest)addMapObjectCommand.MapObject);
		StatusText = "Created: " + pointOfInterest.Name;
		NotifyProjectMutated();
		return pointOfInterest;
	}

	public MapLabel CreateLabelAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		PointD position = GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings);
		AddMapObjectCommand addMapObjectCommand = MapObjectCreationService.CreateLabelCommand(Map, position, ActiveTargetLayerId);
		History.Execute(addMapObjectCommand);
		MapLabel mapLabel = (MapLabel)(SelectedObject = (MapLabel)addMapObjectCommand.MapObject);
		StatusText = "Created: " + mapLabel.Name;
		NotifyProjectMutated();
		return mapLabel;
	}

	public void AddRoadPointAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		_roadDrawingSession.AddPoint(GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings));
		NotifyRoadPreviewChanged();
		RefreshStatus();
	}

	public bool TryFinishRoadDrawing()
	{
		if (ActiveTool != EditorToolType.Road || !_roadDrawingSession.CanFinish)
		{
			return false;
		}
		AddMapObjectCommand addMapObjectCommand = _roadDrawingSession.Finish(Map, ActiveTargetLayerId);
		History.Execute(addMapObjectCommand);
		RoadLine roadLine = (RoadLine)(SelectedObject = (RoadLine)addMapObjectCommand.MapObject);
		NotifyRoadPreviewChanged();
		StatusText = "Created: " + roadLine.Name;
		NotifyProjectMutated();
		return true;
	}

	public bool CancelRoadDrawing()
	{
		if (!CancelRoadDrawingCore())
		{
			return false;
		}
		RefreshStatus();
		return true;
	}

	public void AddDistrictPointAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		_districtDrawingSession.AddPoint(GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings));
		NotifyDistrictPreviewChanged();
		RefreshStatus();
	}

	public bool TryFinishDistrictDrawing()
	{
		if (ActiveTool != EditorToolType.District)
		{
			return false;
		}
		if (!_districtDrawingSession.CanFinish)
		{
			StatusText = "District needs at least 3 points";
			return false;
		}
		AddMapObjectCommand addMapObjectCommand = _districtDrawingSession.Finish(Map, ActiveTargetLayerId);
		History.Execute(addMapObjectCommand);
		DistrictShape districtShape = (DistrictShape)(SelectedObject = (DistrictShape)addMapObjectCommand.MapObject);
		NotifyDistrictPreviewChanged();
		StatusText = "Created: " + districtShape.Name;
		NotifyProjectMutated();
		return true;
	}

	public void ClearSelection()
	{
		SelectedObject = null;
	}

	public bool CancelDistrictDrawing()
	{
		if (!CancelDistrictDrawingCore())
		{
			return false;
		}
		RefreshStatus();
		return true;
	}

	public bool DeleteSelectedObject()
	{
		if (SelectedObject == null)
		{
			return false;
		}
		string name = SelectedObject.Name;
		DeleteMapObjectCommand command = new DeleteMapObjectCommand(Map, SelectedObject.Id);
		History.Execute(command);
		SelectedObject = null;
		StatusText = "Deleted: " + name;
		NotifyProjectMutated();
		return true;
	}

	public void ExecuteCommand(IEditorCommand command, string statusText)
	{
		ArgumentNullException.ThrowIfNull(command, "command");
		History.Execute(command);
		RefreshSelectedObjectReference();
		StatusText = statusText;
		OnPropertyChanged("SelectedObject");
		NotifyProjectMutated();
	}

	public void BeginMoveSelectedObject(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		_isMovingSelectedObject = SelectedObject != null;
		_activeMoveSession = ((SelectedObject == null) ? null : new MoveMapObjectDragSession(Map, SelectedObject.Id));
		_dragStartAnchor = SelectedObject != null ? GetAnchorPoint(SelectedObject) : null;
		_dragAccumulatedRawDelta = default;
		RefreshStatus();
	}

	private PointD GetAnchorPoint(MapObject mapObject)
	{
		return mapObject switch
		{
			PointOfInterest poi => poi.Position,
			MapLabel label => label.Position,
			RoadLine road => road.Points.FirstOrDefault(),
			DistrictShape district => district.PolygonPoints.FirstOrDefault(),
			_ => default
		};
	}

	public void MoveSelectedObjectByScreenDelta(PointD currentScreenPoint, double screenDeltaX, double screenDeltaY)
	{
		_lastScreenPoint = currentScreenPoint;
		if (SelectedObject == null || _activeMoveSession == null)
		{
			RefreshStatus();
			return;
		}
		PointD rawDelta = new PointD(screenDeltaX / Camera.Zoom, screenDeltaY / Camera.Zoom);
		_dragAccumulatedRawDelta = new PointD(_dragAccumulatedRawDelta.X + rawDelta.X, _dragAccumulatedRawDelta.Y + rawDelta.Y);

		if (Map.GridSettings.IsEnabled && Map.GridSettings.SnapToGrid && _dragStartAnchor.HasValue)
		{
			PointD rawTargetAnchor = new PointD(_dragStartAnchor.Value.X + _dragAccumulatedRawDelta.X, _dragStartAnchor.Value.Y + _dragAccumulatedRawDelta.Y);
			PointD snappedTargetAnchor = GridSnapper.Snap(rawTargetAnchor, Map.GridSettings);
			
			PointD targetTotalDelta = new PointD(snappedTargetAnchor.X - _dragStartAnchor.Value.X, snappedTargetAnchor.Y - _dragStartAnchor.Value.Y);
			PointD currentTotalDelta = _activeMoveSession.TotalDelta;
			PointD incrementalDelta = new PointD(targetTotalDelta.X - currentTotalDelta.X, targetTotalDelta.Y - currentTotalDelta.Y);
			
			_activeMoveSession.PreviewMoveBy(incrementalDelta);
		}
		else
		{
			_activeMoveSession.PreviewMoveBy(rawDelta);
		}
		
		RefreshStatus();
		OnPropertyChanged("SelectedObject");
	}

	public void EndMoveSelectedObject()
	{
		if (_isMovingSelectedObject)
		{
			_isMovingSelectedObject = false;
			MoveMapObjectCommand? moveMapObjectCommand = _activeMoveSession?.CreateCommandFromPreview();
			if (moveMapObjectCommand != null)
			{
				History.Execute(moveMapObjectCommand);
				NotifyProjectMutated();
			}
			_activeMoveSession = null;
			_dragStartAnchor = null;
			_dragAccumulatedRawDelta = default;
			RefreshStatus();
			OnPropertyChanged("SelectedObject");
		}
	}

	public bool Undo()
	{
		Guid? guid = SelectedObject?.Id;
		IEditorCommand? editorCommand = History.Undo();
		if (editorCommand == null)
		{
			return false;
		}
		if (editorCommand is AddMapObjectCommand addMapObjectCommand && guid == addMapObjectCommand.MapObject.Id)
		{
			SelectedObject = null;
		}
		else if (editorCommand is DeleteMapObjectCommand deleteMapObjectCommand)
		{
			SelectedObject = deleteMapObjectCommand.MapObject;
		}
		else
		{
			RefreshSelectedObjectReference();
		}
		StatusText = "Undo: " + editorCommand.Description;
		OnPropertyChanged("SelectedObject");
		NotifyProjectMutated();
		return true;
	}

	public bool Redo()
	{
		IEditorCommand? editorCommand = History.Redo();
		if (editorCommand == null)
		{
			return false;
		}
		if (editorCommand is AddMapObjectCommand addMapObjectCommand)
		{
			SelectedObject = addMapObjectCommand.MapObject;
		}
		else if (editorCommand is DeleteMapObjectCommand)
		{
			SelectedObject = null;
		}
		else
		{
			RefreshSelectedObjectReference();
		}
		StatusText = "Redo: " + editorCommand.Description;
		OnPropertyChanged("SelectedObject");
		NotifyProjectMutated();
		return true;
	}

	public void PanBy(double screenDeltaX, double screenDeltaY)
	{
		Camera.PanBy(screenDeltaX, screenDeltaY);
		RefreshStatus();
		OnPropertyChanged("Camera");
	}

	public void ZoomAt(PointD screenPoint, double zoomFactor)
	{
		_lastScreenPoint = screenPoint;
		Camera.ZoomAt(screenPoint, zoomFactor);
		RefreshStatus();
		OnPropertyChanged("Camera");
	}

	public void UpdatePointerPosition(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		if (ActiveTool == EditorToolType.Road && _roadDrawingSession.IsDrawing)
		{
			_roadDrawingSession.UpdatePreviewPoint(GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings));
			OnPropertyChanged("RoadPreviewPoint");
		}
		if (ActiveTool == EditorToolType.District && _districtDrawingSession.IsDrawing)
		{
			_districtDrawingSession.UpdatePreviewPoint(GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings));
			OnPropertyChanged("DistrictPreviewPoint");
		}
		RefreshStatus();
	}

	private void RefreshStatus()
	{
		PointD screenPoint = _lastScreenPoint ?? new PointD(0.0, 0.0);
		PointD worldPoint = Camera.ScreenToWorld(screenPoint);
		StatusText = FormatStatus(worldPoint, Camera.Zoom);
	}

	private string FormatStatus(PointD worldPoint, double zoom)
	{
		string coordinates = FormatCoordinates(worldPoint, zoom);
		string toolText = $"Tool: {ActiveToolText}";
		if (_isMovingSelectedObject && SelectedObject != null)
		{
			return $"{coordinates} | {toolText} | Moving: {SelectedObject.Name}";
		}
		if (ActiveTool == EditorToolType.PointOfInterest)
		{
			if (SelectedObject != null)
			{
				return $"{coordinates} | {toolText} | POI Tool: click to place point | Selected: {SelectedObject.Name}";
			}

			return $"{coordinates} | {toolText} | POI Tool: click to place point";
		}
		if (ActiveTool == EditorToolType.Label)
		{
			if (SelectedObject != null)
			{
				return $"{coordinates} | {toolText} | Label Tool: click to place text | Selected: {SelectedObject.Name}";
			}

			return $"{coordinates} | {toolText} | Label Tool: click to place text";
		}
		if (ActiveTool == EditorToolType.Road)
		{
			string roadText = _roadDrawingSession.IsDrawing ? "Road Tool: click to add point, Enter to finish, Esc to cancel" : "Road Tool: click to start road";
			if (SelectedObject != null)
			{
				return $"{coordinates} | {toolText} | {roadText} | Selected: {SelectedObject.Name}";
			}

			return $"{coordinates} | {toolText} | {roadText}";
		}
		if (ActiveTool == EditorToolType.District)
		{
			string districtText = _districtDrawingSession.IsDrawing ? "District Tool: click to add vertex, Enter to finish, Esc to cancel" : "District Tool: click to start polygon";
			if (SelectedObject != null)
			{
				return $"{coordinates} | {toolText} | {districtText} | Selected: {SelectedObject.Name}";
			}

			return $"{coordinates} | {toolText} | {districtText}";
		}
		if (SelectedObject != null)
		{
			return $"{coordinates} | {toolText} | Selected: {SelectedObject.Name}";
		}

		return $"{coordinates} | {toolText}";
	}

	private static string FormatCoordinates(PointD worldPoint, double zoom)
	{
		return string.Create(CultureInfo.InvariantCulture, $"X: {worldPoint.X:F2} m, Y: {worldPoint.Y:F2} m, Zoom: {zoom * 100.0:F0}%");
	}

	private static string FormatToolName(EditorToolType tool)
	{
		return tool switch
		{
			EditorToolType.SelectMove => "Select/Move", 
			EditorToolType.Pan => "Pan", 
			EditorToolType.District => "District", 
			EditorToolType.Road => "Road", 
			EditorToolType.PointOfInterest => "POI", 
			EditorToolType.Label => "Label", 
			_ => tool.ToString(), 
		};
	}

	private void RefreshSelectedObjectReference()
	{
		if (SelectedObject == null)
		{
			return;
		}
		foreach (MapLayer layer in Map.Layers)
		{
			MapObject? mapObject = layer.Objects.FirstOrDefault((MapObject candidate) => candidate.Id == SelectedObject.Id);
			if (mapObject != null)
			{
				SelectedObject = mapObject;
				return;
			}
		}
		SelectedObject = null;
	}

	private bool CancelRoadDrawingCore()
	{
		if (!_roadDrawingSession.IsDrawing && !_roadDrawingSession.PreviewPoint.HasValue)
		{
			return false;
		}
		_roadDrawingSession.Cancel();
		NotifyRoadPreviewChanged();
		return true;
	}

	private bool CancelDistrictDrawingCore()
	{
		if (!_districtDrawingSession.IsDrawing && !_districtDrawingSession.PreviewPoint.HasValue)
		{
			return false;
		}
		_districtDrawingSession.Cancel();
		NotifyDistrictPreviewChanged();
		return true;
	}

	private void NotifyRoadPreviewChanged()
	{
		OnPropertyChanged("RoadPreviewPoints");
		OnPropertyChanged("RoadPreviewPoint");
		OnPropertyChanged("IsDrawingRoad");
	}

	private void NotifyDistrictPreviewChanged()
	{
		OnPropertyChanged("DistrictPreviewPoints");
		OnPropertyChanged("DistrictPreviewPoint");
		OnPropertyChanged("IsDrawingDistrict");
	}

	private void NotifyProjectMutated()
	{
		this.ProjectMutated?.Invoke(this, EventArgs.Empty);
	}
}
