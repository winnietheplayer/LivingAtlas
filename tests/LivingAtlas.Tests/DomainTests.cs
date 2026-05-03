using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;

namespace LivingAtlas.Tests;

public sealed class DomainTests
{
    [Fact]
    public void SizeD_RejectsNegativeWidthOrHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SizeD(-1.0, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SizeD(1.0, -1.0));
    }

    [Fact]
    public void RectD_ContainsPointsInsideAndOnEdges()
    {
        RectD rect = new RectD(10.0, 20.0, 100.0, 50.0);

        Assert.True(rect.Contains(new PointD(10.0, 20.0)));
        Assert.True(rect.Contains(new PointD(60.0, 45.0)));
        Assert.True(rect.Contains(new PointD(110.0, 70.0)));
        Assert.False(rect.Contains(new PointD(9.9, 45.0)));
        Assert.False(rect.Contains(new PointD(60.0, 70.1)));
    }

    [Fact]
    public void MapDocument_CreatesCityMapWithExpectedSize()
    {
        MapDocument map = TestData.CreateCityMap();

        Assert.Equal(2600.0, map.RealSizeMeters.Width);
        Assert.Equal(1800.0, map.RealSizeMeters.Height);
        Assert.Equal(MapScaleType.City, map.ScaleType);
    }

    [Fact]
    public void MapDocument_AddsLayer()
    {
        MapDocument map = TestData.CreateCityMap();
        MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);

        map.AddLayer(layer);

        Assert.Same(layer, Assert.Single(map.Layers));
    }

    [Fact]
    public void MapDocument_AddsAndRemovesChildMapId()
    {
        MapDocument map = TestData.CreateCityMap();
        Guid childMapId = Guid.NewGuid();

        map.AddChildMapId(childMapId);

        Assert.Equal(childMapId, Assert.Single(map.ChildrenMapIds));

        Assert.True(map.RemoveChildMapId(childMapId));
        Assert.Empty(map.ChildrenMapIds);
    }
}
