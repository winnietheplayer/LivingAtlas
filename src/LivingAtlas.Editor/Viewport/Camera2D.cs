using System;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Editor.Viewport;

public sealed class Camera2D
{
	public const double MinZoom = 0.05;

	public const double MaxZoom = 8.0;

	public double OffsetX { get; private set; }

	public double OffsetY { get; private set; }

	public double Zoom { get; private set; }

	public Camera2D(double offsetX = 0.0, double offsetY = 0.0, double zoom = 1.0)
	{
		OffsetX = offsetX;
		OffsetY = offsetY;
		Zoom = ClampZoom(zoom);
	}

	public void SetView(double offsetX, double offsetY, double zoom)
	{
		OffsetX = offsetX;
		OffsetY = offsetY;
		Zoom = ClampZoom(zoom);
	}

	public PointD ScreenToWorld(PointD screenPoint)
	{
		return new PointD((screenPoint.X - OffsetX) / Zoom, (screenPoint.Y - OffsetY) / Zoom);
	}

	public PointD WorldToScreen(PointD worldPoint)
	{
		return new PointD(worldPoint.X * Zoom + OffsetX, worldPoint.Y * Zoom + OffsetY);
	}

	public void PanBy(double screenDeltaX, double screenDeltaY)
	{
		OffsetX += screenDeltaX;
		OffsetY += screenDeltaY;
	}

	public void ZoomAt(PointD screenPoint, double zoomFactor)
	{
		if (zoomFactor <= 0.0)
		{
			throw new ArgumentOutOfRangeException("zoomFactor", zoomFactor, "Zoom factor must be positive.");
		}
		PointD worldPoint = ScreenToWorld(screenPoint);
		Zoom = ClampZoom(Zoom * zoomFactor);
		PointD pointD = WorldToScreen(worldPoint);
		OffsetX += screenPoint.X - pointD.X;
		OffsetY += screenPoint.Y - pointD.Y;
	}

	private static double ClampZoom(double zoom)
	{
		if (zoom <= 0.0)
		{
			throw new ArgumentOutOfRangeException("zoom", zoom, "Zoom must be positive.");
		}
		return Math.Clamp(zoom, 0.05, 8.0);
	}
}
