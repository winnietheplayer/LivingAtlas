using System;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using Xunit;

namespace LivingAtlas.Tests;

public class MapMetadataTests
{
    [Fact]
    public void MapDocument_Rename_UpdatesName()
    {
        var map = new MapDocument(Guid.NewGuid(), "Old Name", MapScaleType.City, new SizeD(1000, 1000));
        map.Rename("New Name");
        Assert.Equal("New Name", map.Name);
    }

    [Fact]
    public void MapDocument_Rename_ThrowsOnEmpty()
    {
        var map = new MapDocument(Guid.NewGuid(), "Name", MapScaleType.City, new SizeD(1000, 1000));
        Assert.Throws<ArgumentException>(() => map.Rename(""));
        Assert.Throws<ArgumentException>(() => map.Rename(null!));
    }

    [Fact]
    public void MapDocument_SetScaleType_UpdatesScale()
    {
        var map = new MapDocument(Guid.NewGuid(), "Name", MapScaleType.City, new SizeD(1000, 1000));
        map.SetScaleType(MapScaleType.District);
        Assert.Equal(MapScaleType.District, map.ScaleType);
    }

    [Fact]
    public void MapDocument_SetRealSize_UpdatesSize()
    {
        var map = new MapDocument(Guid.NewGuid(), "Name", MapScaleType.City, new SizeD(1000, 1000));
        map.SetRealSize(new SizeD(2000, 3000));
        Assert.Equal(2000, map.RealSizeMeters.Width);
        Assert.Equal(3000, map.RealSizeMeters.Height);
    }

    [Fact]
    public void MapDocument_SetRealSize_ThrowsOnInvalidSize()
    {
        var map = new MapDocument(Guid.NewGuid(), "Name", MapScaleType.City, new SizeD(1000, 1000));
        // SizeD(0, 1000) is valid for SizeD but invalid for SetRealSize (ArgumentException)
        Assert.Throws<ArgumentException>(() => map.SetRealSize(new SizeD(0, 1000)));
        // SizeD(1000, -1) is invalid for SizeD constructor (ArgumentOutOfRangeException)
        Assert.Throws<ArgumentOutOfRangeException>(() => map.SetRealSize(new SizeD(1000, -1)));
    }

    [Theory]
    [InlineData(MapScaleType.BattleMap, 5.0)]
    [InlineData(MapScaleType.District, 10.0)]
    [InlineData(MapScaleType.City, 100.0)]
    public void MapDocument_DefaultFeetPerUnit_ComesFromScaleType(MapScaleType scaleType, double expectedFeetPerUnit)
    {
        var map = new MapDocument(Guid.NewGuid(), "Name", scaleType, new SizeD(1000, 1000));

        Assert.Equal(expectedFeetPerUnit, map.FeetPerUnit);
    }

    [Fact]
    public void MapDocument_SetFeetPerUnit_UpdatesValue()
    {
        var map = new MapDocument(Guid.NewGuid(), "Name", MapScaleType.City, new SizeD(1000, 1000));

        map.SetFeetPerUnit(25.0);

        Assert.Equal(25.0, map.FeetPerUnit);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void MapDocument_SetFeetPerUnit_ThrowsOnInvalidValue(double feetPerUnit)
    {
        var map = new MapDocument(Guid.NewGuid(), "Name", MapScaleType.City, new SizeD(1000, 1000));

        Assert.Throws<ArgumentOutOfRangeException>(() => map.SetFeetPerUnit(feetPerUnit));
    }
}
