using System;
using System.Collections.Generic;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Editor.Commands;

namespace LivingAtlas.Editor.Creation;

public sealed class RoadDrawingSession
{
	private readonly List<PointD> _points = new List<PointD>();

	public IReadOnlyList<PointD> Points => _points;

	public PointD? PreviewPoint { get; private set; }

	public bool IsDrawing => _points.Count > 0;

	public bool CanFinish => _points.Count >= 2;

	public void AddPoint(PointD point)
	{
		_points.Add(point);
		PreviewPoint = point;
	}

	public void UpdatePreviewPoint(PointD point)
	{
		if (IsDrawing)
		{
			PreviewPoint = point;
		}
	}

	public AddMapObjectCommand Finish(MapDocument map)
	{
		if (!CanFinish)
		{
			throw new InvalidOperationException("Road requires at least two points.");
		}
		AddMapObjectCommand result = MapObjectCreationService.CreateRoadLineCommand(map, _points);
		Cancel();
		return result;
	}

	public void Cancel()
	{
		_points.Clear();
		PreviewPoint = null;
	}
}
