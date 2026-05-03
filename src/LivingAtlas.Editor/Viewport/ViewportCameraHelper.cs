using System;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Editor.Viewport;

public static class ViewportCameraHelper
{
	public static void FitToView(Camera2D camera, SizeD worldSize, SizeD viewportSize, double paddingPixels = 48.0)
	{
		ArgumentNullException.ThrowIfNull(camera, "camera");
		if (worldSize.Width <= 0.0 || worldSize.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("worldSize", worldSize, "World size must be positive.");
		}
		if (viewportSize.Width <= 0.0 || viewportSize.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("viewportSize", viewportSize, "Viewport size must be positive.");
		}
		if (paddingPixels < 0.0)
		{
			throw new ArgumentOutOfRangeException("paddingPixels", paddingPixels, "Padding must not be negative.");
		}
		double num = Math.Max(1.0, viewportSize.Width - paddingPixels * 2.0);
		double num2 = Math.Max(1.0, viewportSize.Height - paddingPixels * 2.0);
		double num3 = Math.Min(num / worldSize.Width, num2 / worldSize.Height);
		double offsetX = (viewportSize.Width - worldSize.Width * num3) / 2.0;
		double offsetY = (viewportSize.Height - worldSize.Height * num3) / 2.0;
		camera.SetView(offsetX, offsetY, num3);
	}

	public static RectD GetVisibleWorldBounds(Camera2D camera, SizeD viewportSize)
	{
		ArgumentNullException.ThrowIfNull(camera, "camera");
		if (viewportSize.Width <= 0.0 || viewportSize.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("viewportSize", viewportSize, "Viewport size must be positive.");
		}
		PointD pointD = camera.ScreenToWorld(new PointD(0.0, 0.0));
		PointD pointD2 = camera.ScreenToWorld(new PointD(viewportSize.Width, viewportSize.Height));
		double num = Math.Min(pointD.X, pointD2.X);
		double num2 = Math.Min(pointD.Y, pointD2.Y);
		double num3 = Math.Max(pointD.X, pointD2.X);
		double num4 = Math.Max(pointD.Y, pointD2.Y);
		return new RectD(num, num2, num3 - num, num4 - num2);
	}

	public static double GetScaleBarWidthPixels(Camera2D camera, double lengthMeters)
	{
		ArgumentNullException.ThrowIfNull(camera, "camera");
		if (lengthMeters <= 0.0)
		{
			throw new ArgumentOutOfRangeException("lengthMeters", lengthMeters, "Scale bar length must be positive.");
		}
		return lengthMeters * camera.Zoom;
	}
}
