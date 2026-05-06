using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Domain.Maps.Objects;

public sealed class RoadArea : MapObject
{
	public const string DefaultRoadKind = "secondary";

	public const double DefaultTextureTileSizeMeters = 10.0;

	private readonly List<PointD> _polygonPoints;

	public IReadOnlyList<PointD> PolygonPoints => _polygonPoints;

	public string RoadKind { get; private set; }

	public string? FillTextureAssetId { get; private set; }

	public double TextureTileSizeMeters { get; private set; }

	public RoadArea(Guid id, string name, Guid layerId, IEnumerable<PointD> polygonPoints, IEnumerable<string>? tags = null, string? styleKey = null, string? description = null, string? roadKind = null, string? fillTextureAssetId = null, double textureTileSizeMeters = DefaultTextureTileSizeMeters)
		: base(id, name, MapObjectType.RoadArea, layerId, tags, styleKey, description)
	{
		ArgumentNullException.ThrowIfNull(polygonPoints, nameof(polygonPoints));
		List<PointD> list = polygonPoints.ToList();
		if (list.Count < 3)
		{
			throw new ArgumentException("Road area polygon must contain at least three points.", nameof(polygonPoints));
		}

		_polygonPoints = list;
		RoadKind = NormalizeRoadKind(roadKind);
		SetTextureFill(fillTextureAssetId, textureTileSizeMeters);
	}

	public void MoveBy(PointD delta)
	{
		for (int i = 0; i < _polygonPoints.Count; i++)
		{
			PointD point = _polygonPoints[i];
			_polygonPoints[i] = new PointD(point.X + delta.X, point.Y + delta.Y);
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
			throw new ArgumentOutOfRangeException(nameof(index), index, "Point index is outside the road area polygon point insertion bounds.");
		}

		_polygonPoints.Insert(index, point);
	}

	public void RemovePoint(int index)
	{
		ValidatePointIndex(index);
		if (_polygonPoints.Count <= 3)
		{
			throw new InvalidOperationException("Road area polygon must contain at least three points.");
		}

		_polygonPoints.RemoveAt(index);
	}

	public void SetRoadKind(string? roadKind)
	{
		RoadKind = NormalizeRoadKind(roadKind);
	}

	public void SetTextureFill(string? assetId, double tileSizeMeters)
	{
		if (tileSizeMeters <= 0.0)
		{
			throw new ArgumentOutOfRangeException(nameof(tileSizeMeters), tileSizeMeters, "Texture tile size must be positive.");
		}

		string? normalizedAssetId = NormalizeTextureAssetId(assetId);
		if (normalizedAssetId == null)
		{
			ClearTextureFill();
			return;
		}

		FillTextureAssetId = normalizedAssetId;
		TextureTileSizeMeters = tileSizeMeters;
	}

	public void ClearTextureFill()
	{
		FillTextureAssetId = null;
		TextureTileSizeMeters = DefaultTextureTileSizeMeters;
	}

	private void ValidatePointIndex(int index)
	{
		if (index < 0 || index >= _polygonPoints.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index), index, "Point index is outside the road area polygon point bounds.");
		}
	}

	private static string NormalizeRoadKind(string? roadKind)
	{
		return string.IsNullOrWhiteSpace(roadKind) ? DefaultRoadKind : roadKind.Trim();
	}

	private static string? NormalizeTextureAssetId(string? assetId)
	{
		return string.IsNullOrWhiteSpace(assetId) ? null : assetId.Trim();
	}
}
