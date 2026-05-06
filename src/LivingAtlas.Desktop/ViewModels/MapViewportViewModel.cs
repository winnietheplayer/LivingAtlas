using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LivingAtlas.Assets;
using LivingAtlas.Desktop.Services;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Commands;
using LivingAtlas.Editor.Creation;
using LivingAtlas.Editor.Hierarchy;
using LivingAtlas.Editor.Selection;
using LivingAtlas.Editor.Tools;
using LivingAtlas.Editor.Viewport;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class MapViewportViewModel : ViewModelBase
{
	private const double ParentRoadOverlaySnapTolerancePixels = 12.0;

	private PointD? _lastScreenPoint;

	private MoveMapObjectDragSession? _activeMoveSession;
	private PointD? _dragStartAnchor;
	private PointD _dragAccumulatedRawDelta;

	private bool _isMovingSelectedVertex;

	private Guid? _movingVertexObjectId;

	private int? _movingVertexIndex;

	private PointD _movingVertexOldPoint;

	private PointD _movingVertexPreviewPoint;

	private int? _selectedVertexIndex;

	private int? _hoveredVertexIndex;

	private readonly DistrictDrawingSession _districtDrawingSession = new DistrictDrawingSession();

	private readonly RoadDrawingSession _roadDrawingSession = new RoadDrawingSession();

	private readonly RoadAreaDrawingSession _roadAreaDrawingSession = new RoadAreaDrawingSession();

	private bool _hasInitialCameraFit;

	private bool _isMovingSelectedObject;

	private MapObject? _selectedObject;

	private string _statusText = FormatCoordinates(new PointD(0.0, 0.0), 1.0);

	public Camera2D Camera { get; } = new Camera2D();

	public HistoryService History { get; } = new HistoryService();

	public EditorToolService Tools { get; } = new EditorToolService();

	public MapDocument Map { get; }

	public CampaignMapProject? Project { get; }

	public TextureAssetCatalog TextureAssetCatalog { get; }

	public AvaloniaTextureImageCache TextureImageCache { get; } = new AvaloniaTextureImageCache();

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

	public IReadOnlyList<PointD> RoadAreaPreviewPoints => _roadAreaDrawingSession.Points;

	public PointD? RoadAreaPreviewPoint => _roadAreaDrawingSession.PreviewPoint;

	public bool IsDrawingRoadArea => _roadAreaDrawingSession.IsDrawing;

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

	public int? SelectedVertexIndex
	{
		get
		{
			return _selectedVertexIndex;
		}
		private set
		{
			SetProperty(ref _selectedVertexIndex, value, "SelectedVertexIndex");
		}
	}

	public int? HoveredVertexIndex
	{
		get
		{
			return _hoveredVertexIndex;
		}
		private set
		{
			SetProperty(ref _hoveredVertexIndex, value, "HoveredVertexIndex");
		}
	}

	public bool IsMovingSelectedVertex => _isMovingSelectedVertex;

	public bool IsSelectedGeometryEditable => ActiveTool == EditorToolType.SelectMove && SelectedObject is RoadLine or RoadArea or DistrictShape && IsLayerEditable(SelectedObject.LayerId);

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

	public MapViewportViewModel(MapDocument map, CampaignMapProject? project = null, TextureAssetCatalog? textureAssetCatalog = null)
	{
		Map = map ?? throw new ArgumentNullException("map");
		Project = project;
		TextureAssetCatalog = textureAssetCatalog ?? TextureAssetCatalog.Empty;
		RefreshStatus();
	}

	public void RequestViewportRedraw()
	{
		RedrawRequested?.Invoke(this, EventArgs.Empty);
	}

	public IReadOnlyList<ParentRoadOverlay> GetParentRoadOverlays()
	{
		return Project == null
			? Array.Empty<ParentRoadOverlay>()
			: ParentRoadProjectionService.GetProjectedRoadAreas(Project, Map.Id);
	}

	public void SetActiveTool(EditorToolType activeTool)
	{
		if (ActiveTool != activeTool)
		{
			if (ActiveTool == EditorToolType.Road && activeTool != EditorToolType.Road)
			{
				CancelRoadDrawingCore();
			}
			if (ActiveTool == EditorToolType.RoadArea && activeTool != EditorToolType.RoadArea)
			{
				CancelRoadAreaDrawingCore();
			}
			if (ActiveTool == EditorToolType.District && activeTool != EditorToolType.District)
			{
				CancelDistrictDrawingCore();
			}
			Tools.SetActiveTool(activeTool);
			if (activeTool != EditorToolType.SelectMove)
			{
				ClearSelectedVertex();
				HoveredVertexIndex = null;
			}
			OnPropertyChanged("ActiveTool");
			OnPropertyChanged("ActiveToolText");
			OnPropertyChanged("IsSelectMoveToolActive");
			OnPropertyChanged("IsPanToolActive");
			OnPropertyChanged("IsDrawingToolActive");
			OnPropertyChanged("IsSelectedGeometryEditable");
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
		ClearSelectedVertex();
		HoveredVertexIndex = null;
		OnPropertyChanged("IsSelectedGeometryEditable");
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
		ClearSelectedVertex();
		HoveredVertexIndex = null;
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
		ClearSelectedVertex();
		HoveredVertexIndex = null;
		StatusText = "Created: " + mapLabel.Name;
		NotifyProjectMutated();
		return mapLabel;
	}

	public void AddRoadPointAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		_roadDrawingSession.AddPoint(SnapCreationPoint(screenPoint, includeParentRoadOverlaySnap: true));
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
		ClearSelectedVertex();
		HoveredVertexIndex = null;
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

	public void AddRoadAreaPointAtScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		_roadAreaDrawingSession.AddPoint(SnapCreationPoint(screenPoint, includeParentRoadOverlaySnap: true));
		NotifyRoadAreaPreviewChanged();
		RefreshStatus();
	}

	public bool TryFinishRoadAreaDrawing()
	{
		if (ActiveTool != EditorToolType.RoadArea)
		{
			return false;
		}
		if (!_roadAreaDrawingSession.CanFinish)
		{
			StatusText = "Road Area needs at least 3 points";
			return false;
		}
		AddMapObjectCommand addMapObjectCommand = _roadAreaDrawingSession.Finish(Map, ActiveTargetLayerId);
		History.Execute(addMapObjectCommand);
		RoadArea roadArea = (RoadArea)(SelectedObject = (RoadArea)addMapObjectCommand.MapObject);
		ClearSelectedVertex();
		HoveredVertexIndex = null;
		NotifyRoadAreaPreviewChanged();
		StatusText = "Created: " + roadArea.Name;
		NotifyProjectMutated();
		return true;
	}

	public bool CancelRoadAreaDrawing()
	{
		if (!CancelRoadAreaDrawingCore())
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
		ClearSelectedVertex();
		HoveredVertexIndex = null;
		NotifyDistrictPreviewChanged();
		StatusText = "Created: " + districtShape.Name;
		NotifyProjectMutated();
		return true;
	}

	public void ClearSelection()
	{
		SelectedObject = null;
		ClearSelectedVertex();
		HoveredVertexIndex = null;
		OnPropertyChanged("IsSelectedGeometryEditable");
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
		ClearSelectedVertex();
		HoveredVertexIndex = null;
		StatusText = "Deleted: " + name;
		NotifyProjectMutated();
		return true;
	}

	public bool DuplicateSelectedObject()
	{
		if (SelectedObject == null)
		{
			StatusText = "No object selected";
			return false;
		}

		try
		{
			DuplicateMapObjectCommand command = new DuplicateMapObjectCommand(Map, SelectedObject);
			History.Execute(command);
			SelectedObject = command.Duplicate;
			ClearSelectedVertex();
			HoveredVertexIndex = null;
			StatusText = "Duplicated: " + command.Duplicate.Name;
			NotifyProjectMutated();
			return true;
		}
		catch (Exception ex)
		{
			StatusText = ex.Message;
			return false;
		}
	}

	public void ExecuteCommand(IEditorCommand command, string statusText)
	{
		ArgumentNullException.ThrowIfNull(command, "command");
		History.Execute(command);
		RefreshSelectedObjectReference();
		ValidateSelectedVertexIndex();
		StatusText = statusText;
		OnPropertyChanged("SelectedObject");
		OnPropertyChanged("IsSelectedGeometryEditable");
		NotifyProjectMutated();
	}

	public bool BeginMoveSelectedVertex(int vertexIndex, PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		if (!IsSelectedGeometryEditable || SelectedObject == null || !TryGetVertexPoint(SelectedObject, vertexIndex, out PointD point))
		{
			RefreshStatus();
			return false;
		}
		_isMovingSelectedVertex = true;
		_movingVertexObjectId = SelectedObject.Id;
		_movingVertexIndex = vertexIndex;
		_movingVertexOldPoint = point;
		_movingVertexPreviewPoint = point;
		SelectedVertexIndex = vertexIndex;
		OnPropertyChanged("IsMovingSelectedVertex");
		StatusText = "Moving vertex " + (vertexIndex + 1);
		return true;
	}

	public void MoveSelectedVertexToScreenPoint(PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		if (!_isMovingSelectedVertex || SelectedObject == null || !_movingVertexIndex.HasValue)
		{
			RefreshStatus();
			return;
		}
		PointD point = GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings);
		if (!AreSamePoint(point, _movingVertexPreviewPoint))
		{
			SetVertexPoint(SelectedObject, _movingVertexIndex.Value, point);
			_movingVertexPreviewPoint = point;
			OnPropertyChanged("SelectedObject");
			RequestViewportRedraw();
		}
		StatusText = "Moving vertex " + (_movingVertexIndex.Value + 1);
	}

	public bool EndMoveSelectedVertex()
	{
		if (!_isMovingSelectedVertex || SelectedObject == null || !_movingVertexIndex.HasValue || !_movingVertexObjectId.HasValue)
		{
			ResetVertexMoveState();
			return false;
		}
		Guid objectId = _movingVertexObjectId.Value;
		int vertexIndex = _movingVertexIndex.Value;
		PointD oldPoint = _movingVertexOldPoint;
		PointD newPoint = _movingVertexPreviewPoint;
		SetVertexPoint(SelectedObject, vertexIndex, oldPoint);
		ResetVertexMoveState();
		if (AreSamePoint(oldPoint, newPoint))
		{
			RefreshStatus();
			OnPropertyChanged("SelectedObject");
			RequestViewportRedraw();
			return false;
		}
		History.Execute(new MoveMapObjectVertexCommand(Map, objectId, vertexIndex, oldPoint, newPoint));
		RefreshSelectedObjectReference();
		SelectedVertexIndex = vertexIndex;
		ValidateSelectedVertexIndex();
		StatusText = "Moved vertex " + (vertexIndex + 1);
		OnPropertyChanged("SelectedObject");
		OnPropertyChanged("IsSelectedGeometryEditable");
		NotifyProjectMutated();
		RequestViewportRedraw();
		return true;
	}

	public bool CancelMoveSelectedVertex()
	{
		if (!_isMovingSelectedVertex || SelectedObject == null || !_movingVertexIndex.HasValue)
		{
			return false;
		}
		SetVertexPoint(SelectedObject, _movingVertexIndex.Value, _movingVertexOldPoint);
		ResetVertexMoveState();
		RefreshStatus();
		OnPropertyChanged("SelectedObject");
		RequestViewportRedraw();
		return true;
	}

	public bool SelectVertex(int vertexIndex)
	{
		if (!IsSelectedGeometryEditable || SelectedObject == null || vertexIndex < 0 || vertexIndex >= GetVertexCount(SelectedObject))
		{
			ClearSelectedVertex();
			RefreshStatus();
			return false;
		}
		SelectedVertexIndex = vertexIndex;
		StatusText = FormatSelectedVertexStatus(vertexIndex, GetVertexCount(SelectedObject));
		RequestViewportRedraw();
		return true;
	}

	public bool ClearSelectedVertexSelection()
	{
		if (!SelectedVertexIndex.HasValue)
		{
			return false;
		}
		ClearSelectedVertex();
		RefreshStatus();
		RequestViewportRedraw();
		return true;
	}

	public void SetHoveredVertex(int? vertexIndex)
	{
		int? normalizedIndex = null;
		if (vertexIndex.HasValue && IsSelectedGeometryEditable && SelectedObject != null && vertexIndex.Value >= 0 && vertexIndex.Value < GetVertexCount(SelectedObject))
		{
			normalizedIndex = vertexIndex.Value;
		}
		if (HoveredVertexIndex != normalizedIndex)
		{
			HoveredVertexIndex = normalizedIndex;
			RefreshStatus();
			RequestViewportRedraw();
		}
	}

	public bool AddVertexAtScreenPoint(int segmentStartIndex, PointD screenPoint)
	{
		_lastScreenPoint = screenPoint;
		if (!IsSelectedGeometryEditable || SelectedObject == null || !TryGetInsertIndex(SelectedObject, segmentStartIndex, out int insertIndex))
		{
			RefreshStatus();
			return false;
		}
		PointD point = GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings);
		History.Execute(new AddMapObjectVertexCommand(Map, SelectedObject.Id, insertIndex, point));
		RefreshSelectedObjectReference();
		SelectedVertexIndex = insertIndex;
		ValidateSelectedVertexIndex();
		StatusText = "Added vertex";
		OnPropertyChanged("SelectedObject");
		OnPropertyChanged("IsSelectedGeometryEditable");
		NotifyProjectMutated();
		RequestViewportRedraw();
		return true;
	}

	public bool RemoveSelectedVertex()
	{
		if (!IsSelectedGeometryEditable || SelectedObject == null || !SelectedVertexIndex.HasValue)
		{
			return false;
		}
		int vertexIndex = SelectedVertexIndex.Value;
		int count = GetVertexCount(SelectedObject);
		if (SelectedObject is RoadLine && count <= 2)
		{
			StatusText = "Road must have at least 2 points.";
			return false;
		}
		if (SelectedObject is RoadArea && count <= 3)
		{
			StatusText = "Road area must have at least 3 points.";
			return false;
		}
		if (SelectedObject is DistrictShape && count <= 3)
		{
			StatusText = "District must have at least 3 points.";
			return false;
		}
		History.Execute(new RemoveMapObjectVertexCommand(Map, SelectedObject.Id, vertexIndex));
		RefreshSelectedObjectReference();
		if (SelectedObject != null)
		{
			int newCount = GetVertexCount(SelectedObject);
			SelectedVertexIndex = vertexIndex < newCount ? vertexIndex : newCount - 1;
		}
		ValidateSelectedVertexIndex();
		StatusText = "Removed vertex";
		OnPropertyChanged("SelectedObject");
		OnPropertyChanged("IsSelectedGeometryEditable");
		NotifyProjectMutated();
		RequestViewportRedraw();
		return true;
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
			RoadArea roadArea => roadArea.PolygonPoints.FirstOrDefault(),
			DistrictShape district => district.PolygonPoints.FirstOrDefault(),
			_ => default
		};
	}

	private bool IsLayerEditable(Guid layerId)
	{
		MapLayer? layer = Map.Layers.FirstOrDefault(candidate => candidate.Id == layerId);
		return layer is { IsVisible: true, IsLocked: false };
	}

	private void ClearSelectedVertex()
	{
		SelectedVertexIndex = null;
	}

	private void ResetVertexMoveState()
	{
		_isMovingSelectedVertex = false;
		_movingVertexObjectId = null;
		_movingVertexIndex = null;
		_movingVertexOldPoint = default;
		_movingVertexPreviewPoint = default;
		OnPropertyChanged("IsMovingSelectedVertex");
	}

	private void ValidateSelectedVertexIndex()
	{
		if (SelectedObject == null || !SelectedVertexIndex.HasValue)
		{
			return;
		}
		int count = GetVertexCount(SelectedObject);
		if (SelectedVertexIndex.Value < 0 || SelectedVertexIndex.Value >= count)
		{
			ClearSelectedVertex();
		}
	}

	private static int GetVertexCount(MapObject mapObject)
	{
		return mapObject switch
		{
			RoadLine road => road.Points.Count,
			RoadArea roadArea => roadArea.PolygonPoints.Count,
			DistrictShape district => district.PolygonPoints.Count,
			_ => 0
		};
	}

	private static bool TryGetVertexPoint(MapObject mapObject, int vertexIndex, out PointD point)
	{
		if (mapObject is RoadLine road && vertexIndex >= 0 && vertexIndex < road.Points.Count)
		{
			point = road.Points[vertexIndex];
			return true;
		}
		if (mapObject is RoadArea roadArea && vertexIndex >= 0 && vertexIndex < roadArea.PolygonPoints.Count)
		{
			point = roadArea.PolygonPoints[vertexIndex];
			return true;
		}
		if (mapObject is DistrictShape district && vertexIndex >= 0 && vertexIndex < district.PolygonPoints.Count)
		{
			point = district.PolygonPoints[vertexIndex];
			return true;
		}
		point = default;
		return false;
	}

	private static bool TryGetInsertIndex(MapObject mapObject, int segmentStartIndex, out int insertIndex)
	{
		if (mapObject is RoadLine road && segmentStartIndex >= 0 && segmentStartIndex < road.Points.Count - 1)
		{
			insertIndex = segmentStartIndex + 1;
			return true;
		}
		if (mapObject is RoadArea roadArea && segmentStartIndex >= 0 && segmentStartIndex < roadArea.PolygonPoints.Count)
		{
			insertIndex = segmentStartIndex == roadArea.PolygonPoints.Count - 1 ? roadArea.PolygonPoints.Count : segmentStartIndex + 1;
			return true;
		}
		if (mapObject is DistrictShape district && segmentStartIndex >= 0 && segmentStartIndex < district.PolygonPoints.Count)
		{
			insertIndex = segmentStartIndex == district.PolygonPoints.Count - 1 ? district.PolygonPoints.Count : segmentStartIndex + 1;
			return true;
		}
		insertIndex = -1;
		return false;
	}

	private static void SetVertexPoint(MapObject mapObject, int vertexIndex, PointD point)
	{
		if (mapObject is RoadLine road)
		{
			road.SetPoint(vertexIndex, point);
			return;
		}
		if (mapObject is RoadArea roadArea)
		{
			roadArea.SetPoint(vertexIndex, point);
			return;
		}
		if (mapObject is DistrictShape district)
		{
			district.SetPoint(vertexIndex, point);
			return;
		}
		throw new NotSupportedException("Vertex editing is not supported for object type '" + mapObject.GetType().Name + "'.");
	}

	private static bool AreSamePoint(PointD first, PointD second)
	{
		return Math.Abs(first.X - second.X) < 1E-06 && Math.Abs(first.Y - second.Y) < 1E-06;
	}

	private static string FormatSelectedVertexStatus(int vertexIndex, int vertexCount)
	{
		return "Selected vertex " + (vertexIndex + 1) + "/" + vertexCount;
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
		NotifyProjectMutated();
	}

	public void NotifyMapPropertiesChanged()
	{
		OnPropertyChanged(nameof(Map));
		NotifyProjectMutated();
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
		ValidateSelectedVertexIndex();
		StatusText = "Undo: " + editorCommand.Description;
		OnPropertyChanged("SelectedObject");
		OnPropertyChanged("IsSelectedGeometryEditable");
		NotifyProjectMutated();
		RequestViewportRedraw();
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
		ValidateSelectedVertexIndex();
		StatusText = "Redo: " + editorCommand.Description;
		OnPropertyChanged("SelectedObject");
		OnPropertyChanged("IsSelectedGeometryEditable");
		NotifyProjectMutated();
		RequestViewportRedraw();
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
			_roadDrawingSession.UpdatePreviewPoint(SnapCreationPoint(screenPoint, includeParentRoadOverlaySnap: true));
			OnPropertyChanged("RoadPreviewPoint");
		}
		if (ActiveTool == EditorToolType.RoadArea && _roadAreaDrawingSession.IsDrawing)
		{
			_roadAreaDrawingSession.UpdatePreviewPoint(SnapCreationPoint(screenPoint, includeParentRoadOverlaySnap: true));
			OnPropertyChanged("RoadAreaPreviewPoint");
		}
		if (ActiveTool == EditorToolType.District && _districtDrawingSession.IsDrawing)
		{
			_districtDrawingSession.UpdatePreviewPoint(GridSnapper.Snap(Camera.ScreenToWorld(screenPoint), Map.GridSettings));
			OnPropertyChanged("DistrictPreviewPoint");
		}
		RefreshStatus();
	}

	private PointD SnapCreationPoint(PointD screenPoint, bool includeParentRoadOverlaySnap)
	{
		PointD rawWorldPoint = Camera.ScreenToWorld(screenPoint);
		PointD gridSnappedPoint = GridSnapper.Snap(rawWorldPoint, Map.GridSettings);
		if (!includeParentRoadOverlaySnap || !Map.GridSettings.IsEnabled || !Map.GridSettings.SnapToGrid)
		{
			return gridSnappedPoint;
		}

		return ParentRoadOverlaySnapper.SnapToNearestOverlayVertex(
			rawWorldPoint,
			gridSnappedPoint,
			GetParentRoadOverlays(),
			Camera,
			ParentRoadOverlaySnapTolerancePixels);
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
		if (_isMovingSelectedVertex && SelectedObject != null && SelectedVertexIndex.HasValue)
		{
			return $"{coordinates} | {toolText} | Moving vertex {SelectedVertexIndex.Value + 1}";
		}
		if (SelectedObject != null && SelectedVertexIndex.HasValue)
		{
			return $"{coordinates} | {toolText} | {FormatSelectedVertexStatus(SelectedVertexIndex.Value, GetVertexCount(SelectedObject))}";
		}
		if (SelectedObject != null && HoveredVertexIndex.HasValue)
		{
			return $"{coordinates} | {toolText} | Vertex {HoveredVertexIndex.Value + 1}";
		}
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
		if (ActiveTool == EditorToolType.RoadArea)
		{
			string roadAreaText = _roadAreaDrawingSession.IsDrawing ? "Road Area Tool: click to add vertex, Enter to finish, Esc to cancel" : "Road Area Tool: click to start polygon";
			if (SelectedObject != null)
			{
				return $"{coordinates} | {toolText} | {roadAreaText} | Selected: {SelectedObject.Name}";
			}

			return $"{coordinates} | {toolText} | {roadAreaText}";
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
			EditorToolType.RoadArea => "Road Area", 
			EditorToolType.PointOfInterest => "POI", 
			EditorToolType.Label => "Label", 
			_ => tool.ToString(), 
		};
	}

	private void RefreshSelectedObjectReference()
	{
		if (SelectedObject == null)
		{
			ClearSelectedVertex();
			HoveredVertexIndex = null;
			OnPropertyChanged("IsSelectedGeometryEditable");
			return;
		}
		foreach (MapLayer layer in Map.Layers)
		{
			MapObject? mapObject = layer.Objects.FirstOrDefault((MapObject candidate) => candidate.Id == SelectedObject.Id);
			if (mapObject != null)
			{
				SelectedObject = mapObject;
				ValidateSelectedVertexIndex();
				OnPropertyChanged("IsSelectedGeometryEditable");
				return;
			}
		}
		SelectedObject = null;
		ClearSelectedVertex();
		HoveredVertexIndex = null;
		OnPropertyChanged("IsSelectedGeometryEditable");
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

	private bool CancelRoadAreaDrawingCore()
	{
		if (!_roadAreaDrawingSession.IsDrawing && !_roadAreaDrawingSession.PreviewPoint.HasValue)
		{
			return false;
		}
		_roadAreaDrawingSession.Cancel();
		NotifyRoadAreaPreviewChanged();
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

	private void NotifyRoadAreaPreviewChanged()
	{
		OnPropertyChanged("RoadAreaPreviewPoints");
		OnPropertyChanged("RoadAreaPreviewPoint");
		OnPropertyChanged("IsDrawingRoadArea");
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
