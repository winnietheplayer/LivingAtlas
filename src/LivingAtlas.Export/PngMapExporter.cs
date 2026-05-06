using LivingAtlas.Assets;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Hierarchy;
using LivingAtlas.Rendering;
using SkiaSharp;

namespace LivingAtlas.Export;

public sealed class PngMapExporter
{
	private static readonly SKColor BackgroundColor = new SKColor(31, 34, 39);

	private static readonly SKColor MapFillColor = new SKColor(38, 43, 50);

	private static readonly SKColor TextColor = new SKColor(230, 234, 240);

	private static readonly SKColor MinorGridColor = new SKColor(72, 78, 88, 90);

	private static readonly SKColor MajorGridColor = new SKColor(95, 104, 118, 140);

	private static readonly SKColor MapBoundsColor = new SKColor(218, 225, 232);

	private static readonly SKColor ChildPreviewDistrictFillColor = new SKColor(143, 170, 208, 36);

	private static readonly SKColor ChildPreviewDistrictStrokeColor = new SKColor(143, 170, 208, 190);

	private static readonly SKColor ChildPreviewRoadColor = new SKColor(194, 185, 160, 205);

	private static readonly SKColor ChildPreviewPoiFillColor = new SKColor(185, 210, 226, 210);

	private static readonly SKColor ChildPreviewPoiStrokeColor = new SKColor(44, 50, 58, 190);

	private static readonly SKColor ChildPreviewTextColor = new SKColor(199, 211, 222, 210);

	public Task ExportAsync(CampaignMapProject project, MapDocument map, PngExportOptions options, CancellationToken cancellationToken = default)
	{
		return Task.Run(() => Export(project, map, options), cancellationToken);
	}

	public Task ExportAsync(CampaignMapProject project, MapDocument map, PngExportOptions options, TextureAssetCatalog? textureAssetCatalog, CancellationToken cancellationToken = default)
	{
		return Task.Run(() => Export(project, map, options, textureAssetCatalog), cancellationToken);
	}

	public void Export(CampaignMapProject project, MapDocument map, PngExportOptions options)
	{
		Export(project, map, options, null);
	}

	public void Export(CampaignMapProject project, MapDocument map, PngExportOptions options, TextureAssetCatalog? textureAssetCatalog, SkiaTextureImageCache? textureImageCache = null)
	{
		ArgumentNullException.ThrowIfNull(project);
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(options);

		PngExportImageSize imageSize = ValidateAndGetImageSize(map, options);
		string outputPath = Path.GetFullPath(options.OutputPath);
		string? outputDirectory = Path.GetDirectoryName(outputPath);
		if (!string.IsNullOrWhiteSpace(outputDirectory))
		{
			Directory.CreateDirectory(outputDirectory);
		}

		var imageInfo = new SKImageInfo(imageSize.Width, imageSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		using SKSurface surface = SKSurface.Create(imageInfo) ?? throw new InvalidOperationException("PNG export failed: could not create render surface.");
		SKCanvas canvas = surface.Canvas;
		bool ownsTextureCache = textureImageCache == null;
		textureImageCache ??= new SkiaTextureImageCache();
		try
		{
			Render(canvas, project, map, options, imageSize, textureAssetCatalog ?? TextureAssetCatalog.Empty, textureImageCache);
		}
		finally
		{
			if (ownsTextureCache)
			{
				textureImageCache.Dispose();
			}
		}
		canvas.Flush();

		using SKImage image = surface.Snapshot();
		using SKData data = image.Encode(SKEncodedImageFormat.Png, 100) ?? throw new InvalidOperationException("PNG export failed: could not encode image.");
		using FileStream stream = File.Create(outputPath);
		data.SaveTo(stream);
	}

	public static PngExportImageSize ValidateAndGetImageSize(MapDocument map, PngExportOptions options)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(options);

		if (string.IsNullOrWhiteSpace(options.OutputPath))
		{
			throw new ArgumentException("Output path cannot be empty.", nameof(options));
		}
		if (options.ResolutionScale is not (1 or 2 or 4))
		{
			throw new ArgumentOutOfRangeException(nameof(options), options.ResolutionScale, "Resolution scale must be 1, 2, or 4.");
		}
		if (map.RealSizeMeters.Width <= 0.0 || map.RealSizeMeters.Height <= 0.0)
		{
			throw new ArgumentException("Map size must be positive.", nameof(map));
		}

		int width = checked((int)Math.Ceiling(map.RealSizeMeters.Width * options.ResolutionScale));
		int height = checked((int)Math.Ceiling(map.RealSizeMeters.Height * options.ResolutionScale));
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentException("Export image dimensions must be positive.", nameof(map));
		}
		if (width > PngExportOptions.MaxDimensionPixels || height > PngExportOptions.MaxDimensionPixels)
		{
			throw new InvalidOperationException($"Export image is too large: {width} x {height}. Maximum is {PngExportOptions.MaxDimensionPixels} x {PngExportOptions.MaxDimensionPixels}.");
		}

		return new PngExportImageSize(width, height);
	}

	private static void Render(
		SKCanvas canvas,
		CampaignMapProject project,
		MapDocument map,
		PngExportOptions options,
		PngExportImageSize imageSize,
		TextureAssetCatalog textureAssetCatalog,
		SkiaTextureImageCache textureImageCache)
	{
		float scale = options.ResolutionScale;
		canvas.Clear(options.TransparentBackground ? SKColors.Transparent : BackgroundColor);
		if (!options.TransparentBackground)
		{
			using SKPaint mapFill = FillPaint(MapFillColor);
			canvas.DrawRect(0.0f, 0.0f, imageSize.Width, imageSize.Height, mapFill);
		}

		if (options.IncludeGrid && map.GridSettings.ShowGrid)
		{
			DrawGrid(canvas, map, scale, imageSize);
		}

		if (options.IncludeChildMapPreviews)
		{
			DrawParentRoadOverlays(canvas, project, map, scale, textureAssetCatalog, textureImageCache);
		}

		DrawMapObjects(canvas, project, map, options, scale, textureAssetCatalog, textureImageCache);
		DrawMapBounds(canvas, imageSize, scale);
	}

	private static void DrawGrid(SKCanvas canvas, MapDocument map, float scale, PngExportImageSize imageSize)
	{
		double stepMeters = map.GridSettings.CellSizeMeters;
		if (stepMeters <= 0.0)
		{
			return;
		}

		using SKPaint minorGridPaint = StrokePaint(MinorGridColor, 1.0f);
		using SKPaint majorGridPaint = StrokePaint(MajorGridColor, 1.0f);
		for (double x = 0.0; x <= map.RealSizeMeters.Width; x += stepMeters)
		{
			float screenX = (float)(x * scale);
			canvas.DrawLine(screenX, 0.0f, screenX, imageSize.Height, IsMajorGridLine(x, stepMeters) ? majorGridPaint : minorGridPaint);
		}
		for (double y = 0.0; y <= map.RealSizeMeters.Height; y += stepMeters)
		{
			float screenY = (float)(y * scale);
			canvas.DrawLine(0.0f, screenY, imageSize.Width, screenY, IsMajorGridLine(y, stepMeters) ? majorGridPaint : minorGridPaint);
		}
	}

	private static void DrawMapBounds(SKCanvas canvas, PngExportImageSize imageSize, float scale)
	{
		using SKPaint boundsPaint = StrokePaint(MapBoundsColor, 2.0f * scale);
		float inset = boundsPaint.StrokeWidth / 2.0f;
		canvas.DrawRect(inset, inset, imageSize.Width - boundsPaint.StrokeWidth, imageSize.Height - boundsPaint.StrokeWidth, boundsPaint);
	}

	private static void DrawMapObjects(SKCanvas canvas, CampaignMapProject project, MapDocument map, PngExportOptions options, float scale, TextureAssetCatalog textureAssetCatalog, SkiaTextureImageCache textureImageCache)
	{
		foreach (MapLayer layer in map.Layers)
		{
			if (!layer.IsVisible)
			{
				continue;
			}

			foreach (MapObject mapObject in layer.Objects)
			{
				switch (mapObject)
				{
					case DistrictShape district:
						DrawDistrict(canvas, district, scale, textureAssetCatalog, textureImageCache);
						if (options.IncludeChildMapPreviews)
						{
							DrawChildMapPreview(canvas, project, map, district, options, scale);
						}
						break;
					case RoadLine road:
						DrawRoad(canvas, road, scale);
						break;
					case RoadArea roadArea:
						DrawRoadArea(canvas, roadArea, scale, textureAssetCatalog, textureImageCache);
						break;
					case PointOfInterest poi when options.IncludePointsOfInterest:
						DrawPointOfInterest(canvas, poi, scale);
						break;
					case MapLabel label when options.IncludeLabels:
						DrawLabel(canvas, label, scale);
						break;
				}
			}
		}
	}

	private static void DrawDistrict(SKCanvas canvas, DistrictShape district, float scale, TextureAssetCatalog textureAssetCatalog, SkiaTextureImageCache textureImageCache)
	{
		if (district.PolygonPoints.Count == 0)
		{
			return;
		}

		DistrictRenderStyle style = MapObjectStyleResolver.GetDistrictStyle(district.StyleKey);
		using SKPath path = BuildPolygonPath(district.PolygonPoints, scale);
		using SKPaint fill = FillPaint(ToSkColor(style.Fill));
		using SKPaint stroke = StrokePaint(ToSkColor(style.Stroke), (float)(style.StrokeWidth * scale));
		using SKPaint? textureFill = CreateTextureFillPaint(district.FillTextureAssetId, district.TextureTileSizeMeters, scale, textureAssetCatalog, textureImageCache);
		canvas.DrawPath(path, textureFill ?? fill);
		canvas.DrawPath(path, stroke);
	}

	private static SKPaint? CreateTextureFillPaint(string? fillTextureAssetId, double textureTileSizeMeters, float scale, TextureAssetCatalog textureAssetCatalog, SkiaTextureImageCache textureImageCache, float opacity = 1.0f)
	{
		if (fillTextureAssetId == null || textureTileSizeMeters <= 0.0)
		{
			return null;
		}

		SKBitmap? bitmap = textureImageCache.Get(textureAssetCatalog, fillTextureAssetId);
		if (bitmap == null)
		{
			return null;
		}

		float tilePixels = Math.Max(1.0f, (float)(textureTileSizeMeters * scale));
		SKMatrix shaderMatrix = SKMatrix.CreateScale(bitmap.Width / tilePixels, bitmap.Height / tilePixels);
		SKShader shader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, shaderMatrix);
		return new SKPaint
		{
			Color = new SKColor(255, 255, 255, ToAlphaByte(opacity)),
			IsAntialias = true,
			Style = SKPaintStyle.Fill,
			Shader = shader
		};
	}

	private static void DrawRoad(SKCanvas canvas, RoadLine road, float scale)
	{
		RoadRenderStyle style = MapObjectStyleResolver.GetRoadStyle(road.StyleKey);
		using SKPaint stroke = StrokePaint(ToSkColor(style.Stroke), (float)(style.StrokeWidth * scale));
		stroke.StrokeCap = SKStrokeCap.Round;
		stroke.StrokeJoin = SKStrokeJoin.Round;
		for (int i = 1; i < road.Points.Count; i++)
		{
			SKPoint start = ToPixel(road.Points[i - 1], scale);
			SKPoint end = ToPixel(road.Points[i], scale);
			canvas.DrawLine(start, end, stroke);
		}
	}

	private static void DrawRoadArea(SKCanvas canvas, RoadArea roadArea, float scale, TextureAssetCatalog textureAssetCatalog, SkiaTextureImageCache textureImageCache)
	{
		if (roadArea.PolygonPoints.Count == 0)
		{
			return;
		}

		RoadAreaRenderStyle style = MapObjectStyleResolver.GetRoadAreaStyle(roadArea.StyleKey);
		using SKPath path = BuildPolygonPath(roadArea.PolygonPoints, scale);
		using SKPaint fill = FillPaint(ToSkColor(style.Fill));
		using SKPaint stroke = StrokePaint(ToSkColor(style.Stroke), (float)(style.StrokeWidth * scale));
		using SKPaint? textureFill = CreateTextureFillPaint(roadArea.FillTextureAssetId, roadArea.TextureTileSizeMeters, scale, textureAssetCatalog, textureImageCache);
		canvas.DrawPath(path, textureFill ?? fill);
		canvas.DrawPath(path, stroke);
	}

	private static void DrawParentRoadOverlays(SKCanvas canvas, CampaignMapProject project, MapDocument map, float scale, TextureAssetCatalog textureAssetCatalog, SkiaTextureImageCache textureImageCache)
	{
		IReadOnlyList<ParentRoadOverlay> overlays = ParentRoadProjectionService.GetProjectedRoadAreas(project, map.Id);
		foreach (ParentRoadOverlay overlay in overlays)
		{
			if (overlay.ProjectedPolygonPoints.Count == 0)
			{
				continue;
			}

			RoadAreaRenderStyle style = MapObjectStyleResolver.GetRoadAreaStyle(overlay.StyleKey);
			using SKPath path = BuildPolygonPath(overlay.ProjectedPolygonPoints, scale);
			using SKPaint fill = FillPaint(ToSkColor(style.Fill, 0.65f));
			using SKPaint stroke = StrokePaint(ToSkColor(style.Stroke, 0.65f), (float)(style.StrokeWidth * scale));
			using SKPaint? textureFill = CreateTextureFillPaint(overlay.FillTextureAssetId, overlay.TextureTileSizeMeters, scale, textureAssetCatalog, textureImageCache, 0.65f);
			canvas.DrawPath(path, textureFill ?? fill);
			canvas.DrawPath(path, stroke);
		}
	}

	private static void DrawPointOfInterest(SKCanvas canvas, PointOfInterest poi, float scale)
	{
		PoiRenderStyle style = MapObjectStyleResolver.GetPoiStyle(poi.StyleKey);
		SKPoint center = ToPixel(poi.Position, scale);
		using SKPaint fill = FillPaint(ToSkColor(style.Fill));
		using SKPaint stroke = StrokePaint(ToSkColor(style.Stroke), (float)(style.StrokeWidth * scale));
		float radius = (float)(style.Radius * scale);
		canvas.DrawCircle(center, radius, fill);
		canvas.DrawCircle(center, radius, stroke);
		DrawText(canvas, poi.Name, center.X + (12.0f * scale), center.Y + (4.0f * scale), 12.0f * scale, TextColor, SKFontStyleWeight.Normal);
	}

	private static void DrawLabel(SKCanvas canvas, MapLabel label, float scale)
	{
		LabelRenderStyle style = MapObjectStyleResolver.GetLabelStyle(label.StyleKey);
		SKPoint origin = ToPixel(label.Position, scale);
		DrawText(canvas, label.Text, origin.X, origin.Y + ((float)style.FontSize * scale), (float)style.FontSize * scale, ToSkColor(style.Color), ToSkFontWeight(style.FontWeight));
	}

	private static void DrawChildMapPreview(SKCanvas canvas, CampaignMapProject project, MapDocument parentMap, DistrictShape parentDistrict, PngExportOptions options, float scale)
	{
		if (!parentDistrict.ChildMapId.HasValue)
		{
			return;
		}

		MapDocument? childMap = project.FindMap(parentDistrict.ChildMapId.Value);
		if (childMap == null || childMap.Id == parentMap.Id || childMap.RealSizeMeters.Width <= 0.0 || childMap.RealSizeMeters.Height <= 0.0)
		{
			return;
		}

		RectD parentBounds = GetBoundingBox(parentDistrict.PolygonPoints);
		if (parentBounds.Size.Width <= 0.0 || parentBounds.Size.Height <= 0.0)
		{
			return;
		}

		foreach (MapLayer layer in childMap.Layers)
		{
			if (!layer.IsVisible)
			{
				continue;
			}

			foreach (MapObject mapObject in layer.Objects)
			{
				switch (mapObject)
				{
					case DistrictShape district:
						DrawChildPreviewDistrict(canvas, parentBounds, childMap.RealSizeMeters, district, scale);
						break;
					case RoadLine road:
						DrawChildPreviewRoad(canvas, parentBounds, childMap.RealSizeMeters, road, scale);
						break;
					case PointOfInterest poi when options.IncludePointsOfInterest:
						DrawChildPreviewPointOfInterest(canvas, parentBounds, childMap.RealSizeMeters, poi, scale);
						break;
					case MapLabel label when options.IncludeLabels:
						DrawChildPreviewLabel(canvas, parentBounds, childMap.RealSizeMeters, label, scale);
						break;
				}
			}
		}
	}

	private static void DrawChildPreviewDistrict(SKCanvas canvas, RectD parentBounds, SizeD childSize, DistrictShape district, float scale)
	{
		if (district.PolygonPoints.Count == 0)
		{
			return;
		}

		using SKPath path = BuildPreviewPolygonPath(district.PolygonPoints, parentBounds, childSize, scale);
		using SKPaint fill = FillPaint(ChildPreviewDistrictFillColor);
		using SKPaint stroke = StrokePaint(ChildPreviewDistrictStrokeColor, 1.0f * scale);
		canvas.DrawPath(path, fill);
		canvas.DrawPath(path, stroke);
	}

	private static void DrawChildPreviewRoad(SKCanvas canvas, RectD parentBounds, SizeD childSize, RoadLine road, float scale)
	{
		using SKPaint stroke = StrokePaint(ChildPreviewRoadColor, 2.0f * scale);
		stroke.StrokeCap = SKStrokeCap.Round;
		stroke.StrokeJoin = SKStrokeJoin.Round;
		for (int i = 1; i < road.Points.Count; i++)
		{
			canvas.DrawLine(ToPreviewPixel(road.Points[i - 1], parentBounds, childSize, scale), ToPreviewPixel(road.Points[i], parentBounds, childSize, scale), stroke);
		}
	}

	private static void DrawChildPreviewPointOfInterest(SKCanvas canvas, RectD parentBounds, SizeD childSize, PointOfInterest poi, float scale)
	{
		SKPoint center = ToPreviewPixel(poi.Position, parentBounds, childSize, scale);
		using SKPaint fill = FillPaint(ChildPreviewPoiFillColor);
		using SKPaint stroke = StrokePaint(ChildPreviewPoiStrokeColor, 1.0f * scale);
		canvas.DrawCircle(center, 4.0f * scale, fill);
		canvas.DrawCircle(center, 4.0f * scale, stroke);
	}

	private static void DrawChildPreviewLabel(SKCanvas canvas, RectD parentBounds, SizeD childSize, MapLabel label, float scale)
	{
		SKPoint origin = ToPreviewPixel(label.Position, parentBounds, childSize, scale);
		DrawText(canvas, label.Text, origin.X, origin.Y + (10.0f * scale), 10.0f * scale, ChildPreviewTextColor, SKFontStyleWeight.Normal);
	}

	private static SKPath BuildPolygonPath(IReadOnlyList<PointD> points, float scale)
	{
		var path = new SKPath();
		path.MoveTo(ToPixel(points[0], scale));
		for (int i = 1; i < points.Count; i++)
		{
			path.LineTo(ToPixel(points[i], scale));
		}
		path.Close();
		return path;
	}

	private static SKPath BuildPreviewPolygonPath(IReadOnlyList<PointD> points, RectD parentBounds, SizeD childSize, float scale)
	{
		var path = new SKPath();
		path.MoveTo(ToPreviewPixel(points[0], parentBounds, childSize, scale));
		for (int i = 1; i < points.Count; i++)
		{
			path.LineTo(ToPreviewPixel(points[i], parentBounds, childSize, scale));
		}
		path.Close();
		return path;
	}

	private static void DrawText(SKCanvas canvas, string text, float x, float baselineY, float size, SKColor color, SKFontStyleWeight weight)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		using SKTypeface typeface = SKTypeface.FromFamilyName("Inter", weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) ?? SKTypeface.Default;
		using var font = new SKFont(typeface, size);
		using SKPaint paint = FillPaint(color);
		paint.IsAntialias = true;
		canvas.DrawText(text, x, baselineY, font, paint);
	}

	private static SKPoint ToPixel(PointD point, float scale)
	{
		return new SKPoint((float)(point.X * scale), (float)(point.Y * scale));
	}

	private static SKPoint ToPreviewPixel(PointD childPoint, RectD parentBounds, SizeD childSize, float scale)
	{
		double worldX = parentBounds.Left + childPoint.X / childSize.Width * parentBounds.Size.Width;
		double worldY = parentBounds.Top + childPoint.Y / childSize.Height * parentBounds.Size.Height;
		return ToPixel(new PointD(worldX, worldY), scale);
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

	private static bool IsMajorGridLine(double value, double baseGridStepMeters)
	{
		double majorStep = baseGridStepMeters * 10.0;
		if (majorStep <= 0.0)
		{
			return false;
		}

		double nearest = Math.Round(value / majorStep) * majorStep;
		return Math.Abs(value - nearest) < 0.001;
	}

	private static SKPaint FillPaint(SKColor color)
	{
		return new SKPaint
		{
			Color = color,
			IsAntialias = true,
			Style = SKPaintStyle.Fill
		};
	}

	private static SKPaint StrokePaint(SKColor color, float strokeWidth)
	{
		return new SKPaint
		{
			Color = color,
			IsAntialias = true,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = strokeWidth
		};
	}

	private static SKColor ToSkColor(RenderColor color)
	{
		return new SKColor(color.R, color.G, color.B, color.A);
	}

	private static SKColor ToSkColor(RenderColor color, float opacity)
	{
		return new SKColor(color.R, color.G, color.B, ToAlphaByte(opacity, color.A));
	}

	private static byte ToAlphaByte(float opacity, byte baseAlpha = byte.MaxValue)
	{
		return (byte)Math.Clamp((int)Math.Round(baseAlpha * Math.Clamp(opacity, 0.0f, 1.0f)), 0, byte.MaxValue);
	}

	private static SKFontStyleWeight ToSkFontWeight(RenderTextWeight weight)
	{
		return weight switch
		{
			RenderTextWeight.Bold => SKFontStyleWeight.Bold,
			RenderTextWeight.DemiBold => SKFontStyleWeight.SemiBold,
			_ => SKFontStyleWeight.Normal
		};
	}
}
