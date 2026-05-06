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
    public void RoadArea_RequiresAtLeastThreePoints()
    {
        Guid layerId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new RoadArea(
            Guid.NewGuid(),
            "Short Road Area",
            layerId,
            new[]
            {
                new PointD(1.0, 2.0),
                new PointD(3.0, 4.0)
            }));
    }

    [Fact]
    public void RoadArea_RoadKindDefaultsToSecondaryAndEmptyNormalizesToDefault()
    {
        RoadArea roadArea = TestData.CreateRoadArea(Guid.NewGuid());

        Assert.Equal(RoadArea.DefaultRoadKind, roadArea.RoadKind);

        roadArea.SetRoadKind(" primary ");
        Assert.Equal("primary", roadArea.RoadKind);

        roadArea.SetRoadKind(null);
        Assert.Equal(RoadArea.DefaultRoadKind, roadArea.RoadKind);
    }

    [Fact]
    public void RoadArea_TextureFillDefaultsSetAndClear()
    {
        RoadArea roadArea = TestData.CreateRoadArea(Guid.NewGuid());

        Assert.Null(roadArea.FillTextureAssetId);
        Assert.Equal(RoadArea.DefaultTextureTileSizeMeters, roadArea.TextureTileSizeMeters);

        roadArea.SetTextureFill(" road.cobble.01 ", 6.5);

        Assert.Equal("road.cobble.01", roadArea.FillTextureAssetId);
        Assert.Equal(6.5, roadArea.TextureTileSizeMeters);

        roadArea.ClearTextureFill();

        Assert.Null(roadArea.FillTextureAssetId);
        Assert.Equal(RoadArea.DefaultTextureTileSizeMeters, roadArea.TextureTileSizeMeters);

        Assert.Throws<ArgumentOutOfRangeException>(() => roadArea.SetTextureFill("road.cobble.01", 0.0));
    }

    [Fact]
    public void RoadArea_EditsPolygonPointsAndRejectsInvalidIndexes()
    {
        RoadArea roadArea = TestData.CreateRoadArea(Guid.NewGuid());

        roadArea.SetPoint(1, new PointD(90.0, 25.0));
        roadArea.InsertPoint(2, new PointD(90.0, 35.0));

        Assert.Equal(new PointD(90.0, 25.0), roadArea.PolygonPoints[1]);
        Assert.Equal(new PointD(90.0, 35.0), roadArea.PolygonPoints[2]);

        roadArea.RemovePoint(2);

        Assert.Equal(4, roadArea.PolygonPoints.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => roadArea.SetPoint(99, new PointD(0.0, 0.0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => roadArea.InsertPoint(-1, new PointD(0.0, 0.0)));
    }

    [Fact]
    public void RoadArea_RemovePointBelowThreeThrows()
    {
        Guid layerId = Guid.NewGuid();
        RoadArea roadArea = new RoadArea(
            Guid.NewGuid(),
            "Triangle Road Area",
            layerId,
            new[]
            {
                new PointD(0.0, 0.0),
                new PointD(10.0, 0.0),
                new PointD(0.0, 10.0)
            });

        Assert.Throws<InvalidOperationException>(() => roadArea.RemovePoint(1));
    }

    [Fact]
    public void RoadArea_MoveByMovesAllPoints()
    {
        RoadArea roadArea = TestData.CreateRoadArea(Guid.NewGuid());

        roadArea.MoveBy(new PointD(5.0, -10.0));

        Assert.Equal(new PointD(15.0, 10.0), roadArea.PolygonPoints[0]);
        Assert.Equal(new PointD(85.0, 10.0), roadArea.PolygonPoints[1]);
        Assert.Equal(new PointD(85.0, 40.0), roadArea.PolygonPoints[2]);
        Assert.Equal(new PointD(15.0, 40.0), roadArea.PolygonPoints[3]);
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
    public void DistrictShape_TextureFillDefaultsSetAndClear()
    {
        DistrictShape district = TestData.CreateDistrict(Guid.NewGuid());

        Assert.Null(district.FillTextureAssetId);
        Assert.Equal(DistrictShape.DefaultTextureTileSizeMeters, district.TextureTileSizeMeters);

        district.SetTextureFill(" ground.dirt.01 ", 12.5);

        Assert.Equal("ground.dirt.01", district.FillTextureAssetId);
        Assert.Equal(12.5, district.TextureTileSizeMeters);

        district.SetTextureFill(null, 8.0);

        Assert.Null(district.FillTextureAssetId);
        Assert.Equal(DistrictShape.DefaultTextureTileSizeMeters, district.TextureTileSizeMeters);

        Assert.Throws<ArgumentOutOfRangeException>(() => district.SetTextureFill("ground.dirt.01", 0.0));
    }

    [Fact]
    public void DistrictShape_TextureFillIsIndependentFromChildMapTextures()
    {
        DistrictShape parentDistrict = TestData.CreateDistrict(Guid.NewGuid(), childMapId: Guid.NewGuid());
        DistrictShape childDistrict = TestData.CreateDistrict(Guid.NewGuid());
        parentDistrict.SetTextureFill("parent.texture", 40.0);

        childDistrict.SetTextureFill("child.texture", 5.0);

        Assert.Equal("parent.texture", parentDistrict.FillTextureAssetId);
        Assert.Equal(40.0, parentDistrict.TextureTileSizeMeters);
        Assert.Equal("child.texture", childDistrict.FillTextureAssetId);
        Assert.Equal(5.0, childDistrict.TextureTileSizeMeters);
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
