using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Domain.Maps.Objects;

public sealed class DistrictShape : MapObject
{
	public const string DefaultDistrictKind = "generic";

	private readonly List<PointD> _polygonPoints;

	public IReadOnlyList<PointD> PolygonPoints => _polygonPoints;

	public Guid? ChildMapId { get; private set; }

	public string DistrictKind { get; private set; }

	public DistrictShape(Guid id, string name, Guid layerId, IEnumerable<PointD> polygonPoints, IEnumerable<string>? tags = null, string? styleKey = null, Guid? childMapId = null, string? description = null, string? districtKind = null)
		: base(id, name, MapObjectType.DistrictShape, layerId, tags, styleKey, description)
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
		DistrictKind = NormalizeDistrictKind(districtKind);
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

	public void SetPoint(int index, PointD point)
	{
		ValidatePointIndex(index);
		_polygonPoints[index] = point;
	}

	public void InsertPoint(int index, PointD point)
	{
		if (index < 0 || index > _polygonPoints.Count)
		{
			throw new ArgumentOutOfRangeException("index", index, "Point index is outside the district polygon point insertion bounds.");
		}
		_polygonPoints.Insert(index, point);
	}

	public void RemovePoint(int index)
	{
		ValidatePointIndex(index);
		if (_polygonPoints.Count <= 3)
		{
			throw new InvalidOperationException("District polygon must contain at least three points.");
		}
		_polygonPoints.RemoveAt(index);
	}

	public void SetDistrictKind(string? districtKind)
	{
		DistrictKind = NormalizeDistrictKind(districtKind);
	}

	private void ValidatePointIndex(int index)
	{
		if (index < 0 || index >= _polygonPoints.Count)
		{
			throw new ArgumentOutOfRangeException("index", index, "Point index is outside the district polygon point bounds.");
		}
	}

	private static string NormalizeDistrictKind(string? districtKind)
	{
		return string.IsNullOrWhiteSpace(districtKind) ? DefaultDistrictKind : districtKind.Trim();
	}
}
