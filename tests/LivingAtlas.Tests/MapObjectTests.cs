using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Tests;

public sealed class MapObjectTests
{
    [Fact]
    public void RoadLine_RequiresAtLeastTwoPoints()
    {
        Guid layerId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new RoadLine(
            Guid.NewGuid(),
            "Short Road",
            layerId,
            new[] { new PointD(1.0, 2.0) }));
    }

    [Fact]
    public void MapObject_DefaultDescriptionIsEmpty()
    {
        PointOfInterest poi = TestData.CreatePointOfInterest(Guid.NewGuid());

        Assert.Equal(string.Empty, poi.Description);
    }

    [Fact]
    public void MapObject_SetDescription_UpdatesValueAndPreservesMultilineText()
    {
        PointOfInterest poi = TestData.CreatePointOfInterest(Guid.NewGuid());

        poi.SetDescription("First line\r\n  Second line  ");

        Assert.Equal("First line\r\n  Second line  ", poi.Description);
    }

    [Fact]
    public void MapObject_SetDescription_NullNormalizesToEmpty()
    {
        PointOfInterest poi = TestData.CreatePointOfInterest(Guid.NewGuid());

        poi.SetDescription("Notes");
        poi.SetDescription(null);

        Assert.Equal(string.Empty, poi.Description);
    }

    [Fact]
    public void DistrictShape_CreatesWithPolygon()
    {
        Guid layerId = Guid.NewGuid();

        DistrictShape district = TestData.CreateDistrict(layerId);

        Assert.Equal(MapObjectType.DistrictShape, district.ObjectType);
        Assert.Equal(4, district.PolygonPoints.Count);
        Assert.Equal(layerId, district.LayerId);
    }

    [Fact]
    public void PointOfInterest_StoresPosition()
    {
        Guid layerId = Guid.NewGuid();
        PointOfInterest poi = new PointOfInterest(
            Guid.NewGuid(),
            "Gate",
            layerId,
            new PointD(42.0, 84.0),
            "gate");

        Assert.Equal(new PointD(42.0, 84.0), poi.Position);
        Assert.Equal(MapObjectType.PointOfInterest, poi.ObjectType);
    }

    [Fact]
    public void PointOfInterest_CategoryDefaultsToEmptyAndNullNormalizesToEmpty()
    {
        PointOfInterest poi = TestData.CreatePointOfInterest(Guid.NewGuid());

        Assert.Equal(string.Empty, poi.Category);

        poi.SetCategory("Gate");
        poi.SetCategory(null);

        Assert.Equal(string.Empty, poi.Category);
    }

    [Fact]
    public void RoadLine_RoadKindDefaultsToSecondaryAndEmptyNormalizesToDefault()
    {
        RoadLine road = TestData.CreateRoad(Guid.NewGuid());

        Assert.Equal(RoadLine.DefaultRoadKind, road.RoadKind);

        road.SetRoadKind(" primary ");
        Assert.Equal("primary", road.RoadKind);

        road.SetRoadKind(" ");
        Assert.Equal(RoadLine.DefaultRoadKind, road.RoadKind);
    }

    [Fact]
    public void DistrictShape_DistrictKindDefaultsToGenericAndEmptyNormalizesToDefault()
    {
        DistrictShape district = TestData.CreateDistrict(Guid.NewGuid());

        Assert.Equal(DistrictShape.DefaultDistrictKind, district.DistrictKind);

        district.SetDistrictKind(" market ");
        Assert.Equal("market", district.DistrictKind);

        district.SetDistrictKind(null);
        Assert.Equal(DistrictShape.DefaultDistrictKind, district.DistrictKind);
    }

    [Fact]
    public void MapLabel_StoresText()
    {
        Guid layerId = Guid.NewGuid();
        MapLabel label = new MapLabel(
            Guid.NewGuid(),
            "Title",
            layerId,
            new PointD(10.0, 15.0),
            "Market Square");

        Assert.Equal("Market Square", label.Text);
        Assert.Equal(MapObjectType.MapLabel, label.ObjectType);
    }

    [Fact]
    public void MapLabel_LabelKindDefaultsToNoteAndEmptyNormalizesToDefault()
    {
        MapLabel label = TestData.CreateLabel(Guid.NewGuid());

        Assert.Equal(MapLabel.DefaultLabelKind, label.LabelKind);

        label.SetLabelKind(" city ");
        Assert.Equal("city", label.LabelKind);

        label.SetLabelKind("");
        Assert.Equal(MapLabel.DefaultLabelKind, label.LabelKind);
    }
}
