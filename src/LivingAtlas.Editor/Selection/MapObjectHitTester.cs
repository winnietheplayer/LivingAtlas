using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Selection;

public static class MapObjectHitTester
{
	private readonly record struct HitCandidate(MapObject MapObject, double Distance, bool IsAreaHit, int DrawOrder);

	public static MapObject? HitTest(MapDocument map, PointD worldPoint, double worldTolerance)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (worldTolerance < 0.0)
		{
			throw new ArgumentOutOfRangeException("worldTolerance", worldTolerance, "Tolerance must not be negative.");
		}
		List<HitCandidate> list = new List<HitCandidate>();
		int num = 0;
		foreach (MapLayer layer in map.Layers)
		{
			if (!layer.IsVisible || layer.IsLocked)
			{
				continue;
			}
			foreach (MapObject @object in layer.Objects)
			{
				num++;
				HitCandidate? hitCandidate = HitTestObject(@object, worldPoint, worldTolerance, num);
				if (hitCandidate.HasValue)
				{
					list.Add(hitCandidate.Value);
				}
			}
		}
		return (from hit in list
			orderby hit.IsAreaHit, hit.Distance, hit.DrawOrder descending
			select hit.MapObject).FirstOrDefault();
	}

	private static HitCandidate? HitTestObject(MapObject mapObject, PointD worldPoint, double worldTolerance, int drawOrder)
	{
		return mapObject switch
		{
			PointOfInterest poi => HitTestPointOfInterest(poi, worldPoint, worldTolerance, drawOrder),
			MapLabel label => HitTestMapLabel(label, worldPoint, worldTolerance, drawOrder),
			RoadLine road => HitTestRoadLine(road, worldPoint, worldTolerance, drawOrder),
			RoadArea roadArea => HitTestRoadArea(roadArea, worldPoint, drawOrder),
			DistrictShape district => HitTestDistrictShape(district, worldPoint, drawOrder),
			_ => null
		};
	}

	private static HitCandidate? HitTestPointOfInterest(PointOfInterest poi, PointD worldPoint, double worldTolerance, int drawOrder)
	{
		double num = Distance(worldPoint, poi.Position);
		return (num <= worldTolerance) ? new HitCandidate?(new HitCandidate(poi, num, IsAreaHit: false, drawOrder)) : ((HitCandidate?)null);
	}

	private static HitCandidate? HitTestMapLabel(MapLabel label, PointD worldPoint, double worldTolerance, int drawOrder)
	{
		double num = Math.Max(worldTolerance * 2.0, (double)label.Text.Length * worldTolerance);
		double num2 = Math.Max(worldTolerance * 2.0, worldTolerance * 2.5);
		double x = label.Position.X;
		double num3 = label.Position.Y - worldTolerance;
		double num4 = x + num;
		double num5 = num3 + num2;
		if (worldPoint.X >= x && worldPoint.X <= num4 && worldPoint.Y >= num3 && worldPoint.Y <= num5)
		{
			return new HitCandidate(label, 0.0, IsAreaHit: false, drawOrder);
		}
		double num6 = Distance(worldPoint, label.Position);
		return (num6 <= worldTolerance * 2.0) ? new HitCandidate?(new HitCandidate(label, num6, IsAreaHit: false, drawOrder)) : ((HitCandidate?)null);
	}

	private static HitCandidate? HitTestRoadLine(RoadLine road, PointD worldPoint, double worldTolerance, int drawOrder)
	{
		double num = double.PositiveInfinity;
		for (int i = 1; i < road.Points.Count; i++)
		{
			num = Math.Min(num, DistanceToSegment(worldPoint, road.Points[i - 1], road.Points[i]));
		}
		return (num <= worldTolerance) ? new HitCandidate?(new HitCandidate(road, num, IsAreaHit: false, drawOrder)) : ((HitCandidate?)null);
	}

	private static HitCandidate? HitTestDistrictShape(DistrictShape district, PointD worldPoint, int drawOrder)
	{
		return IsPointInPolygon(worldPoint, district.PolygonPoints) ? new HitCandidate?(new HitCandidate(district, 0.0, IsAreaHit: true, drawOrder)) : ((HitCandidate?)null);
	}

	private static HitCandidate? HitTestRoadArea(RoadArea roadArea, PointD worldPoint, int drawOrder)
	{
		return IsPointInPolygon(worldPoint, roadArea.PolygonPoints) ? new HitCandidate?(new HitCandidate(roadArea, 0.0, IsAreaHit: true, drawOrder)) : ((HitCandidate?)null);
	}

	private static bool IsPointInPolygon(PointD point, IReadOnlyList<PointD> polygon)
	{
		bool flag = false;
		int index = polygon.Count - 1;
		for (int i = 0; i < polygon.Count; i++)
		{
			PointD pointD = polygon[i];
			PointD pointD2 = polygon[index];
			if (pointD.Y > point.Y != pointD2.Y > point.Y)
			{
				double num = (pointD2.X - pointD.X) * (point.Y - pointD.Y) / (pointD2.Y - pointD.Y) + pointD.X;
				if (point.X < num)
				{
					flag = !flag;
				}
			}
			index = i;
		}
		return flag;
	}

	private static double Distance(PointD first, PointD second)
	{
		double num = first.X - second.X;
		double num2 = first.Y - second.Y;
		return Math.Sqrt(num * num + num2 * num2);
	}

	private static double DistanceToSegment(PointD point, PointD segmentStart, PointD segmentEnd)
	{
		double num = segmentEnd.X - segmentStart.X;
		double num2 = segmentEnd.Y - segmentStart.Y;
		double num3 = num * num + num2 * num2;
		if (num3 == 0.0)
		{
			return Distance(point, segmentStart);
		}
		double value = ((point.X - segmentStart.X) * num + (point.Y - segmentStart.Y) * num2) / num3;
		double num4 = Math.Clamp(value, 0.0, 1.0);
		return Distance(second: new PointD(segmentStart.X + num4 * num, segmentStart.Y + num4 * num2), first: point);
	}
}
