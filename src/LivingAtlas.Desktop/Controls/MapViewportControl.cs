using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Hierarchy;
using LivingAtlas.Editor.Tools;
using LivingAtlas.Editor.Viewport;

namespace LivingAtlas.Desktop.Controls;

public sealed class MapViewportControl : Control
{
	private const double ClickDragThresholdPixels = 4.0;

	private const double HitTestScreenTolerancePixels = 10.0;

	private const double VertexHandleRadiusPixels = 5.5;

	private const double VertexHandleHitTolerancePixels = 9.0;

	private const double ScaleBarMeters = 100.0;

	private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(31, 34, 39));

	private static readonly IBrush MapFillBrush = new SolidColorBrush(Color.FromRgb(38, 43, 50));

	private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(230, 234, 240));

	private static readonly IBrush MutedTextBrush = new SolidColorBrush(Color.FromRgb(178, 187, 198));

	private static readonly IBrush DistrictFillBrush = new SolidColorBrush(Color.FromArgb(72, 87, 155, 137));

	private static readonly IBrush DistrictPreviewFillBrush = new SolidColorBrush(Color.FromArgb(42, 166, 245, 213));

	private static readonly IBrush PoiFillBrush = new SolidColorBrush(Color.FromRgb(238, 200, 96));

	private static readonly IBrush LabelTextBrush = new SolidColorBrush(Color.FromRgb(245, 240, 224));

	private static readonly IBrush ChildPreviewDistrictFillBrush = new SolidColorBrush(Color.FromArgb(36, 143, 170, 208));

	private static readonly IBrush ChildPreviewPoiFillBrush = new SolidColorBrush(Color.FromArgb(210, 185, 210, 226));

	private static readonly IBrush ChildPreviewTextBrush = new SolidColorBrush(Color.FromArgb(210, 199, 211, 222));

	private static readonly Pen MinorGridPen = new Pen(new SolidColorBrush(Color.FromArgb(90, 72, 78, 88)));

	private static readonly Pen MajorGridPen = new Pen(new SolidColorBrush(Color.FromArgb(140, 95, 104, 118)));

	private static readonly Pen MapBoundsPen = new Pen(new SolidColorBrush(Color.FromRgb(218, 225, 232)), 2.0);

	private static readonly Pen ScaleBarPen = new Pen(new SolidColorBrush(Color.FromRgb(230, 234, 240)), 2.0);

	private static readonly Pen DistrictPen = new Pen(new SolidColorBrush(Color.FromRgb(94, 181, 154)), 2.0);

	private static readonly Pen DistrictPreviewPen = new Pen(new SolidColorBrush(Color.FromArgb(210, 166, 245, 213)), 2.0);

	private static readonly Pen RoadPen = new Pen(new SolidColorBrush(Color.FromRgb(224, 186, 118)), 4.0);

	private static readonly Pen PoiPen = new Pen(new SolidColorBrush(Color.FromRgb(42, 45, 50)), 2.0);

	private static readonly Pen RoadPreviewPen = new Pen(new SolidColorBrush(Color.FromArgb(190, byte.MaxValue, 241, 179)), 2.0);

	private static readonly Pen SelectedDistrictPen = new Pen(new SolidColorBrush(Color.FromRgb(166, 245, 213)), 4.0);

	private static readonly Pen SelectedRoadPen = new Pen(new SolidColorBrush(Color.FromRgb(byte.MaxValue, 225, 144)), 7.0);

	private static readonly Pen SelectedPoiRingPen = new Pen(new SolidColorBrush(Color.FromRgb(byte.MaxValue, 241, 179)), 3.0);

	private static readonly Pen SelectedLabelPen = new Pen(new SolidColorBrush(Color.FromRgb(byte.MaxValue, 241, 179)), 2.0);

	private static readonly Pen ChildPreviewDistrictPen = new Pen(new SolidColorBrush(Color.FromArgb(190, 143, 170, 208)));

	private static readonly Pen ChildPreviewRoadPen = new Pen(new SolidColorBrush(Color.FromArgb(205, 194, 185, 160)), 2.0);

	private static readonly Pen ChildPreviewPoiPen = new Pen(new SolidColorBrush(Color.FromArgb(190, 44, 50, 58)));

	private static readonly IBrush VertexHandleFillBrush = new SolidColorBrush(Color.FromRgb(245, 247, 250));

	private static readonly IBrush SelectedVertexHandleFillBrush = new SolidColorBrush(Color.FromRgb(byte.MaxValue, 241, 179));

	private static readonly IBrush HoveredVertexHandleFillBrush = new SolidColorBrush(Color.FromRgb(166, 245, 213));

	private static readonly Pen VertexHandlePen = new Pen(new SolidColorBrush(Color.FromRgb(44, 50, 58)), 2.0);

	private readonly record struct DistrictVisualStyle(IBrush Fill, Pen Stroke);

	private readonly record struct RoadVisualStyle(Pen Stroke);

	private readonly record struct PoiVisualStyle(IBrush Fill, Pen Stroke, double Radius);

	private readonly record struct LabelVisualStyle(IBrush Brush, double FontSize, FontWeight FontWeight);

	private bool _isPointerDown;

	private bool _isMovingObject;

	private bool _isMovingVertex;

	private bool _isObjectDragCandidate;

	private bool _isPanning;

	private bool _hasDragged;

	private Point _panStartPoint;

	private Point _lastPanPoint;

	public MapViewportControl()
	{
		base.ClipToBounds = true;
		base.Focusable = true;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == DataContextProperty)
		{
			if (change.OldValue is MapViewportViewModel oldVm)
			{
				oldVm.RedrawRequested -= OnRedrawRequested;
			}
			if (change.NewValue is MapViewportViewModel newVm)
			{
				newVm.RedrawRequested -= OnRedrawRequested;
				newVm.RedrawRequested += OnRedrawRequested;
			}
		}
	}

	private void OnRedrawRequested(object? sender, EventArgs e)
	{
		InvalidateVisual();
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		Rect bounds = base.Bounds;
		context.DrawRectangle(BackgroundBrush, null, bounds);
		if (base.DataContext is MapViewportViewModel mapViewportViewModel)
		{
			if (bounds.Width > 0.0 && bounds.Height > 0.0)
			{
				mapViewportViewModel.EnsureInitialCameraFit(new SizeD(bounds.Width, bounds.Height));
			}
			DrawMapArea(context, mapViewportViewModel.Camera, mapViewportViewModel.Map.RealSizeMeters);
			DrawGrid(context, bounds, mapViewportViewModel.Camera, mapViewportViewModel.GridStepMeters);
			DrawMapObjects(context, mapViewportViewModel.Camera, mapViewportViewModel.Map, mapViewportViewModel.Project, mapViewportViewModel.SelectedObjectId);
			DrawVertexHandles(context, mapViewportViewModel);
			DrawDistrictPreview(context, mapViewportViewModel.Camera, mapViewportViewModel.DistrictPreviewPoints, mapViewportViewModel.DistrictPreviewPoint);
			DrawRoadPreview(context, mapViewportViewModel.Camera, mapViewportViewModel.RoadPreviewPoints, mapViewportViewModel.RoadPreviewPoint);
			DrawMapBounds(context, mapViewportViewModel.Camera, mapViewportViewModel.Map.RealSizeMeters);
			DrawTitle(context, mapViewportViewModel.Map.Name);
			DrawScaleBar(context, bounds, mapViewportViewModel.Camera);
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		PointerPoint currentPoint = e.GetCurrentPoint(this);
		if (currentPoint.Properties.IsLeftButtonPressed)
		{
			MapViewportViewModel? mapViewportViewModel = base.DataContext as MapViewportViewModel;
			if (mapViewportViewModel != null && mapViewportViewModel.ActiveTool == EditorToolType.PointOfInterest)
			{
				Focus();
				mapViewportViewModel.CreatePointOfInterestAtScreenPoint(ToPointD(currentPoint.Position));
				InvalidateVisual();
				e.Handled = true;
				return;
			}
			if (mapViewportViewModel != null && mapViewportViewModel.ActiveTool == EditorToolType.Label)
			{
				Focus();
				mapViewportViewModel.CreateLabelAtScreenPoint(ToPointD(currentPoint.Position));
				InvalidateVisual();
				e.Handled = true;
				return;
			}
			if (mapViewportViewModel != null && mapViewportViewModel.ActiveTool == EditorToolType.Road)
			{
				Focus();
				mapViewportViewModel.AddRoadPointAtScreenPoint(ToPointD(currentPoint.Position));
				InvalidateVisual();
				e.Handled = true;
				return;
			}
			if (mapViewportViewModel != null && mapViewportViewModel.ActiveTool == EditorToolType.District)
			{
				Focus();
				mapViewportViewModel.AddDistrictPointAtScreenPoint(ToPointD(currentPoint.Position));
				InvalidateVisual();
				e.Handled = true;
				return;
			}
			if (mapViewportViewModel != null
				&& mapViewportViewModel.IsSelectedGeometryEditable
				&& TryHitSelectedVertexHandle(mapViewportViewModel, currentPoint.Position, out int vertexIndex)
				&& mapViewportViewModel.BeginMoveSelectedVertex(vertexIndex, ToPointD(currentPoint.Position)))
			{
				Focus();
				_isPointerDown = true;
				_isMovingObject = false;
				_isMovingVertex = true;
				_isObjectDragCandidate = false;
				_isPanning = false;
				_hasDragged = false;
				_panStartPoint = currentPoint.Position;
				_lastPanPoint = currentPoint.Position;
				e.Pointer.Capture(this);
				InvalidateVisual();
				e.Handled = true;
				return;
			}
			if (mapViewportViewModel != null
				&& e.ClickCount == 2
				&& mapViewportViewModel.IsSelectedGeometryEditable
				&& TryHitSelectedSegment(mapViewportViewModel, currentPoint.Position, out int segmentStartIndex)
				&& mapViewportViewModel.AddVertexAtScreenPoint(segmentStartIndex, ToPointD(currentPoint.Position)))
			{
				Focus();
				InvalidateVisual();
				e.Handled = true;
				return;
			}
			MapObject? mapObject = ((mapViewportViewModel != null && mapViewportViewModel.Tools.AllowsSelectionChanges) ? mapViewportViewModel.SelectAtScreenPoint(ToPointD(currentPoint.Position), 10.0) : null);
			Focus();
			_isPointerDown = true;
			_isMovingObject = false;
			_isMovingVertex = false;
			_isObjectDragCandidate = mapViewportViewModel != null && mapViewportViewModel.Tools.AllowsObjectMove && mapObject != null;
			_isPanning = false;
			_hasDragged = false;
			_panStartPoint = currentPoint.Position;
			_lastPanPoint = currentPoint.Position;
			e.Pointer.Capture(this);
			InvalidateVisual();
			e.Handled = true;
		}
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		if (!(base.DataContext is MapViewportViewModel mapViewportViewModel))
		{
			return;
		}
		Point position = e.GetPosition(this);
		if (_isPointerDown)
		{
			if (_isMovingVertex)
			{
				mapViewportViewModel.MoveSelectedVertexToScreenPoint(ToPointD(position));
			}
			else if (!_hasDragged && Distance(_panStartPoint, position) > 4.0)
			{
				_hasDragged = true;
				if (_isObjectDragCandidate)
				{
					_isMovingObject = true;
					mapViewportViewModel.BeginMoveSelectedObject(ToPointD(position));
				}
				else if (mapViewportViewModel.Tools.AllowsViewportPanFromDrag)
				{
					_isPanning = true;
				}
			}
			if (_isMovingObject)
			{
				mapViewportViewModel.MoveSelectedObjectByScreenDelta(ToPointD(position), position.X - _lastPanPoint.X, position.Y - _lastPanPoint.Y);
			}
			else if (_isPanning)
			{
				mapViewportViewModel.PanBy(position.X - _lastPanPoint.X, position.Y - _lastPanPoint.Y);
			}
			_lastPanPoint = position;
			InvalidateVisual();
		}
		else if (mapViewportViewModel.IsSelectedGeometryEditable && TryHitSelectedVertexHandle(mapViewportViewModel, position, out int hoveredVertexIndex))
		{
			mapViewportViewModel.SetHoveredVertex(hoveredVertexIndex);
		}
		else
		{
			mapViewportViewModel.SetHoveredVertex(null);
		}
		mapViewportViewModel.UpdatePointerPosition(ToPointD(position));
		if (mapViewportViewModel.IsDrawingRoad || mapViewportViewModel.IsDrawingDistrict)
		{
			InvalidateVisual();
		}
		e.Handled = true;
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		if (_isPointerDown)
		{
			if (_isMovingObject && base.DataContext is MapViewportViewModel mapViewportViewModel)
			{
				mapViewportViewModel.EndMoveSelectedObject();
				InvalidateVisual();
			}
			if (_isMovingVertex && base.DataContext is MapViewportViewModel vertexViewModel)
			{
				vertexViewModel.EndMoveSelectedVertex();
				InvalidateVisual();
			}
			_isPointerDown = false;
			_isMovingObject = false;
			_isMovingVertex = false;
			_isObjectDragCandidate = false;
			_isPanning = false;
			e.Pointer.Capture(null);
			e.Handled = true;
		}
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost(e);
		if (_isMovingObject && base.DataContext is MapViewportViewModel mapViewportViewModel)
		{
			mapViewportViewModel.EndMoveSelectedObject();
		}
		if (_isMovingVertex && base.DataContext is MapViewportViewModel vertexViewModel)
		{
			vertexViewModel.CancelMoveSelectedVertex();
		}
		_isPointerDown = false;
		_isMovingObject = false;
		_isMovingVertex = false;
		_isObjectDragCandidate = false;
		_isPanning = false;
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		base.OnPointerWheelChanged(e);
		if (base.DataContext is MapViewportViewModel mapViewportViewModel)
		{
			PointD screenPoint = ToPointD(e.GetPosition(this));
			double zoomFactor = Math.Pow(1.1, e.Delta.Y);
			mapViewportViewModel.ZoomAt(screenPoint, zoomFactor);
			InvalidateVisual();
			e.Handled = true;
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (EditorHotkeyGuard.ShouldIgnoreEditorHotkeys(e))
		{
			return;
		}
		if (!(base.DataContext is MapViewportViewModel mapViewportViewModel))
		{
			return;
		}
		if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Return && mapViewportViewModel.ActiveTool == EditorToolType.Road)
		{
			mapViewportViewModel.TryFinishRoadDrawing();
			InvalidateVisual();
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Return && mapViewportViewModel.ActiveTool == EditorToolType.District)
		{
			mapViewportViewModel.TryFinishDistrictDrawing();
			InvalidateVisual();
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Escape && mapViewportViewModel.ActiveTool == EditorToolType.Road)
		{
			mapViewportViewModel.CancelRoadDrawing();
			InvalidateVisual();
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Escape && mapViewportViewModel.ActiveTool == EditorToolType.District)
		{
			mapViewportViewModel.CancelDistrictDrawing();
			InvalidateVisual();
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Escape && mapViewportViewModel.CancelMoveSelectedVertex())
		{
			_isMovingVertex = false;
			InvalidateVisual();
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Escape && mapViewportViewModel.ClearSelectedVertexSelection())
		{
			InvalidateVisual();
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Delete)
		{
			if (mapViewportViewModel.SelectedVertexIndex.HasValue)
			{
				mapViewportViewModel.RemoveSelectedVertex();
				InvalidateVisual();
			}
			else if (mapViewportViewModel.DeleteSelectedObject())
			{
				InvalidateVisual();
			}
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && e.Key == Key.Back)
		{
			if (mapViewportViewModel.DeleteSelectedObject())
			{
				InvalidateVisual();
			}
			e.Handled = true;
		}
		else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Z)
		{
			if (mapViewportViewModel.Undo())
			{
				InvalidateVisual();
			}
			e.Handled = true;
		}
		else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Y)
		{
			if (mapViewportViewModel.Redo())
			{
				InvalidateVisual();
			}
			e.Handled = true;
		}
		else if (e.KeyModifiers == KeyModifiers.None && TrySetToolFromKey(mapViewportViewModel, e.Key))
		{
			InvalidateVisual();
			e.Handled = true;
		}
	}

	private static void DrawGrid(DrawingContext context, Rect bounds, Camera2D camera, double baseGridStepMeters)
	{
		double visibleGridStepMeters = GetVisibleGridStepMeters(baseGridStepMeters, camera.Zoom);
		PointD pointD = camera.ScreenToWorld(new PointD(0.0, 0.0));
		PointD pointD2 = camera.ScreenToWorld(new PointD(bounds.Width, bounds.Height));
		double num = Math.Min(pointD.X, pointD2.X);
		double num2 = Math.Max(pointD.X, pointD2.X);
		double num3 = Math.Min(pointD.Y, pointD2.Y);
		double num4 = Math.Max(pointD.Y, pointD2.Y);
		double num5 = Math.Floor(num / visibleGridStepMeters) * visibleGridStepMeters;
		double num6 = Math.Floor(num3 / visibleGridStepMeters) * visibleGridStepMeters;
		for (double num7 = num5; num7 <= num2; num7 += visibleGridStepMeters)
		{
			double x = camera.WorldToScreen(new PointD(num7, 0.0)).X;
			Pen pen = (IsMajorLine(num7, baseGridStepMeters) ? MajorGridPen : MinorGridPen);
			context.DrawLine(pen, new Point(x, 0.0), new Point(x, bounds.Height));
		}
		for (double num8 = num6; num8 <= num4; num8 += visibleGridStepMeters)
		{
			double y = camera.WorldToScreen(new PointD(0.0, num8)).Y;
			Pen pen2 = (IsMajorLine(num8, baseGridStepMeters) ? MajorGridPen : MinorGridPen);
			context.DrawLine(pen2, new Point(0.0, y), new Point(bounds.Width, y));
		}
	}

	private static void DrawMapArea(DrawingContext context, Camera2D camera, SizeD mapSizeMeters)
	{
		context.DrawRectangle(MapFillBrush, null, ToScreenRect(camera, mapSizeMeters));
	}

	private static void DrawMapBounds(DrawingContext context, Camera2D camera, SizeD mapSizeMeters)
	{
		context.DrawRectangle(null, MapBoundsPen, ToScreenRect(camera, mapSizeMeters));
	}

	private static void DrawMapObjects(DrawingContext context, Camera2D camera, MapDocument map, CampaignMapProject? project, Guid? selectedObjectId)
	{
		foreach (MapLayer layer in map.Layers)
		{
			if (!layer.IsVisible)
			{
				continue;
			}
			foreach (MapObject @object in layer.Objects)
			{
				bool isSelected = @object.Id == selectedObjectId;
				MapObject mapObject = @object;
				MapObject mapObject2 = mapObject;
				if (!(mapObject2 is DistrictShape districtShape))
				{
					if (!(mapObject2 is RoadLine road))
					{
						if (!(mapObject2 is PointOfInterest poi))
						{
							if (mapObject2 is MapLabel label)
							{
								DrawMapLabel(context, camera, label, isSelected);
							}
						}
						else
						{
							DrawPointOfInterest(context, camera, poi, isSelected);
						}
					}
					else
					{
						DrawRoadLine(context, camera, road, isSelected);
					}
				}
				else
				{
					DrawDistrictShape(context, camera, districtShape, isSelected);
					DrawChildMapPreview(context, camera, project, map, districtShape);
				}
			}
		}
	}

	private static void DrawVertexHandles(DrawingContext context, MapViewportViewModel viewModel)
	{
		if (!viewModel.IsSelectedGeometryEditable || viewModel.SelectedObject == null)
		{
			return;
		}
		IReadOnlyList<PointD> points = GetVertexPoints(viewModel.SelectedObject);
		for (int i = 0; i < points.Count; i++)
		{
			Point center = ToAvaloniaPoint(viewModel.Camera.WorldToScreen(points[i]));
			IBrush fill = viewModel.SelectedVertexIndex == i
				? SelectedVertexHandleFillBrush
				: viewModel.HoveredVertexIndex == i ? HoveredVertexHandleFillBrush : VertexHandleFillBrush;
			context.DrawEllipse(fill, VertexHandlePen, center, VertexHandleRadiusPixels, VertexHandleRadiusPixels);
		}
	}

	private static bool TryHitSelectedVertexHandle(MapViewportViewModel viewModel, Point screenPoint, out int vertexIndex)
	{
		vertexIndex = -1;
		if (!viewModel.IsSelectedGeometryEditable || viewModel.SelectedObject == null)
		{
			return false;
		}
		IReadOnlyList<PointD> points = GetVertexPoints(viewModel.SelectedObject);
		double bestDistance = double.PositiveInfinity;
		for (int i = 0; i < points.Count; i++)
		{
			Point center = ToAvaloniaPoint(viewModel.Camera.WorldToScreen(points[i]));
			double distance = Distance(screenPoint, center);
			if (distance <= VertexHandleHitTolerancePixels && distance < bestDistance)
			{
				bestDistance = distance;
				vertexIndex = i;
			}
		}
		return vertexIndex >= 0;
	}

	private static bool TryHitSelectedSegment(MapViewportViewModel viewModel, Point screenPoint, out int segmentStartIndex)
	{
		segmentStartIndex = -1;
		if (!viewModel.IsSelectedGeometryEditable || viewModel.SelectedObject == null)
		{
			return false;
		}
		IReadOnlyList<PointD> points = GetVertexPoints(viewModel.SelectedObject);
		int segmentCount = viewModel.SelectedObject is DistrictShape ? points.Count : Math.Max(0, points.Count - 1);
		double bestDistance = double.PositiveInfinity;
		for (int i = 0; i < segmentCount; i++)
		{
			Point start = ToAvaloniaPoint(viewModel.Camera.WorldToScreen(points[i]));
			Point end = ToAvaloniaPoint(viewModel.Camera.WorldToScreen(points[(i + 1) % points.Count]));
			double distance = DistanceToSegment(screenPoint, start, end);
			if (distance <= HitTestScreenTolerancePixels && distance < bestDistance)
			{
				bestDistance = distance;
				segmentStartIndex = i;
			}
		}
		return segmentStartIndex >= 0;
	}

	private static IReadOnlyList<PointD> GetVertexPoints(MapObject mapObject)
	{
		return mapObject switch
		{
			RoadLine road => road.Points,
			DistrictShape district => district.PolygonPoints,
			_ => Array.Empty<PointD>()
		};
	}

	private static void DrawDistrictShape(DrawingContext context, Camera2D camera, DistrictShape district, bool isSelected)
	{
		DistrictVisualStyle style = GetDistrictStyle(district.StyleKey);
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			streamGeometryContext.BeginFigure(ToAvaloniaPoint(camera.WorldToScreen(district.PolygonPoints[0])));
			for (int i = 1; i < district.PolygonPoints.Count; i++)
			{
				streamGeometryContext.LineTo(ToAvaloniaPoint(camera.WorldToScreen(district.PolygonPoints[i])));
			}
			streamGeometryContext.EndFigure(isClosed: true);
		}
		context.DrawGeometry(style.Fill, style.Stroke, streamGeometry);
		if (isSelected)
		{
			context.DrawGeometry(null, SelectedDistrictPen, streamGeometry);
		}
	}

	private static void DrawRoadLine(DrawingContext context, Camera2D camera, RoadLine road, bool isSelected)
	{
		Pen pen = GetRoadStyle(road.StyleKey).Stroke;
		for (int i = 1; i < road.Points.Count; i++)
		{
			Point p = ToAvaloniaPoint(camera.WorldToScreen(road.Points[i - 1]));
			Point p2 = ToAvaloniaPoint(camera.WorldToScreen(road.Points[i]));
			if (isSelected)
			{
				context.DrawLine(SelectedRoadPen, p, p2);
			}
			context.DrawLine(pen, p, p2);
		}
	}

	private static void DrawChildMapPreview(DrawingContext context, Camera2D camera, CampaignMapProject? project, MapDocument parentMap, DistrictShape parentDistrict)
	{
		if (project == null)
		{
			return;
		}
		Guid? childMapId = parentDistrict.ChildMapId;
		Guid valueOrDefault = default(Guid);
		int num;
		if (childMapId.HasValue)
		{
			valueOrDefault = childMapId.GetValueOrDefault();
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num == 0)
		{
			return;
		}
		MapDocument? mapDocument = project.FindMap(valueOrDefault);
		if (mapDocument == null || mapDocument.Id == parentMap.Id || mapDocument.RealSizeMeters.Width <= 0.0 || mapDocument.RealSizeMeters.Height <= 0.0)
		{
			return;
		}
		RectD boundingBox = GetBoundingBox(parentDistrict.PolygonPoints);
		if (boundingBox.Size.Width <= 0.0 || boundingBox.Size.Height <= 0.0)
		{
			return;
		}
		foreach (MapLayer layer in mapDocument.Layers)
		{
			if (!layer.IsVisible)
			{
				continue;
			}
			foreach (MapObject @object in layer.Objects)
			{
				MapObject mapObject = @object;
				MapObject mapObject2 = mapObject;
				if (!(mapObject2 is DistrictShape district))
				{
					if (!(mapObject2 is RoadLine road))
					{
						if (!(mapObject2 is PointOfInterest poi))
						{
							if (mapObject2 is MapLabel label)
							{
								DrawChildPreviewLabel(context, camera, boundingBox, mapDocument.RealSizeMeters, label);
							}
						}
						else
						{
							DrawChildPreviewPointOfInterest(context, camera, boundingBox, mapDocument.RealSizeMeters, poi);
						}
					}
					else
					{
						DrawChildPreviewRoad(context, camera, boundingBox, mapDocument.RealSizeMeters, road);
					}
				}
				else
				{
					DrawChildPreviewDistrict(context, camera, boundingBox, mapDocument.RealSizeMeters, district);
				}
			}
		}
	}

	private static void DrawChildPreviewDistrict(DrawingContext context, Camera2D camera, RectD parentBounds, SizeD childSize, DistrictShape district)
	{
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			streamGeometryContext.BeginFigure(ToPreviewScreenPoint(camera, district.PolygonPoints[0], parentBounds, childSize));
			for (int i = 1; i < district.PolygonPoints.Count; i++)
			{
				streamGeometryContext.LineTo(ToPreviewScreenPoint(camera, district.PolygonPoints[i], parentBounds, childSize));
			}
			streamGeometryContext.EndFigure(isClosed: true);
		}
		context.DrawGeometry(ChildPreviewDistrictFillBrush, ChildPreviewDistrictPen, streamGeometry);
	}

	private static void DrawChildPreviewRoad(DrawingContext context, Camera2D camera, RectD parentBounds, SizeD childSize, RoadLine road)
	{
		for (int i = 1; i < road.Points.Count; i++)
		{
			context.DrawLine(ChildPreviewRoadPen, ToPreviewScreenPoint(camera, road.Points[i - 1], parentBounds, childSize), ToPreviewScreenPoint(camera, road.Points[i], parentBounds, childSize));
		}
	}

	private static void DrawChildPreviewPointOfInterest(DrawingContext context, Camera2D camera, RectD parentBounds, SizeD childSize, PointOfInterest poi)
	{
		Point center = ToPreviewScreenPoint(camera, poi.Position, parentBounds, childSize);
		context.DrawEllipse(ChildPreviewPoiFillBrush, ChildPreviewPoiPen, center, 4.0, 4.0);
	}

	private static void DrawChildPreviewLabel(DrawingContext context, Camera2D camera, RectD parentBounds, SizeD childSize, MapLabel label)
	{
		Point origin = ToPreviewScreenPoint(camera, label.Position, parentBounds, childSize);
		DrawText(context, label.Text, origin, 10.0, ChildPreviewTextBrush);
	}

	private static Point ToPreviewScreenPoint(Camera2D camera, PointD childPoint, RectD parentBounds, SizeD childSize)
	{
		PointD worldPoint = ChildMapPreviewTransform.ChildToParent(childPoint, parentBounds, childSize);
		return ToAvaloniaPoint(camera.WorldToScreen(worldPoint));
	}

	private static void DrawDistrictPreview(DrawingContext context, Camera2D camera, IReadOnlyList<PointD> points, PointD? previewPoint)
	{
		if (points.Count == 0)
		{
			return;
		}
		IReadOnlyList<PointD> readOnlyList = BuildPreviewPolygon(points, previewPoint);
		if (readOnlyList.Count >= 3)
		{
			StreamGeometry streamGeometry = new StreamGeometry();
			using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
			{
				streamGeometryContext.BeginFigure(ToAvaloniaPoint(camera.WorldToScreen(readOnlyList[0])));
				for (int i = 1; i < readOnlyList.Count; i++)
				{
					streamGeometryContext.LineTo(ToAvaloniaPoint(camera.WorldToScreen(readOnlyList[i])));
				}
				streamGeometryContext.EndFigure(isClosed: true);
			}
			context.DrawGeometry(DistrictPreviewFillBrush, DistrictPreviewPen, streamGeometry);
		}
		for (int j = 1; j < points.Count; j++)
		{
			context.DrawLine(DistrictPreviewPen, ToAvaloniaPoint(camera.WorldToScreen(points[j - 1])), ToAvaloniaPoint(camera.WorldToScreen(points[j])));
		}
		if (previewPoint.HasValue)
		{
			context.DrawLine(DistrictPreviewPen, ToAvaloniaPoint(camera.WorldToScreen(points[points.Count - 1])), ToAvaloniaPoint(camera.WorldToScreen(previewPoint.Value)));
		}
		if (points.Count >= 3)
		{
			PointD? pointD = previewPoint;
			PointD pointD2;
			if (!pointD.HasValue)
			{
				pointD2 = points[points.Count - 1];
			}
			else
			{
				pointD2 = pointD.GetValueOrDefault();
			}
			PointD worldPoint = pointD2;
			context.DrawLine(DistrictPreviewPen, ToAvaloniaPoint(camera.WorldToScreen(worldPoint)), ToAvaloniaPoint(camera.WorldToScreen(points[0])));
		}
		foreach (PointD point in points)
		{
			context.DrawEllipse(PoiFillBrush, null, ToAvaloniaPoint(camera.WorldToScreen(point)), 4.0, 4.0);
		}
	}

	private static IReadOnlyList<PointD> BuildPreviewPolygon(IReadOnlyList<PointD> points, PointD? previewPoint)
	{
		if (!previewPoint.HasValue)
		{
			return points;
		}
		List<PointD> list = new List<PointD>(points.Count + 1);
		list.AddRange(points);
		list.Add(previewPoint.Value);
		return list;
	}

	private static void DrawRoadPreview(DrawingContext context, Camera2D camera, IReadOnlyList<PointD> points, PointD? previewPoint)
	{
		if (points.Count == 0)
		{
			return;
		}
		for (int i = 1; i < points.Count; i++)
		{
			context.DrawLine(RoadPreviewPen, ToAvaloniaPoint(camera.WorldToScreen(points[i - 1])), ToAvaloniaPoint(camera.WorldToScreen(points[i])));
		}
		if (previewPoint.HasValue)
		{
			context.DrawLine(RoadPreviewPen, ToAvaloniaPoint(camera.WorldToScreen(points[points.Count - 1])), ToAvaloniaPoint(camera.WorldToScreen(previewPoint.Value)));
		}
		foreach (PointD point in points)
		{
			context.DrawEllipse(PoiFillBrush, null, ToAvaloniaPoint(camera.WorldToScreen(point)), 4.0, 4.0);
		}
	}

	private static void DrawPointOfInterest(DrawingContext context, Camera2D camera, PointOfInterest poi, bool isSelected)
	{
		PoiVisualStyle style = GetPoiStyle(poi.StyleKey);
		Point center = ToAvaloniaPoint(camera.WorldToScreen(poi.Position));
		if (isSelected)
		{
			double selectedRadius = style.Radius + 5.0;
			context.DrawEllipse(null, SelectedPoiRingPen, center, selectedRadius, selectedRadius);
		}
		context.DrawEllipse(style.Fill, style.Stroke, center, style.Radius, style.Radius);
		DrawText(context, poi.Name, new Point(center.X + 12.0, center.Y - 8.0), 12.0, TextBrush);
	}

	private static void DrawMapLabel(DrawingContext context, Camera2D camera, MapLabel label, bool isSelected)
	{
		LabelVisualStyle style = GetLabelStyle(label.StyleKey);
		Point origin = ToAvaloniaPoint(camera.WorldToScreen(label.Position));
		if (isSelected)
		{
			double width = label.Text.Length * style.FontSize * 0.62 + 8.0;
			context.DrawRectangle(null, SelectedLabelPen, new Rect(origin.X - 4.0, origin.Y - 2.0, width, style.FontSize + 8.0));
		}
		DrawText(context, label.Text, origin, style.FontSize, style.Brush, style.FontWeight);
	}

	private static DistrictVisualStyle GetDistrictStyle(string styleKey)
	{
		return styleKey switch
		{
			"" or "district.default" => new DistrictVisualStyle(DistrictFillBrush, DistrictPen),
			"district.old" => new DistrictVisualStyle(new SolidColorBrush(Color.FromArgb(64, 91, 124, 156)), new Pen(new SolidColorBrush(Color.FromRgb(116, 151, 184)), 2.0)),
			"district.boundary" => new DistrictVisualStyle(new SolidColorBrush(Color.FromArgb(28, 166, 245, 213)), new Pen(new SolidColorBrush(Color.FromRgb(166, 245, 213)), 3.0)),
			"district.industrial" => new DistrictVisualStyle(new SolidColorBrush(Color.FromArgb(78, 107, 117, 126)), new Pen(new SolidColorBrush(Color.FromRgb(168, 177, 184)), 2.0)),
			"district.slums" => new DistrictVisualStyle(new SolidColorBrush(Color.FromArgb(72, 157, 119, 73)), new Pen(new SolidColorBrush(Color.FromRgb(207, 157, 88)), 2.0)),
			_ => new DistrictVisualStyle(DistrictFillBrush, DistrictPen)
		};
	}

	private static RoadVisualStyle GetRoadStyle(string styleKey)
	{
		return styleKey switch
		{
			"" or "road.primary" => new RoadVisualStyle(RoadPen),
			"road.secondary" => new RoadVisualStyle(new Pen(new SolidColorBrush(Color.FromRgb(198, 185, 154)), 3.0)),
			"road.alley" => new RoadVisualStyle(new Pen(new SolidColorBrush(Color.FromArgb(190, 168, 161, 142)), 1.5)),
			_ => new RoadVisualStyle(RoadPen)
		};
	}

	private static PoiVisualStyle GetPoiStyle(string styleKey)
	{
		return styleKey switch
		{
			"" or "poi.default" => new PoiVisualStyle(PoiFillBrush, PoiPen, 7.0),
			"poi.gate" => new PoiVisualStyle(new SolidColorBrush(Color.FromRgb(111, 176, 225)), new Pen(new SolidColorBrush(Color.FromRgb(34, 48, 61)), 2.0), 8.0),
			"poi.landmark" => new PoiVisualStyle(new SolidColorBrush(Color.FromRgb(136, 211, 154)), new Pen(new SolidColorBrush(Color.FromRgb(31, 58, 43)), 2.0), 9.0),
			"poi.danger" => new PoiVisualStyle(new SolidColorBrush(Color.FromRgb(226, 101, 91)), new Pen(new SolidColorBrush(Color.FromRgb(66, 34, 34)), 2.0), 8.0),
			_ => new PoiVisualStyle(PoiFillBrush, PoiPen, 7.0)
		};
	}

	private static LabelVisualStyle GetLabelStyle(string styleKey)
	{
		return styleKey switch
		{
			"label.city" => new LabelVisualStyle(new SolidColorBrush(Color.FromRgb(248, 236, 197)), 26.0, FontWeight.Bold),
			"" or "label.district" => new LabelVisualStyle(LabelTextBrush, 18.0, FontWeight.DemiBold),
			"label.map-title" => new LabelVisualStyle(new SolidColorBrush(Color.FromRgb(245, 247, 250)), 30.0, FontWeight.Bold),
			"label.note" => new LabelVisualStyle(new SolidColorBrush(Color.FromArgb(205, 209, 212, 218)), 14.0, FontWeight.Normal),
			_ => new LabelVisualStyle(LabelTextBrush, 18.0, FontWeight.DemiBold)
		};
	}

	private static Rect ToScreenRect(Camera2D camera, SizeD mapSizeMeters)
	{
		PointD pointD = camera.WorldToScreen(new PointD(0.0, 0.0));
		PointD pointD2 = camera.WorldToScreen(new PointD(mapSizeMeters.Width, mapSizeMeters.Height));
		double num = Math.Min(pointD.X, pointD2.X);
		double num2 = Math.Min(pointD.Y, pointD2.Y);
		double num3 = Math.Max(pointD.X, pointD2.X);
		double num4 = Math.Max(pointD.Y, pointD2.Y);
		return new Rect(num, num2, num3 - num, num4 - num2);
	}

	private static RectD GetBoundingBox(IReadOnlyList<PointD> points)
	{
		double num = points[0].X;
		double num2 = points[0].X;
		double num3 = points[0].Y;
		double num4 = points[0].Y;
		for (int i = 1; i < points.Count; i++)
		{
			PointD pointD = points[i];
			num = Math.Min(num, pointD.X);
			num2 = Math.Max(num2, pointD.X);
			num3 = Math.Min(num3, pointD.Y);
			num4 = Math.Max(num4, pointD.Y);
		}
		return new RectD(num, num3, num2 - num, num4 - num3);
	}

	private static void DrawTitle(DrawingContext context, string mapName)
	{
		DrawText(context, mapName, new Point(16.0, 14.0), 18.0, TextBrush, FontWeight.DemiBold);
	}

	private static void DrawScaleBar(DrawingContext context, Rect bounds, Camera2D camera)
	{
		double scaleBarWidthPixels = ViewportCameraHelper.GetScaleBarWidthPixels(camera, 100.0);
		Point p = new Point(16.0, Math.Max(32.0, bounds.Height - 32.0));
		Point p2 = new Point(p.X + scaleBarWidthPixels, p.Y);
		context.DrawLine(ScaleBarPen, p, p2);
		context.DrawLine(ScaleBarPen, new Point(p.X, p.Y - 5.0), new Point(p.X, p.Y + 5.0));
		context.DrawLine(ScaleBarPen, new Point(p2.X, p2.Y - 5.0), new Point(p2.X, p2.Y + 5.0));
		DrawText(context, "100 m", new Point(p.X, p.Y - 27.0), 12.0, MutedTextBrush);
	}

	private static void DrawText(DrawingContext context, string text, Point origin, double fontSize, IBrush brush, FontWeight? fontWeight = null)
	{
		FormattedText text2 = new FormattedText(typeface: new Typeface("Inter", FontStyle.Normal, fontWeight ?? FontWeight.Normal), textToFormat: text, culture: CultureInfo.InvariantCulture, flowDirection: FlowDirection.LeftToRight, emSize: fontSize, foreground: brush);
		context.DrawText(text2, origin);
	}

	private static double GetVisibleGridStepMeters(double baseGridStepMeters, double zoom)
	{
		double num = baseGridStepMeters;
		while (num * zoom < 8.0)
		{
			num *= 2.0;
		}
		return num;
	}

	private static bool IsMajorLine(double coordinate, double baseGridStepMeters)
	{
		double num = baseGridStepMeters * 10.0;
		return Math.Abs(coordinate / num - Math.Round(coordinate / num)) < 0.0001;
	}

	private static PointD ToPointD(Point point)
	{
		return new PointD(point.X, point.Y);
	}

	private static Point ToAvaloniaPoint(PointD point)
	{
		return new Point(point.X, point.Y);
	}

	private static double Distance(Point first, Point second)
	{
		double num = first.X - second.X;
		double num2 = first.Y - second.Y;
		return Math.Sqrt(num * num + num2 * num2);
	}

	private static double DistanceToSegment(Point point, Point segmentStart, Point segmentEnd)
	{
		double x = segmentEnd.X - segmentStart.X;
		double y = segmentEnd.Y - segmentStart.Y;
		double lengthSquared = x * x + y * y;
		if (lengthSquared == 0.0)
		{
			return Distance(point, segmentStart);
		}
		double value = ((point.X - segmentStart.X) * x + (point.Y - segmentStart.Y) * y) / lengthSquared;
		double clamped = Math.Clamp(value, 0.0, 1.0);
		return Distance(point, new Point(segmentStart.X + clamped * x, segmentStart.Y + clamped * y));
	}

	private static bool TrySetToolFromKey(MapViewportViewModel viewModel, Key key)
	{
		EditorToolType? editorToolType;
		switch (key)
		{
		case Key.S:
		case Key.V:
			editorToolType = EditorToolType.SelectMove;
			break;
		case Key.Space:
		case Key.H:
			editorToolType = EditorToolType.Pan;
			break;
		case Key.D:
			editorToolType = EditorToolType.District;
			break;
		case Key.R:
			editorToolType = EditorToolType.Road;
			break;
		case Key.P:
			editorToolType = EditorToolType.PointOfInterest;
			break;
		case Key.T:
			editorToolType = EditorToolType.Label;
			break;
		default:
			editorToolType = null;
			break;
		}
		EditorToolType? editorToolType2 = editorToolType;
		if (!editorToolType2.HasValue)
		{
			return false;
		}
		viewModel.SetActiveTool(editorToolType2.Value);
		return true;
	}
}
