using System;
using LivingAtlas.Domain.Maps;
using Xunit;

namespace LivingAtlas.Tests;

public class LayerRenameTests
{
    [Fact]
    public void MapLayer_Rename_UpdatesName()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Original Name", MapLayerType.PointsOfInterest);
        layer.Rename("New Name");
        Assert.Equal("New Name", layer.Name);
    }

    [Fact]
    public void MapLayer_Rename_TrimsName()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Original Name", MapLayerType.PointsOfInterest);
        layer.Rename("  Spaced Name  ");
        Assert.Equal("Spaced Name", layer.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void MapLayer_Rename_RejectsEmptyOrWhitespaceName(string emptyName)
    {
        var layer = new MapLayer(Guid.NewGuid(), "Original Name", MapLayerType.PointsOfInterest);
        Assert.Throws<ArgumentException>(() => layer.Rename(emptyName));
    }

    [Fact]
    public void MapLayer_Rename_RejectsNullName()
    {
        var layer = new MapLayer(Guid.NewGuid(), "Original Name", MapLayerType.PointsOfInterest);
        Assert.Throws<ArgumentNullException>(() => layer.Rename(null!));
    }
}
