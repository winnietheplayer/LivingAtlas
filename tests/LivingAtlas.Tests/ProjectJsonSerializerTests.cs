using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Commands;
using LivingAtlas.ProjectSystem;

namespace LivingAtlas.Tests;

public sealed class ProjectJsonSerializerTests
{
    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsProjectMapsObjectsAndHierarchy()
    {
        (CampaignMapProject project, MapDocument rootMap, Guid childMapId) = CreateProjectWithHierarchy();
        string path = Path.Combine("C:\\tmp", "LivingAtlas.Tests", $"{Guid.NewGuid()}.json");

        try
        {
            await ProjectJsonSerializer.SaveAsync(project, path);

            CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(path);

            Assert.Equal(project.Id, loaded.Id);
            Assert.Equal(project.Name, loaded.Name);
            Assert.Equal(rootMap.Id, loaded.RootMapId);

            MapDocument loadedRoot = loaded.RootMap;
            Assert.Equal(2600.0, loadedRoot.RealSizeMeters.Width);
            Assert.Equal(1800.0, loadedRoot.RealSizeMeters.Height);
            Assert.Equal(4, loadedRoot.Layers.Count);
            Assert.Equal(childMapId, Assert.Single(loadedRoot.ChildrenMapIds));

            Assert.Contains(loadedRoot.Layers, layer => layer.LayerType == MapLayerType.Districts);
            Assert.Contains(loadedRoot.Layers, layer => layer.LayerType == MapLayerType.Streets);
            Assert.Contains(loadedRoot.Layers, layer => layer.LayerType == MapLayerType.PointsOfInterest);
            Assert.Contains(loadedRoot.Layers, layer => layer.LayerType == MapLayerType.Labels);

            DistrictShape loadedDistrict = Assert.IsType<DistrictShape>(
                loadedRoot.Layers.Single(layer => layer.LayerType == MapLayerType.Districts).Objects.Single());
            Assert.Equal(childMapId, loadedDistrict.ChildMapId);
            Assert.Equal(4, loadedDistrict.PolygonPoints.Count);

            Assert.IsType<RoadLine>(
                loadedRoot.Layers.Single(layer => layer.LayerType == MapLayerType.Streets).Objects.Single());
            Assert.IsType<PointOfInterest>(
                loadedRoot.Layers.Single(layer => layer.LayerType == MapLayerType.PointsOfInterest).Objects.Single());
            Assert.IsType<MapLabel>(
                loadedRoot.Layers.Single(layer => layer.LayerType == MapLayerType.Labels).Objects.Single());

            MapDocument? loadedChild = loaded.FindMap(childMapId);

            Assert.NotNull(loadedChild);
            Assert.Equal(loadedRoot.Id, loadedChild!.ParentMapId);
            Assert.Equal(MapScaleType.District, loadedChild.ScaleType);
            Assert.Equal(2, loaded.Maps.Count);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsGridSettings()
    {
        MapDocument map = TestData.CreateCityMap();
        map.SetGridSettings(new GridSettings(isEnabled: true, 12.5, showGrid: true, snapToGrid: true));
        CampaignMapProject project = TestData.CreateProject(map);
        string path = Path.Combine("C:\\tmp", "LivingAtlas.Tests", $"{Guid.NewGuid()}.json");

        try
        {
            await ProjectJsonSerializer.SaveAsync(project, path);
            CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(path);
            GridSettings loadedSettings = loaded.RootMap.GridSettings;

            Assert.True(loadedSettings.IsEnabled);
            Assert.Equal(12.5, loadedSettings.CellSizeMeters);
            Assert.True(loadedSettings.ShowGrid);
            Assert.True(loadedSettings.SnapToGrid);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static (CampaignMapProject Project, MapDocument RootMap, Guid ChildMapId) CreateProjectWithHierarchy()
    {
        MapDocument rootMap = TestData.CreateCityMap();
        CampaignMapProject project = TestData.CreateProject(rootMap);

        MapLayer districtsLayer = TestData.CreateLayer(name: "Districts", layerType: MapLayerType.Districts);
        MapLayer roadsLayer = TestData.CreateLayer(name: "Roads", layerType: MapLayerType.Streets);
        MapLayer poiLayer = TestData.CreateLayer(name: "POI", layerType: MapLayerType.PointsOfInterest);
        MapLayer labelsLayer = TestData.CreateLayer(name: "Labels", layerType: MapLayerType.Labels);

        DistrictShape district = TestData.CreateDistrict(districtsLayer.Id);
        districtsLayer.AddObject(district);
        roadsLayer.AddObject(TestData.CreateRoad(roadsLayer.Id));
        poiLayer.AddObject(TestData.CreatePointOfInterest(poiLayer.Id));
        labelsLayer.AddObject(TestData.CreateLabel(labelsLayer.Id));

        rootMap.AddLayer(districtsLayer);
        rootMap.AddLayer(roadsLayer);
        rootMap.AddLayer(poiLayer);
        rootMap.AddLayer(labelsLayer);

        Guid childMapId = Guid.NewGuid();
        new CreateChildMapCommand(project, rootMap, district, childMapId).Execute();

        return (project, rootMap, childMapId);
    }
}
