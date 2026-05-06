using LivingAtlas.Assets;
using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Tests;

public sealed class MapSettingsViewModelTests
{
	[Fact]
	public void EditMapSettingsViewModel_ComputesRepresentedPhysicalSize()
	{
		MapDocument map = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(50.0, 40.0));
		EditMapSettingsViewModel viewModel = new EditMapSettingsViewModel(map);

		Assert.Equal(50.0, viewModel.Width);
		Assert.Equal(40.0, viewModel.Height);
		Assert.Equal(100.0, viewModel.FeetPerUnit);
		Assert.Equal(5000.0, viewModel.RepresentedWidthFeet);
		Assert.Equal(4000.0, viewModel.RepresentedHeightFeet);
		Assert.Equal("1 units = 100 ft", viewModel.GridPhysicalSizeText);
		Assert.Equal("50 × 40 units", viewModel.LocalSizeText);
		Assert.Equal("1 unit = 100 ft", viewModel.ScaleText);
		Assert.Equal("5000 × 4000 ft", viewModel.RepresentedSizeText);
	}

	[Fact]
	public void EditMapSettingsViewModel_ScaleTypeChangeUsesDefaultFeetPerUnit()
	{
		MapDocument map = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(50.0, 40.0));
		EditMapSettingsViewModel viewModel = new EditMapSettingsViewModel(map);

		viewModel.ScaleType = MapScaleType.District;

		Assert.Equal(10.0, viewModel.FeetPerUnit);
		Assert.Equal(500.0, viewModel.RepresentedWidthFeet);
		Assert.Equal(400.0, viewModel.RepresentedHeightFeet);
	}

	[Fact]
	public void EditMapSettingsViewModel_IsInvalid_WhenFeetPerUnitIsNotPositive()
	{
		MapDocument map = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(50.0, 40.0));
		EditMapSettingsViewModel viewModel = new EditMapSettingsViewModel(map);

		viewModel.FeetPerUnit = 0.0;

		Assert.False(viewModel.IsValid());
		Assert.False(viewModel.CanApply);
	}

	[Fact]
	public void MainWindowViewModel_UpdateMapSettings_DoesNotDirtyWhenSettingsAreUnchanged()
	{
		MapDocument map = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(50.0, 40.0));
		CampaignMapProject project = TestData.CreateProject(map);
		MainWindowViewModel viewModel = new MainWindowViewModel(project, TextureAssetCatalog.Empty);
		EditMapSettingsViewModel settings = new EditMapSettingsViewModel(map);

		viewModel.UpdateMapSettings(map.Id, settings);

		Assert.False(viewModel.IsDirty);
	}

	[Fact]
	public void MainWindowViewModel_UpdateMapSettings_AppliesFeetPerUnitAndMarksDirty()
	{
		MapDocument map = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(50.0, 40.0));
		CampaignMapProject project = TestData.CreateProject(map);
		MainWindowViewModel viewModel = new MainWindowViewModel(project, TextureAssetCatalog.Empty);
		EditMapSettingsViewModel settings = new EditMapSettingsViewModel(map)
		{
			FeetPerUnit = 75.0
		};

		viewModel.UpdateMapSettings(map.Id, settings);

		Assert.Equal(75.0, map.FeetPerUnit);
		Assert.True(viewModel.IsDirty);
	}

	[Fact]
	public void EditMapSettingsViewModel_ShowsChildScaleDiagnostics()
	{
		(CampaignMapProject project, MapDocument childMap) = CreateCityToDistrictProject(new SizeD(500.0, 400.0));
		EditMapSettingsViewModel viewModel = new EditMapSettingsViewModel(childMap, project);

		Assert.True(viewModel.HasChildScaleDiagnostics);
		Assert.False(viewModel.HasChildScaleWarning);
		Assert.Contains("expected child 500x400 units", viewModel.ChildScaleDiagnosticsText);

		viewModel.Width = 480.0;

		Assert.True(viewModel.HasChildScaleWarning);
		Assert.Contains("expected 500x400 units, actual 480x400", viewModel.ChildScaleWarningText);
	}

	private static (CampaignMapProject Project, MapDocument ChildMap) CreateCityToDistrictProject(SizeD childSize)
	{
		Guid childMapId = Guid.NewGuid();
		MapDocument parentMap = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(100.0, 100.0));
		MapDocument childMap = new MapDocument(childMapId, "District", MapScaleType.District, childSize, parentMap.Id);
		MapLayer layer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
		layer.AddObject(new DistrictShape(
			Guid.NewGuid(),
			"Linked District",
			layer.Id,
			new[]
			{
				new PointD(10.0, 20.0),
				new PointD(60.0, 20.0),
				new PointD(60.0, 60.0),
				new PointD(10.0, 60.0)
			},
			childMapId: childMapId));
		parentMap.AddLayer(layer);
		parentMap.AddChildMapId(childMapId);
		return (new CampaignMapProject(Guid.NewGuid(), "Project", parentMap.Id, new[] { parentMap, childMap }), childMap);
	}
}
