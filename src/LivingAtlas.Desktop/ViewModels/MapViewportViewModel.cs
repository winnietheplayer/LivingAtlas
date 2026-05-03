using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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

	public MapViewportViewModel(MapDocument map, CampaignMapProject? project = null)
	{
		Map = map ?? throw new ArgumentNullException("map");
		Project = project;
		RefreshStatus();
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
		PointD position = Camera.ScreenToWorld(screenPoint);
		AddMapObjectCommand addMapObjectCommand = MapObjectCreationService.CreatePointOfInterestCommand(Map, position);
		History.Execute(addMapObjectCommand);
		PointOfInterest pointOfInterest = (PointOfInterest)(SelectedObject = (PointOfInterest)addMapObjectCommand.MapObject);
		StatusText = "Created: " + pointOfInterest.Name;
		NotifyProjectMutated();
		return pointOfInterest;
	}

	public MapLabel CreateLabelAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		PointD position = Camera.ScreenToWorld(screenPoint);
		AddMapObjectCommand addMapObjectCommand = MapObjectCreationService.CreateLabelCommand(Map, position);
		History.Execute(addMapObjectCommand);
		MapLabel mapLabel = (MapLabel)(SelectedObject = (MapLabel)addMapObjectCommand.MapObject);
		StatusText = "Created: " + mapLabel.Name;
		NotifyProjectMutated();
		return mapLabel;
	}

	public void AddRoadPointAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		_roadDrawingSession.AddPoint(Camera.ScreenToWorld(screenPoint));
		NotifyRoadPreviewChanged();
		RefreshStatus();
	}

	public bool TryFinishRoadDrawing()
	{
		if (ActiveTool != EditorToolType.Road || !_roadDrawingSession.CanFinish)
		{
			return false;
		}
		AddMapObjectCommand addMapObjectCommand = _roadDrawingSession.Finish(Map);
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
		_districtDrawingSession.AddPoint(Camera.ScreenToWorld(screenPoint));
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
		AddMapObjectCommand addMapObjectCommand = _districtDrawingSession.Finish(Map);
		History.Execute(addMapObjectCommand);
		DistrictShape districtShape = (DistrictShape)(SelectedObject = (DistrictShape)addMapObjectCommand.MapObject);
		NotifyDistrictPreviewChanged();
		StatusText = "Created: " + districtShape.Name;
		NotifyProjectMutated();
		return true;
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
		RefreshStatus();
	}

	public void MoveSelectedObjectByScreenDelta(PointD currentScreenPoint, double screenDeltaX, double screenDeltaY)
	{
		_lastScreenPoint = currentScreenPoint;
		if (SelectedObject == null)
		{
			RefreshStatus();
			return;
		}
		PointD delta = new PointD(screenDeltaX / Camera.Zoom, screenDeltaY / Camera.Zoom);
		_activeMoveSession?.PreviewMoveBy(delta);
		RefreshStatus();
		OnPropertyChanged("SelectedObject");
	}

	public void EndMoveSelectedObject()
	{
		if (_isMovingSelectedObject)
		{
			_isMovingSelectedObject = false;
			MoveMapObjectCommand moveMapObjectCommand = _activeMoveSession?.CreateCommandFromPreview();
			if (moveMapObjectCommand != null)
			{
				History.Execute(moveMapObjectCommand);
				NotifyProjectMutated();
			}
			_activeMoveSession = null;
			RefreshStatus();
			OnPropertyChanged("SelectedObject");
		}
	}

	public bool Undo()
	{
		Guid? guid = SelectedObject?.Id;
		IEditorCommand editorCommand = History.Undo();
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
		IEditorCommand editorCommand = History.Redo();
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
			_roadDrawingSession.UpdatePreviewPoint(Camera.ScreenToWorld(screenPoint));
			OnPropertyChanged("RoadPreviewPoint");
		}
		if (ActiveTool == EditorToolType.District && _districtDrawingSession.IsDrawing)
		{
			_districtDrawingSession.UpdatePreviewPoint(Camera.ScreenToWorld(screenPoint));
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
		string value = FormatCoordinates(worldPoint, zoom);
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(6, 1, invariantCulture);
		handler.AppendLiteral("Tool: ");
		handler.AppendFormatted(ActiveToolText);
		string value2 = string.Create(provider, ref handler);
		if (_isMovingSelectedObject && SelectedObject != null)
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(14, 3, invariantCulture);
			handler2.AppendFormatted(value);
			handler2.AppendLiteral(" | ");
			handler2.AppendFormatted(value2);
			handler2.AppendLiteral(" | Moving: ");
			handler2.AppendFormatted(SelectedObject.Name);
			return string.Create(provider2, ref handler2);
		}
		if (ActiveTool == EditorToolType.PointOfInterest)
		{
			string result;
			if (SelectedObject != null)
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				DefaultInterpolatedStringHandler handler3 = new DefaultInterpolatedStringHandler(49, 3, invariantCulture);
				handler3.AppendFormatted(value);
				handler3.AppendLiteral(" | ");
				handler3.AppendFormatted(value2);
				handler3.AppendLiteral(" | POI Tool: click to place point | Selected: ");
				handler3.AppendFormatted(SelectedObject.Name);
				result = string.Create(provider3, ref handler3);
			}
			else
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider4 = invariantCulture;
				DefaultInterpolatedStringHandler handler4 = new DefaultInterpolatedStringHandler(36, 2, invariantCulture);
				handler4.AppendFormatted(value);
				handler4.AppendLiteral(" | ");
				handler4.AppendFormatted(value2);
				handler4.AppendLiteral(" | POI Tool: click to place point");
				result = string.Create(provider4, ref handler4);
			}
			return result;
		}
		if (ActiveTool == EditorToolType.Label)
		{
			string result2;
			if (SelectedObject != null)
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider5 = invariantCulture;
				DefaultInterpolatedStringHandler handler5 = new DefaultInterpolatedStringHandler(50, 3, invariantCulture);
				handler5.AppendFormatted(value);
				handler5.AppendLiteral(" | ");
				handler5.AppendFormatted(value2);
				handler5.AppendLiteral(" | Label Tool: click to place text | Selected: ");
				handler5.AppendFormatted(SelectedObject.Name);
				result2 = string.Create(provider5, ref handler5);
			}
			else
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider6 = invariantCulture;
				DefaultInterpolatedStringHandler handler6 = new DefaultInterpolatedStringHandler(37, 2, invariantCulture);
				handler6.AppendFormatted(value);
				handler6.AppendLiteral(" | ");
				handler6.AppendFormatted(value2);
				handler6.AppendLiteral(" | Label Tool: click to place text");
				result2 = string.Create(provider6, ref handler6);
			}
			return result2;
		}
		if (ActiveTool == EditorToolType.Road)
		{
			string value3 = (_roadDrawingSession.IsDrawing ? "Road Tool: click to add point, Enter to finish, Esc to cancel" : "Road Tool: click to start road");
			string result3;
			if (SelectedObject != null)
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider7 = invariantCulture;
				DefaultInterpolatedStringHandler handler7 = new DefaultInterpolatedStringHandler(19, 4, invariantCulture);
				handler7.AppendFormatted(value);
				handler7.AppendLiteral(" | ");
				handler7.AppendFormatted(value2);
				handler7.AppendLiteral(" | ");
				handler7.AppendFormatted(value3);
				handler7.AppendLiteral(" | Selected: ");
				handler7.AppendFormatted(SelectedObject.Name);
				result3 = string.Create(provider7, ref handler7);
			}
			else
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider8 = invariantCulture;
				DefaultInterpolatedStringHandler handler8 = new DefaultInterpolatedStringHandler(6, 3, invariantCulture);
				handler8.AppendFormatted(value);
				handler8.AppendLiteral(" | ");
				handler8.AppendFormatted(value2);
				handler8.AppendLiteral(" | ");
				handler8.AppendFormatted(value3);
				result3 = string.Create(provider8, ref handler8);
			}
			return result3;
		}
		if (ActiveTool == EditorToolType.District)
		{
			string value4 = (_districtDrawingSession.IsDrawing ? "District Tool: click to add vertex, Enter to finish, Esc to cancel" : "District Tool: click to start polygon");
			string result4;
			if (SelectedObject != null)
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider9 = invariantCulture;
				DefaultInterpolatedStringHandler handler9 = new DefaultInterpolatedStringHandler(19, 4, invariantCulture);
				handler9.AppendFormatted(value);
				handler9.AppendLiteral(" | ");
				handler9.AppendFormatted(value2);
				handler9.AppendLiteral(" | ");
				handler9.AppendFormatted(value4);
				handler9.AppendLiteral(" | Selected: ");
				handler9.AppendFormatted(SelectedObject.Name);
				result4 = string.Create(provider9, ref handler9);
			}
			else
			{
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider10 = invariantCulture;
				DefaultInterpolatedStringHandler handler10 = new DefaultInterpolatedStringHandler(6, 3, invariantCulture);
				handler10.AppendFormatted(value);
				handler10.AppendLiteral(" | ");
				handler10.AppendFormatted(value2);
				handler10.AppendLiteral(" | ");
				handler10.AppendFormatted(value4);
				result4 = string.Create(provider10, ref handler10);
			}
			return result4;
		}
		string result5;
		if (SelectedObject != null)
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider11 = invariantCulture;
			DefaultInterpolatedStringHandler handler11 = new DefaultInterpolatedStringHandler(16, 3, invariantCulture);
			handler11.AppendFormatted(value);
			handler11.AppendLiteral(" | ");
			handler11.AppendFormatted(value2);
			handler11.AppendLiteral(" | Selected: ");
			handler11.AppendFormatted(SelectedObject.Name);
			result5 = string.Create(provider11, ref handler11);
		}
		else
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider12 = invariantCulture;
			DefaultInterpolatedStringHandler handler12 = new DefaultInterpolatedStringHandler(3, 2, invariantCulture);
			handler12.AppendFormatted(value);
			handler12.AppendLiteral(" | ");
			handler12.AppendFormatted(value2);
			result5 = string.Create(provider12, ref handler12);
		}
		return result5;
	}

	private static string FormatCoordinates(PointD worldPoint, double zoom)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(21, 3, invariantCulture);
		handler.AppendLiteral("X: ");
		handler.AppendFormatted(worldPoint.X, "F2");
		handler.AppendLiteral(" m, Y: ");
		handler.AppendFormatted(worldPoint.Y, "F2");
		handler.AppendLiteral(" m, Zoom: ");
		handler.AppendFormatted(zoom * 100.0, "F0");
		handler.AppendLiteral("%");
		return string.Create(invariantCulture, ref handler);
	}

	private static string FormatToolName(EditorToolType tool)
	{
		if (1 == 0)
		{
		}
		string result = tool switch
		{
			EditorToolType.SelectMove => "Select/Move", 
			EditorToolType.Pan => "Pan", 
			EditorToolType.District => "District", 
			EditorToolType.Road => "Road", 
			EditorToolType.PointOfInterest => "POI", 
			EditorToolType.Label => "Label", 
			_ => tool.ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private void RefreshSelectedObjectReference()
	{
		if (SelectedObject == null)
		{
			return;
		}
		foreach (MapLayer layer in Map.Layers)
		{
			MapObject mapObject = layer.Objects.FirstOrDefault((MapObject candidate) => candidate.Id == SelectedObject.Id);
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
