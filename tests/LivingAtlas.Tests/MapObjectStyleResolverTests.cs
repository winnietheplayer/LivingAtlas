using LivingAtlas.Rendering;
using Xunit;

namespace LivingAtlas.Tests;

public class MapObjectStyleResolverTests
{
	[Fact]
	public void DistrictStyleResolver_ReturnsDistinctKnownStyles()
	{
		var styles = new[]
		{
			MapObjectStyleResolver.GetDistrictStyle("district.default"),
			MapObjectStyleResolver.GetDistrictStyle("district.old"),
			MapObjectStyleResolver.GetDistrictStyle("district.boundary"),
			MapObjectStyleResolver.GetDistrictStyle("district.industrial"),
			MapObjectStyleResolver.GetDistrictStyle("district.slums")
		};

		Assert.Equal(styles.Length, styles.Distinct().Count());
		Assert.Equal(styles.Length, styles.Select(style => style.Fill).Distinct().Count());
		Assert.Equal(styles.Length, styles.Select(style => style.Stroke).Distinct().Count());
	}

	[Fact]
	public void RoadStyleResolver_ReturnsDistinctKnownStyles()
	{
		var primaryStyle = MapObjectStyleResolver.GetRoadStyle("road.primary");
		var alleyStyle = MapObjectStyleResolver.GetRoadStyle("road.alley");

		Assert.NotEqual(primaryStyle.Stroke, alleyStyle.Stroke);
		Assert.True(primaryStyle.StrokeWidth > alleyStyle.StrokeWidth);
	}

	[Fact]
	public void PoiStyleResolver_ReturnsDistinctKnownStyles()
	{
		var defaultStyle = MapObjectStyleResolver.GetPoiStyle("poi.default");
		var dangerStyle = MapObjectStyleResolver.GetPoiStyle("poi.danger");

		Assert.NotEqual(defaultStyle.Fill, dangerStyle.Fill);
		Assert.NotEqual(defaultStyle.Radius, dangerStyle.Radius);
	}

	[Fact]
	public void LabelStyleResolver_ReturnsDistinctKnownStyles()
	{
		var cityStyle = MapObjectStyleResolver.GetLabelStyle("label.city");
		var noteStyle = MapObjectStyleResolver.GetLabelStyle("label.note");

		Assert.NotEqual(cityStyle.Color, noteStyle.Color);
		Assert.True(cityStyle.FontSize > noteStyle.FontSize);
	}

	[Fact]
	public void StyleResolver_UnknownStyleKeysFallBackToDefaults()
	{
		Assert.Equal(MapObjectStyleResolver.GetDistrictStyle("district.default"), MapObjectStyleResolver.GetDistrictStyle("district.unknown"));
		Assert.Equal(MapObjectStyleResolver.GetRoadStyle("road.primary"), MapObjectStyleResolver.GetRoadStyle("road.unknown"));
		Assert.Equal(MapObjectStyleResolver.GetPoiStyle("poi.default"), MapObjectStyleResolver.GetPoiStyle("poi.unknown"));
		Assert.Equal(MapObjectStyleResolver.GetLabelStyle("label.district"), MapObjectStyleResolver.GetLabelStyle("label.unknown"));
	}
}
