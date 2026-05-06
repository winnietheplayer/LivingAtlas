using LivingAtlas.Assets;
using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Tests;

public sealed class InspectorViewModelTests
{
	[Fact]
	public void SetSelection_ExposesOnlyCurrentTypeFields()
	{
		InspectorViewModel inspector = new InspectorViewModel();
		RoadLine road = TestData.CreateRoad(Guid.NewGuid());
		MapLabel label = TestData.CreateLabel(Guid.NewGuid());

		inspector.SetSelection(road);

		Assert.True(inspector.IsRoadLineSelected);
		Assert.False(inspector.IsRoadAreaSelected);
		Assert.True(inspector.IsRoadKindSelected);
		Assert.False(inspector.IsMapLabelSelected);
		Assert.False(inspector.IsPointOfInterestSelected);
		Assert.False(inspector.IsDistrictShapeSelected);
		Assert.False(inspector.IsTextureFillSelected);
		Assert.Equal(RoadLine.DefaultRoadKind, inspector.EditableRoadKind);
		Assert.Single(inspector.AvailableFillTextureAssets);

		inspector.SetSelection(label);

		Assert.True(inspector.IsMapLabelSelected);
		Assert.False(inspector.IsRoadLineSelected);
		Assert.False(inspector.IsRoadKindSelected);
		Assert.Equal(MapLabel.DefaultLabelKind, inspector.EditableLabelKind);

		inspector.SetSelection(null);

		Assert.False(inspector.HasSelection);
		Assert.Equal("No selection", inspector.ObjectTypeText);
		Assert.Equal("No selection", inspector.SelectionDetails);
	}

	[Fact]
	public void SetSelection_ExposesTextureFillFieldsForDistricts()
	{
		InspectorViewModel inspector = new InspectorViewModel(CreateTextureCatalog());
		DistrictShape district = TestData.CreateDistrict(Guid.NewGuid());
		district.SetTextureFill("ground.dirt.01", 12.5);

		inspector.SetSelection(district);

		Assert.True(inspector.IsDistrictShapeSelected);
		Assert.Equal("12.5", inspector.EditableTextureTileSizeMeters);
		Assert.Contains(inspector.AvailableFillTextureAssets, option => option.AssetId == null && option.DisplayName == "None");
		Assert.Contains(inspector.AvailableFillTextureAssets, option => option.AssetId == "ground.dirt.01");
		Assert.Equal("ground.dirt.01", inspector.SelectedFillTextureAsset?.AssetId);
	}

	[Fact]
	public void SetSelection_ExposesRoadKindAndTextureFillFieldsForRoadAreas()
	{
		InspectorViewModel inspector = new InspectorViewModel(CreateTextureCatalog());
		RoadArea roadArea = TestData.CreateRoadArea(Guid.NewGuid());
		roadArea.SetRoadKind("primary");
		roadArea.SetTextureFill("ground.dirt.01", 9.5);

		inspector.SetSelection(roadArea);

		Assert.True(inspector.IsRoadAreaSelected);
		Assert.True(inspector.IsRoadKindSelected);
		Assert.True(inspector.IsTextureFillSelected);
		Assert.False(inspector.IsRoadLineSelected);
		Assert.Equal("primary", inspector.EditableRoadKind);
		Assert.Equal("9.5", inspector.EditableTextureTileSizeMeters);
		Assert.Equal("ground.dirt.01", inspector.SelectedFillTextureAsset?.AssetId);
		Assert.Contains("Polygon points: 4", inspector.SelectionDetails);
	}

	[Fact]
	public void SetSelection_PreservesMissingTextureAssetSelection()
	{
		InspectorViewModel inspector = new InspectorViewModel(TextureAssetCatalog.Empty);
		DistrictShape district = TestData.CreateDistrict(Guid.NewGuid());
		district.SetTextureFill("ground.missing.01", 18.0);

		inspector.SetSelection(district);

		Assert.Equal("ground.missing.01", inspector.SelectedFillTextureAsset?.AssetId);
		Assert.Equal("Missing: ground.missing.01", inspector.SelectedFillTextureAsset?.DisplayName);
	}

	[Fact]
	public void ApplyInspectorChanges_NoChangesDoesNotCreateCommandOrDirtyProject()
	{
		(MainWindowViewModel viewModel, PointOfInterest poi) = CreateMainWindowWithSelectedPoi();

		Assert.Same(poi, viewModel.MapViewport.SelectedObject);

		Assert.False(viewModel.ApplyInspectorChanges());

		Assert.False(viewModel.MapViewport.History.CanUndo);
		Assert.False(viewModel.IsDirty);
		Assert.Equal("No inspector changes", viewModel.StatusBar.Message);
	}

	[Fact]
	public void ApplyInspectorChanges_InvalidEmptyNameIsRejected()
	{
		(MainWindowViewModel viewModel, PointOfInterest poi) = CreateMainWindowWithSelectedPoi();

		viewModel.Inspector.EditableName = " ";

		Assert.False(viewModel.ApplyInspectorChanges());

		Assert.Equal("Gate", poi.Name);
		Assert.False(viewModel.MapViewport.History.CanUndo);
		Assert.False(viewModel.IsDirty);
		Assert.StartsWith("Inspector apply failed:", viewModel.StatusBar.Message);
	}

	[Fact]
	public void ApplyInspectorChanges_UpdatesDistrictTextureFill()
	{
		(MainWindowViewModel viewModel, DistrictShape district) = CreateMainWindowWithSelectedDistrict();
		viewModel.Inspector.SelectedFillTextureAsset = new TextureAssetOptionViewModel("Dirt 01", "ground.dirt.01", 10.0);
		viewModel.Inspector.EditableTextureTileSizeMeters = "16.5";

		Assert.True(viewModel.ApplyInspectorChanges());

		Assert.Equal("ground.dirt.01", district.FillTextureAssetId);
		Assert.Equal(16.5, district.TextureTileSizeMeters);
		Assert.True(viewModel.MapViewport.History.CanUndo);
		Assert.True(viewModel.IsDirty);
	}

	[Fact]
	public void ApplyInspectorChanges_UpdatesRoadAreaRoadKindAndTextureFill()
	{
		(MainWindowViewModel viewModel, RoadArea roadArea) = CreateMainWindowWithSelectedRoadArea();
		viewModel.Inspector.EditableRoadKind = "primary";
		viewModel.Inspector.SelectedFillTextureAsset = new TextureAssetOptionViewModel("Dirt 01", "ground.dirt.01", 10.0);
		viewModel.Inspector.EditableTextureTileSizeMeters = "8.25";

		Assert.True(viewModel.ApplyInspectorChanges());

		Assert.Equal("primary", roadArea.RoadKind);
		Assert.Equal("ground.dirt.01", roadArea.FillTextureAssetId);
		Assert.Equal(8.25, roadArea.TextureTileSizeMeters);
		Assert.True(viewModel.MapViewport.History.CanUndo);
		Assert.True(viewModel.IsDirty);
	}

	[Fact]
	public void ApplyInspectorChanges_InvalidTextureTileSizeIsRejected()
	{
		(MainWindowViewModel viewModel, DistrictShape district) = CreateMainWindowWithSelectedDistrict();
		viewModel.Inspector.SelectedFillTextureAsset = new TextureAssetOptionViewModel("Dirt 01", "ground.dirt.01", 10.0);
		viewModel.Inspector.EditableTextureTileSizeMeters = "0";

		Assert.False(viewModel.ApplyInspectorChanges());

		Assert.Null(district.FillTextureAssetId);
		Assert.False(viewModel.MapViewport.History.CanUndo);
		Assert.False(viewModel.IsDirty);
		Assert.Equal("Inspector apply failed: texture tile size must be positive.", viewModel.StatusBar.Message);
	}

	[Fact]
	public void ApplyInspectorChanges_InvalidRoadAreaTextureTileSizeIsRejected()
	{
		(MainWindowViewModel viewModel, RoadArea roadArea) = CreateMainWindowWithSelectedRoadArea();
		viewModel.Inspector.SelectedFillTextureAsset = new TextureAssetOptionViewModel("Dirt 01", "ground.dirt.01", 10.0);
		viewModel.Inspector.EditableTextureTileSizeMeters = "-1";

		Assert.False(viewModel.ApplyInspectorChanges());

		Assert.Null(roadArea.FillTextureAssetId);
		Assert.False(viewModel.MapViewport.History.CanUndo);
		Assert.False(viewModel.IsDirty);
		Assert.Equal("Inspector apply failed: texture tile size must be positive.", viewModel.StatusBar.Message);
	}

	private static (MainWindowViewModel ViewModel, PointOfInterest Poi) CreateMainWindowWithSelectedPoi()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.PointsOfInterest);
		PointOfInterest poi = TestData.CreatePointOfInterest(layer.Id);
		layer.AddObject(poi);
		map.AddLayer(layer);
		CampaignMapProject project = TestData.CreateProject(map);
		MainWindowViewModel viewModel = new MainWindowViewModel(project);

		viewModel.MapViewport.Camera.SetView(0.0, 0.0, 1.0);
		viewModel.MapViewport.SelectAtScreenPoint(new PointD(100.0, 200.0), 20.0);

		return (viewModel, poi);
	}

	private static (MainWindowViewModel ViewModel, DistrictShape District) CreateMainWindowWithSelectedDistrict()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Districts);
		DistrictShape district = TestData.CreateDistrict(layer.Id);
		layer.AddObject(district);
		map.AddLayer(layer);
		CampaignMapProject project = TestData.CreateProject(map);
		MainWindowViewModel viewModel = new MainWindowViewModel(project, CreateTextureCatalog());

		viewModel.MapViewport.Camera.SetView(0.0, 0.0, 1.0);
		viewModel.MapViewport.SelectAtScreenPoint(new PointD(200.0, 200.0), 20.0);

		return (viewModel, district);
	}

	private static (MainWindowViewModel ViewModel, RoadArea RoadArea) CreateMainWindowWithSelectedRoadArea()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer layer = TestData.CreateLayer(layerType: MapLayerType.Streets);
		RoadArea roadArea = TestData.CreateRoadArea(layer.Id);
		layer.AddObject(roadArea);
		map.AddLayer(layer);
		CampaignMapProject project = TestData.CreateProject(map);
		MainWindowViewModel viewModel = new MainWindowViewModel(project, CreateTextureCatalog());

		viewModel.MapViewport.Camera.SetView(0.0, 0.0, 1.0);
		viewModel.MapViewport.SelectAtScreenPoint(new PointD(30.0, 30.0), 20.0);

		return (viewModel, roadArea);
	}

	private static TextureAssetCatalog CreateTextureCatalog()
	{
		return new TextureAssetCatalog(new[]
		{
			new TextureAssetDefinition(
				"base_terrain_01",
				"ground.dirt.01",
				"Dirt 01",
				"texture",
				"ground",
				"textures/ground/ground_dirt_01.png",
				@"C:\textures\ground_dirt_01.png",
				IsTileable: true,
				DefaultTileSizeMeters: 10.0,
				new[] { "ground", "dirt" },
				FileExists: false)
		});
	}
}
