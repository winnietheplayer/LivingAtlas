using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Editor.Hierarchy;

public static class ParentRoadProjectionService
{
	private const double Epsilon = 1E-09;

	public static IReadOnlyList<ParentRoadOverlay> GetProjectedRoadAreas(CampaignMapProject project, Guid activeMapId)
	{
		ArgumentNullException.ThrowIfNull(project, nameof(project));
		MapDocument? activeMap = project.FindMap(activeMapId);
		if (activeMap?.ParentMapId == null)
		{
			return Array.Empty<ParentRoadOverlay>();
		}

		MapDocument? parentMap = project.FindMap(activeMap.ParentMapId.Value);
		if (parentMap == null)
		{
			return Array.Empty<ParentRoadOverlay>();
		}

		DistrictShape? linkedDistrict = FindLinkedParentDistrict(parentMap, activeMap.Id);
		if (linkedDistrict == null)
		{
			return Array.Empty<ParentRoadOverlay>();
		}

		RectD districtBounds = GetBoundingBox(linkedDistrict.PolygonPoints);
		if (districtBounds.Size.Width <= 0.0 || districtBounds.Size.Height <= 0.0)
		{
			return Array.Empty<ParentRoadOverlay>();
		}

		List<ParentRoadOverlay> overlays = new List<ParentRoadOverlay>();
		foreach (MapLayer layer in parentMap.Layers)
		{
			if (!layer.IsVisible)
			{
				continue;
			}

			foreach (RoadArea roadArea in layer.Objects.OfType<RoadArea>())
			{
				if (!RoadAreaIntersectsDistrict(roadArea, linkedDistrict, districtBounds))
				{
					continue;
				}

				List<PointD> projectedPoints = roadArea.PolygonPoints
					.Select(point => ChildMapPreviewTransform.ParentToChild(point, districtBounds, activeMap.RealSizeMeters))
					.ToList();
				overlays.Add(new ParentRoadOverlay(
					roadArea.Id,
					layer.Id,
					roadArea.Name,
					projectedPoints,
					roadArea.StyleKey,
					roadArea.RoadKind,
					roadArea.FillTextureAssetId,
					roadArea.TextureTileSizeMeters));
			}
		}

		return overlays;
	}

	private static DistrictShape? FindLinkedParentDistrict(MapDocument parentMap, Guid childMapId)
	{
		return parentMap.Layers
			.SelectMany(layer => layer.Objects)
			.OfType<DistrictShape>()
			.FirstOrDefault(district => district.ChildMapId == childMapId);
	}

	private static bool RoadAreaIntersectsDistrict(RoadArea roadArea, DistrictShape district, RectD districtBounds)
	{
		RectD roadBounds = GetBoundingBox(roadArea.PolygonPoints);
		if (!BoundsIntersect(roadBounds, districtBounds))
		{
			return false;
		}

		if (roadArea.PolygonPoints.Any(point => IsPointInPolygon(point, district.PolygonPoints)))
		{
			return true;
		}

		if (district.PolygonPoints.Any(point => IsPointInPolygon(point, roadArea.PolygonPoints)))
		{
			return true;
		}

		return AnyEdgesIntersect(roadArea.PolygonPoints, district.PolygonPoints);
	}

	private static bool BoundsIntersect(RectD first, RectD second)
	{
		return first.Left <= second.Right
			&& first.Right >= second.Left
			&& first.Top <= second.Bottom
			&& first.Bottom >= second.Top;
	}

	private static bool AnyEdgesIntersect(IReadOnlyList<PointD> first, IReadOnlyList<PointD> second)
	{
		for (int i = 0; i < first.Count; i++)
		{
			PointD firstStart = first[i];
			PointD firstEnd = first[(i + 1) % first.Count];
			for (int j = 0; j < second.Count; j++)
			{
				if (SegmentsIntersect(firstStart, firstEnd, second[j], second[(j + 1) % second.Count]))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool SegmentsIntersect(PointD a, PointD b, PointD c, PointD d)
	{
		double o1 = Orientation(a, b, c);
		double o2 = Orientation(a, b, d);
		double o3 = Orientation(c, d, a);
		double o4 = Orientation(c, d, b);

		if (Math.Abs(o1) <= Epsilon && IsOnSegment(a, c, b))
		{
			return true;
		}
		if (Math.Abs(o2) <= Epsilon && IsOnSegment(a, d, b))
		{
			return true;
		}
		if (Math.Abs(o3) <= Epsilon && IsOnSegment(c, a, d))
		{
			return true;
		}
		if (Math.Abs(o4) <= Epsilon && IsOnSegment(c, b, d))
		{
			return true;
		}

		return (o1 > 0.0) != (o2 > 0.0) && (o3 > 0.0) != (o4 > 0.0);
	}

	private static double Orientation(PointD a, PointD b, PointD c)
	{
		return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
	}

	private static bool IsOnSegment(PointD start, PointD point, PointD end)
	{
		return point.X >= Math.Min(start.X, end.X) - Epsilon
			&& point.X <= Math.Max(start.X, end.X) + Epsilon
			&& point.Y >= Math.Min(start.Y, end.Y) - Epsilon
			&& point.Y <= Math.Max(start.Y, end.Y) + Epsilon;
	}

	private static bool IsPointInPolygon(PointD point, IReadOnlyList<PointD> polygon)
	{
		bool inside = false;
		int previousIndex = polygon.Count - 1;
		for (int i = 0; i < polygon.Count; i++)
		{
			PointD current = polygon[i];
			PointD previous = polygon[previousIndex];
			if ((current.Y > point.Y) != (previous.Y > point.Y))
			{
				double crossingX = (previous.X - current.X) * (point.Y - current.Y) / (previous.Y - current.Y) + current.X;
				if (point.X < crossingX)
				{
					inside = !inside;
				}
			}
			previousIndex = i;
		}
		return inside;
	}

	private static RectD GetBoundingBox(IReadOnlyList<PointD> points)
	{
		double left = points[0].X;
		double right = points[0].X;
		double top = points[0].Y;
		double bottom = points[0].Y;
		for (int i = 1; i < points.Count; i++)
		{
			PointD point = points[i];
			left = Math.Min(left, point.X);
			right = Math.Max(right, point.X);
			top = Math.Min(top, point.Y);
			bottom = Math.Max(bottom, point.Y);
		}

		return new RectD(left, top, right - left, bottom - top);
	}
}
