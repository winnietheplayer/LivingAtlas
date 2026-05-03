using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Editor.Tools;
using Xunit;

namespace LivingAtlas.Tests;

public sealed class GridSnapperTests
{
    [Fact]
    public void Snap_ReturnsOriginalPoint_WhenGridDisabled()
    {
        PointD point = new PointD(1.2, 3.4);
        GridSettings grid = new GridSettings(isEnabled: false, 1.0, showGrid: true, snapToGrid: true);
        
        PointD result = GridSnapper.Snap(point, grid);
        
        Assert.Equal(point.X, result.X);
        Assert.Equal(point.Y, result.Y);
    }

    [Fact]
    public void Snap_ReturnsOriginalPoint_WhenSnapToGridDisabled()
    {
        PointD point = new PointD(1.2, 3.4);
        GridSettings grid = new GridSettings(isEnabled: true, 1.0, showGrid: true, snapToGrid: false);
        
        PointD result = GridSnapper.Snap(point, grid);
        
        Assert.Equal(point.X, result.X);
        Assert.Equal(point.Y, result.Y);
    }

    [Fact]
    public void Snap_RoundsToNearestGridLine()
    {
        GridSettings grid = new GridSettings(isEnabled: true, 10.0, showGrid: true, snapToGrid: true);
        
        Assert.Equal(0.0, GridSnapper.Snap(new PointD(4.9, 2.1), grid).X);
        Assert.Equal(10.0, GridSnapper.Snap(new PointD(5.1, 2.1), grid).X);
        Assert.Equal(20.0, GridSnapper.Snap(new PointD(15.0, 20.1), grid).X);
        Assert.Equal(20.0, GridSnapper.Snap(new PointD(15.0, 20.1), grid).Y);
    }

    [Fact]
    public void Snap_HandlesNegativeCoordinates()
    {
        GridSettings grid = new GridSettings(isEnabled: true, 5.0, showGrid: true, snapToGrid: true);
        
        Assert.Equal(-5.0, GridSnapper.Snap(new PointD(-4.2, -7.8), grid).X);
        Assert.Equal(-10.0, GridSnapper.Snap(new PointD(-4.2, -7.8), grid).Y);
    }
}
