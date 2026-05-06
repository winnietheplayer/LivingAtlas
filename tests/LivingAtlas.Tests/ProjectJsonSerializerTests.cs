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
	public async Task SaveAndLoadAsync_RoundTripsObjectDescription()
	{
		string tempFileName = Path.GetTempFileName();
		try
		{
			MapDocument rootMap = TestData.CreateCityMap();
			MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.PointsOfInterest);
			PointOfInterest poi = TestData.CreatePointOfInterest(layer.Id);
			poi.SetDescription("First line\r\nSecond line  ");
			layer.AddObject(poi);
			rootMap.AddLayer(layer);
			CampaignMapProject project = TestData.CreateProject(rootMap);

			await ProjectJsonSerializer.SaveAsync(project, tempFileName);

			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);
			PointOfInterest loadedPoi = Assert.IsType<PointOfInterest>(
				loaded.RootMap.Layers.Single(l => l.Id == layer.Id).Objects.Single());
			Assert.Equal("First line\r\nSecond line  ", loadedPoi.Description);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task SaveAndLoadAsync_RoundTripsTypeSpecificObjectProperties()
	{
		string tempFileName = Path.GetTempFileName();
		try
		{
			MapDocument rootMap = TestData.CreateCityMap();
			MapLayer districtsLayer = TestData.CreateLayer(layerType: MapLayerType.Districts);
			MapLayer roadsLayer = TestData.CreateLayer(layerType: MapLayerType.Streets);
			MapLayer poiLayer = TestData.CreateLayer(layerType: MapLayerType.PointsOfInterest);
			MapLayer labelsLayer = TestData.CreateLayer(layerType: MapLayerType.Labels);
			DistrictShape district = TestData.CreateDistrict(districtsLayer.Id);
			RoadLine road = TestData.CreateRoad(roadsLayer.Id);
			PointOfInterest poi = TestData.CreatePointOfInterest(poiLayer.Id);
			MapLabel label = TestData.CreateLabel(labelsLayer.Id);

			district.SetDistrictKind("market");
			district.SetTextureFill("ground.dirt.01", 12.5);
			road.SetRoadKind("primary");
			poi.SetCategory("landmark");
			label.SetLabelKind("map-title");
			districtsLayer.AddObject(district);
			roadsLayer.AddObject(road);
			poiLayer.AddObject(poi);
			labelsLayer.AddObject(label);
			rootMap.AddLayer(districtsLayer);
			rootMap.AddLayer(roadsLayer);
			rootMap.AddLayer(poiLayer);
			rootMap.AddLayer(labelsLayer);
			CampaignMapProject project = TestData.CreateProject(rootMap);

			await ProjectJsonSerializer.SaveAsync(project, tempFileName);

			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);
			DistrictShape loadedDistrict = Assert.IsType<DistrictShape>(
				loaded.RootMap.Layers.Single(l => l.Id == districtsLayer.Id).Objects.Single());
			RoadLine loadedRoad = Assert.IsType<RoadLine>(
				loaded.RootMap.Layers.Single(l => l.Id == roadsLayer.Id).Objects.Single());
			PointOfInterest loadedPoi = Assert.IsType<PointOfInterest>(
				loaded.RootMap.Layers.Single(l => l.Id == poiLayer.Id).Objects.Single());
			MapLabel loadedLabel = Assert.IsType<MapLabel>(
				loaded.RootMap.Layers.Single(l => l.Id == labelsLayer.Id).Objects.Single());

			Assert.Equal("market", loadedDistrict.DistrictKind);
			Assert.Equal("ground.dirt.01", loadedDistrict.FillTextureAssetId);
			Assert.Equal(12.5, loadedDistrict.TextureTileSizeMeters);
			Assert.Equal("primary", loadedRoad.RoadKind);
			Assert.Equal("landmark", loadedPoi.Category);
			Assert.Equal("map-title", loadedLabel.LabelKind);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task LoadAsync_OldJsonWithoutTextureFill_LoadsDistrictTextureDefaults()
	{
		string tempFileName = Path.GetTempFileName();
		Guid projectId = Guid.NewGuid();
		Guid mapId = Guid.NewGuid();
		Guid layerId = Guid.NewGuid();
		Guid objectId = Guid.NewGuid();
		try
		{
			string json = $$"""
			{
			  "id": "{{projectId}}",
			  "name": "Old Project",
			  "rootMapId": "{{mapId}}",
			  "maps": [
			    {
			      "id": "{{mapId}}",
			      "name": "Old Map",
			      "scaleType": "City",
			      "realSizeMeters": {
			        "width": 2600.0,
			        "height": 1800.0
			      },
			      "parentMapId": null,
			      "gridSettings": {
			        "isEnabled": true,
			        "cellSizeMeters": 10.0,
			        "showGrid": true,
			        "snapToGrid": false
			      },
			      "layers": [
			        {
			          "id": "{{layerId}}",
			          "name": "Districts",
			          "layerType": "Districts",
			          "isVisible": true,
			          "isLocked": false,
			          "objects": [
			            {
			              "id": "{{objectId}}",
			              "name": "Old District",
			              "objectType": "DistrictShape",
			              "layerId": "{{layerId}}",
			              "tags": [],
			              "styleKey": "district.old",
			              "description": "",
			              "districtKind": "generic",
			              "points": [
			                { "x": 0.0, "y": 0.0 },
			                { "x": 100.0, "y": 0.0 },
			                { "x": 0.0, "y": 100.0 }
			              ],
			              "childMapId": null
			            }
			          ]
			        }
			      ],
			      "childrenMapIds": []
			    }
			  ]
			}
			""";

			await File.WriteAllTextAsync(tempFileName, json);

			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);
			DistrictShape loadedDistrict = Assert.IsType<DistrictShape>(
				loaded.RootMap.Layers.Single().Objects.Single());
			Assert.Null(loadedDistrict.FillTextureAssetId);
			Assert.Equal(DistrictShape.DefaultTextureTileSizeMeters, loadedDistrict.TextureTileSizeMeters);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task LoadAsync_OldJsonWithoutDescription_LoadsEmptyDescription()
	{
		string tempFileName = Path.GetTempFileName();
		Guid projectId = Guid.NewGuid();
		Guid mapId = Guid.NewGuid();
		Guid layerId = Guid.NewGuid();
		Guid objectId = Guid.NewGuid();
		try
		{
			string json = $$"""
			{
			  "id": "{{projectId}}",
			  "name": "Old Project",
			  "rootMapId": "{{mapId}}",
			  "maps": [
			    {
			      "id": "{{mapId}}",
			      "name": "Old Map",
			      "scaleType": "City",
			      "realSizeMeters": {
			        "width": 2600.0,
			        "height": 1800.0
			      },
			      "parentMapId": null,
			      "gridSettings": {
			        "isEnabled": true,
			        "cellSizeMeters": 10.0,
			        "showGrid": true,
			        "snapToGrid": false
			      },
			      "layers": [
			        {
			          "id": "{{layerId}}",
			          "name": "POI",
			          "layerType": "PointsOfInterest",
			          "isVisible": true,
			          "isLocked": false,
			          "objects": [
			            {
			              "id": "{{objectId}}",
			              "name": "Gate",
			              "objectType": "PointOfInterest",
			              "layerId": "{{layerId}}",
			              "tags": [],
			              "styleKey": "poi.gate",
			              "points": [],
			              "position": {
			                "x": 100.0,
			                "y": 200.0
			              },
			              "iconKey": "gate",
			              "childMapId": null
			            }
			          ]
			        }
			      ],
			      "childrenMapIds": []
			    }
			  ]
			}
			""";

			await File.WriteAllTextAsync(tempFileName, json);

			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);
			PointOfInterest loadedPoi = Assert.IsType<PointOfInterest>(
				loaded.RootMap.Layers.Single().Objects.Single());
			Assert.Equal(string.Empty, loadedPoi.Description);
			Assert.Equal(string.Empty, loadedPoi.Category);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task SaveAndLoadAsync_RoundTripsGridSettings()
	{
		string tempFileName = Path.GetTempFileName();
		try
		{
			MapDocument rootMap = TestData.CreateCityMap();
			CampaignMapProject project = TestData.CreateProject(rootMap);
			GridSettings gridSettings = new GridSettings(isEnabled: true, 5.0, showGrid: true, snapToGrid: true);
			rootMap.SetGridSettings(gridSettings);
			await ProjectJsonSerializer.SaveAsync(project, tempFileName);
			CampaignMapProject project2 = await ProjectJsonSerializer.LoadAsync(tempFileName);
			GridSettings gridSettings2 = project2.RootMap.GridSettings;
			Assert.Equal(gridSettings.IsEnabled, gridSettings2.IsEnabled);
			Assert.Equal(gridSettings.CellSizeMeters, gridSettings2.CellSizeMeters);
			Assert.Equal(gridSettings.ShowGrid, gridSettings2.ShowGrid);
			Assert.Equal(gridSettings.SnapToGrid, gridSettings2.SnapToGrid);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task SaveAndLoadAsync_RoundTripsLayerVisibility()
	{
		string tempFileName = Path.GetTempFileName();
		try
		{
			MapDocument rootMap = TestData.CreateCityMap();
			var layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.PointsOfInterest);
			rootMap.AddLayer(layer);

			CampaignMapProject project = TestData.CreateProject(rootMap);
			layer.SetVisibility(false);

			await ProjectJsonSerializer.SaveAsync(project, tempFileName);
			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);

			var loadedLayer = loaded.RootMap.Layers.First(l => l.Id == layer.Id);
			Assert.False(loadedLayer.IsVisible);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task SaveAndLoadAsync_RoundTripsLayerLock()
	{
		string tempFileName = Path.GetTempFileName();
		try
		{
			MapDocument rootMap = TestData.CreateCityMap();
			var layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.PointsOfInterest);
			rootMap.AddLayer(layer);
			layer.SetLocked(true);

			CampaignMapProject project = TestData.CreateProject(rootMap);
			await ProjectJsonSerializer.SaveAsync(project, tempFileName);
			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);

			var loadedLayer = loaded.RootMap.Layers.First(l => l.Id == layer.Id);
			Assert.True(loadedLayer.IsLocked);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task SaveAndLoadAsync_RoundTripsLayerRename()
	{
		string tempFileName = Path.GetTempFileName();
		try
		{
			MapDocument rootMap = TestData.CreateCityMap();
			var layer = new MapLayer(Guid.NewGuid(), "Test Layer", MapLayerType.PointsOfInterest);
			rootMap.AddLayer(layer);
			layer.Rename("Renamed Layer");

			CampaignMapProject project = TestData.CreateProject(rootMap);
			await ProjectJsonSerializer.SaveAsync(project, tempFileName);
			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);

			var loadedLayer = loaded.RootMap.Layers.First(l => l.Id == layer.Id);
			Assert.Equal("Renamed Layer", loadedLayer.Name);
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
			}
		}
	}

	[Fact]
	public async Task SaveAndLoadAsync_RoundTripsLayerOrder()
	{
		string tempFileName = Path.GetTempFileName();
		try
		{
			MapDocument rootMap = TestData.CreateCityMap();
			var layer1 = new MapLayer(Guid.NewGuid(), "Layer 1", MapLayerType.PointsOfInterest);
			var layer2 = new MapLayer(Guid.NewGuid(), "Layer 2", MapLayerType.Streets);
			rootMap.AddLayer(layer1);
			rootMap.AddLayer(layer2);

			CampaignMapProject project = TestData.CreateProject(rootMap);
			
			// Initial order: layer1, layer2
			Assert.Equal(layer1.Id, rootMap.Layers[rootMap.Layers.Count - 2].Id);
			Assert.Equal(layer2.Id, rootMap.Layers[rootMap.Layers.Count - 1].Id);

			rootMap.MoveLayerDown(layer2.Id); // Move layer2 down (before layer1)
			
			await ProjectJsonSerializer.SaveAsync(project, tempFileName);
			CampaignMapProject loaded = await ProjectJsonSerializer.LoadAsync(tempFileName);

			var loadedLayers = loaded.RootMap.Layers;
			// Find our test layers in the loaded list
			int idx1 = loadedLayers.Select((l, i) => new { l, i }).First(x => x.l.Id == layer1.Id).i;
			int idx2 = loadedLayers.Select((l, i) => new { l, i }).First(x => x.l.Id == layer2.Id).i;

			Assert.Equal(idx1, idx2 + 1); // layer1 should be after layer2
		}
		finally
		{
			if (File.Exists(tempFileName))
			{
				File.Delete(tempFileName);
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
        new CreateChildMapCommand(project, rootMap, district, childMapId: childMapId).Execute();

        return (project, rootMap, childMapId);
    }
}
