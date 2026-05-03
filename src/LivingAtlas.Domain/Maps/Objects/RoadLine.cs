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
}
