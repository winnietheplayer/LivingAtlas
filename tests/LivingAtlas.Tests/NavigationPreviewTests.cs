using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Hierarchy;
using LivingAtlas.Editor.Navigation;

namespace LivingAtlas.Tests;

public sealed class NavigationPreviewTests
{
    [Fact]
    public void MapBreadcrumbService_BuildsRootChildGrandchildBreadcrumbs()
    {
        MapDocument root = TestData.CreateCityMap();
        MapDocument child = new MapDocument(
            Guid.NewGuid(),
            "Child",
            MapScaleType.District,
            new SizeD(500.0, 300.0),
            root.Id);
        MapDocument grandchild = new MapDocument(
            Guid.NewGuid(),
            "Grandchild",
            MapScaleType.Building,
            new SizeD(100.0, 80.0),
            child.Id);
        CampaignMapProject project = new CampaignMapProject(
            Guid.NewGuid(),
            "Breadcrumb Project",
            root.Id,
            new[] { root, child, grandchild });

        root.AddChildMapId(child.Id);
        child.AddChildMapId(grandchild.Id);

        IReadOnlyList<MapBreadcrumb> breadcrumbs = MapBreadcrumbService.BuildBreadcrumbs(project, grandchild.Id);

        Assert.Collection(
            breadcrumbs,
            item =>
            {
                Assert.Equal(root.Id, item.MapId);
                Assert.Equal(root.Name, item.Name);
            },
            item =>
            {
                Assert.Equal(child.Id, item.MapId);
                Assert.Equal(child.Name, item.Name);
            },
            item =>
            {
                Assert.Equal(grandchild.Id, item.MapId);
                Assert.Equal(grandchild.Name, item.Name);
            });
    }

    [Fact]
    public void ChildMapPreviewTransform_MapsCornersAndCenterIntoParentBounds()
    {
        RectD parentBounds = new RectD(10.0, 20.0, 200.0, 100.0);
        SizeD childSize = new SizeD(400.0, 200.0);

        Assert.Equal(
            new PointD(10.0, 20.0),
            ChildMapPreviewTransform.ChildToParent(new PointD(0.0, 0.0), parentBounds, childSize));
        Assert.Equal(
            new PointD(210.0, 120.0),
            ChildMapPreviewTransform.ChildToParent(new PointD(400.0, 200.0), parentBounds, childSize));
        Assert.Equal(
            new PointD(110.0, 70.0),
            ChildMapPreviewTransform.ChildToParent(new PointD(200.0, 100.0), parentBounds, childSize));
    }
}
