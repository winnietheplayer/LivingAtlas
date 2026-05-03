using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Domain.Maps.Objects;

public sealed class DistrictShape : MapObject
{
	private readonly List<PointD> _polygonPoints;

	public IReadOnlyList<PointD> PolygonPoints => _polygonPoints;

	public Guid? ChildMapId { get; private set; }

	public DistrictShape(Guid id, string name, Guid layerId, IEnumerable<PointD> polygonPoints, IEnumerable<string>? tags = null, string? styleKey = null, Guid? childMapId = null)
		: base(id, name, MapObjectType.DistrictShape, layerId, tags, styleKey)
	{
		ArgumentNullException.ThrowIfNull(polygonPoints, "polygonPoints");
		if (childMapId == Guid.Empty)
		{
			throw new ArgumentException("Child map id cannot be empty.", "childMapId");
		}
		List<PointD> list = polygonPoints.ToList();
		if (list.Count < 3)
		{
			throw new ArgumentException("District polygon must contain at least three points.", "polygonPoints");
		}
		_polygonPoints = list;
		ChildMapId = childMapId;
	}

	public void SetChildMapId(Guid? childMapId)
	{
		if (childMapId == Guid.Empty)
		{
			throw new ArgumentException("Child map id cannot be empty.", "childMapId");
		}
		ChildMapId = childMapId;
	}

	public void MoveBy(PointD delta)
	{
		for (int i = 0; i < _polygonPoints.Count; i++)
		{
			PointD pointD = _polygonPoints[i];
			_polygonPoints[i] = new PointD(pointD.X + delta.X, pointD.Y + delta.Y);
		}
	}
}
