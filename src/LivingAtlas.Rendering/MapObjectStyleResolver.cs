namespace LivingAtlas.Rendering;

public readonly record struct RenderColor(byte A, byte R, byte G, byte B)
{
	public static RenderColor FromRgb(byte red, byte green, byte blue)
	{
		return new RenderColor(byte.MaxValue, red, green, blue);
	}

	public static RenderColor FromArgb(byte alpha, byte red, byte green, byte blue)
	{
		return new RenderColor(alpha, red, green, blue);
	}
}

public enum RenderTextWeight
{
	Normal,
	DemiBold,
	Bold
}

public readonly record struct DistrictRenderStyle(RenderColor Fill, RenderColor Stroke, double StrokeWidth);

public readonly record struct RoadRenderStyle(RenderColor Stroke, double StrokeWidth);

public readonly record struct RoadAreaRenderStyle(RenderColor Fill, RenderColor Stroke, double StrokeWidth);

public readonly record struct PoiRenderStyle(RenderColor Fill, RenderColor Stroke, double StrokeWidth, double Radius);

public readonly record struct LabelRenderStyle(RenderColor Color, double FontSize, RenderTextWeight FontWeight);

public static class MapObjectStyleResolver
{
	public static DistrictRenderStyle GetDistrictStyle(string styleKey)
	{
		return styleKey switch
		{
			"" or "district.default" => new DistrictRenderStyle(RenderColor.FromArgb(104, 72, 142, 126), RenderColor.FromRgb(118, 214, 184), 2.0),
			"district.old" => new DistrictRenderStyle(RenderColor.FromArgb(112, 141, 102, 72), RenderColor.FromRgb(222, 174, 98), 2.0),
			"district.boundary" => new DistrictRenderStyle(RenderColor.FromArgb(24, 75, 104, 96), RenderColor.FromRgb(123, 244, 218), 4.0),
			"district.industrial" => new DistrictRenderStyle(RenderColor.FromArgb(116, 76, 91, 111), RenderColor.FromRgb(168, 194, 216), 2.0),
			"district.slums" => new DistrictRenderStyle(RenderColor.FromArgb(116, 99, 81, 48), RenderColor.FromRgb(176, 138, 78), 2.0),
			_ => GetDistrictStyle("district.default")
		};
	}

	public static RoadRenderStyle GetRoadStyle(string styleKey)
	{
		return styleKey switch
		{
			"" or "road.primary" => new RoadRenderStyle(RenderColor.FromRgb(224, 186, 118), 4.0),
			"road.secondary" => new RoadRenderStyle(RenderColor.FromRgb(198, 185, 154), 3.0),
			"road.alley" => new RoadRenderStyle(RenderColor.FromArgb(190, 168, 161, 142), 1.5),
			_ => GetRoadStyle("road.primary")
		};
	}

	public static RoadAreaRenderStyle GetRoadAreaStyle(string styleKey)
	{
		return styleKey switch
		{
			"road.area.primary" => new RoadAreaRenderStyle(RenderColor.FromArgb(182, 133, 112, 83), RenderColor.FromRgb(232, 194, 128), 2.5),
			"" or "road.area.secondary" => new RoadAreaRenderStyle(RenderColor.FromArgb(162, 112, 105, 91), RenderColor.FromRgb(205, 190, 157), 2.0),
			"road.area.alley" => new RoadAreaRenderStyle(RenderColor.FromArgb(132, 82, 78, 70), RenderColor.FromArgb(220, 168, 161, 142), 1.5),
			_ => GetRoadAreaStyle("road.area.secondary")
		};
	}

	public static PoiRenderStyle GetPoiStyle(string styleKey)
	{
		return styleKey switch
		{
			"" or "poi.default" => new PoiRenderStyle(RenderColor.FromRgb(238, 200, 96), RenderColor.FromRgb(42, 45, 50), 2.0, 7.0),
			"poi.gate" => new PoiRenderStyle(RenderColor.FromRgb(111, 176, 225), RenderColor.FromRgb(34, 48, 61), 2.0, 8.0),
			"poi.landmark" => new PoiRenderStyle(RenderColor.FromRgb(136, 211, 154), RenderColor.FromRgb(31, 58, 43), 2.0, 9.0),
			"poi.danger" => new PoiRenderStyle(RenderColor.FromRgb(226, 101, 91), RenderColor.FromRgb(66, 34, 34), 2.0, 8.0),
			_ => GetPoiStyle("poi.default")
		};
	}

	public static LabelRenderStyle GetLabelStyle(string styleKey)
	{
		return styleKey switch
		{
			"label.city" => new LabelRenderStyle(RenderColor.FromRgb(248, 236, 197), 26.0, RenderTextWeight.Bold),
			"" or "label.district" => new LabelRenderStyle(RenderColor.FromRgb(245, 240, 224), 18.0, RenderTextWeight.DemiBold),
			"label.map-title" => new LabelRenderStyle(RenderColor.FromRgb(245, 247, 250), 30.0, RenderTextWeight.Bold),
			"label.note" => new LabelRenderStyle(RenderColor.FromArgb(205, 209, 212, 218), 14.0, RenderTextWeight.Normal),
			_ => GetLabelStyle("label.district")
		};
	}
}
