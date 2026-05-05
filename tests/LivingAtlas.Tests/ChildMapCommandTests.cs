using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Commands;

namespace LivingAtlas.Tests;

public sealed class ChildMapCommandTests
{
    [Fact]
    public void CreateChildMapCommand_CreatesChildMapAndHierarchyLinks()
    {
        (CampaignMapProject project, MapDocument parentMap, DistrictShape district) = CreateProjectWithDistrict();
        Guid childMapId = Guid.NewGuid();
        CreateChildMapCommand command = new CreateChildMapCommand(project, parentMap, district, childMapId: childMapId);

        command.Execute();

        MapDocument? childMap = project.FindMap(childMapId);

        Assert.NotNull(childMap);
        Assert.Equal(childMapId, Assert.Single(parentMap.ChildrenMapIds));
        Assert.Equal(childMapId, district.ChildMapId);
        Assert.Equal(parentMap.Id, childMap!.ParentMapId);
        Assert.Equal(MapScaleType.District, childMap.ScaleType);
    }

    [Fact]
    public void CreateChildMapCommand_UndoRemovesChildMapAndClearsLinks()
    {
        (CampaignMapProject project, MapDocument parentMap, DistrictShape district) = CreateProjectWithDistrict();
        Guid childMapId = Guid.NewGuid();
        CreateChildMapCommand command = new CreateChildMapCommand(project, parentMap, district, childMapId: childMapId);

        command.Execute();
        command.Undo();

        Assert.Null(project.FindMap(childMapId));
        Assert.Empty(parentMap.ChildrenMapIds);
        Assert.Null(district.ChildMapId);
    }

    private static (CampaignMapProject Project, MapDocument ParentMap, DistrictShape District) CreateProjectWithDistrict()
    {
        MapDocument parentMap = TestData.CreateCityMap();
        MapLayer districtLayer = TestData.CreateLayer(layerType: MapLayerType.Districts);
        DistrictShape district = TestData.CreateDistrict(districtLayer.Id);
        CampaignMapProject project = TestData.CreateProject(parentMap);

        parentMap.AddLayer(districtLayer);
        districtLayer.AddObject(district);

        return (project, parentMap, district);
    }
}
