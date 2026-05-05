using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Domain.Maps.Objects;

public sealed class RoadLine : MapObject
{
	private readonly List<PointD> _points;

	public IReadOnlyList<PointD> Points => _points;

	public RoadLine(Guid id, string name, Guid layerId, IEnumerable<PointD> points, IEnumerable<string>? tags = null, string? styleKey = null)
		: base(id, name, MapObjectType.RoadLine, layerId, tags, styleKey)
	{
		ArgumentNullException.ThrowIfNull(points, "points");
		List<PointD> list = points.ToList();
		if (list.Count < 2)
		{
			throw new ArgumentException("Road line must contain at least two points.", "points");
		}
		_points = list;
	}

	public void MoveBy(PointD delta)
	{
		for (int i = 0; i < _points.Count; i++)
		{
			PointD pointD = _points[i];
			_points[i] = new PointD(pointD.X + delta.X, pointD.Y + delta.Y);
		}
	}

	public void SetPoint(int index, PointD point)
	{
		ValidatePointIndex(index);
		_points[index] = point;
	}

	public void InsertPoint(int index, PointD point)
	{
		if (index < 0 || index > _points.Count)
		{
			throw new ArgumentOutOfRangeException("index", index, "Point index is outside the road point insertion bounds.");
		}
		_points.Insert(index, point);
	}

	public void RemovePoint(int index)
	{
		ValidatePointIndex(index);
		if (_points.Count <= 2)
		{
			throw new InvalidOperationException("Road line must contain at least two points.");
		}
		_points.RemoveAt(index);
	}

	private void ValidatePointIndex(int index)
	{
		if (index < 0 || index >= _points.Count)
		{
			throw new ArgumentOutOfRangeException("index", index, "Point index is outside the road point bounds.");
		}
	}
}
