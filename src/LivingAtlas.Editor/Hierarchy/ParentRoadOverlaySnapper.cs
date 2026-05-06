using System;
using System.Collections.Generic;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Editor.Viewport;

namespace LivingAtlas.Editor.Hierarchy;

public static class ParentRoadOverlaySnapper
{
	public static PointD SnapToNearestOverlayVertex(
		PointD rawWorldPoint,
		PointD gridSnappedWorldPoint,
		IReadOnlyList<ParentRoadOverlay> overlays,
		Camera2D camera,
		double tolerancePixels)
	{
		ArgumentNullException.ThrowIfNull(overlays, nameof(overlays));
		ArgumentNullException.ThrowIfNull(camera, nameof(camera));
		if (tolerancePixels < 0.0)
		{
			throw new ArgumentOutOfRangeException(nameof(tolerancePixels), tolerancePixels, "Tolerance must not be negative.");
		}

		PointD rawScreenPoint = camera.WorldToScreen(rawWorldPoint);
		PointD bestPoint = gridSnappedWorldPoint;
		double bestDistance = Distance(rawScreenPoint, camera.WorldToScreen(gridSnappedWorldPoint));
		foreach (ParentRoadOverlay overlay in overlays)
		{
			foreach (PointD candidate in overlay.ProjectedPolygonPoints)
			{
				double distance = Distance(rawScreenPoint, camera.WorldToScreen(candidate));
				if (distance <= tolerancePixels && distance < bestDistance)
				{
					bestDistance = distance;
					bestPoint = candidate;
				}
			}
		}

		return bestPoint;
	}

	private static double Distance(PointD first, PointD second)
	{
		double dx = first.X - second.X;
		double dy = first.Y - second.Y;
		return Math.Sqrt(dx * dx + dy * dy);
	}
}
