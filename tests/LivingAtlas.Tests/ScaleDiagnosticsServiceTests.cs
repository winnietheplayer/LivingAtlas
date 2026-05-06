using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Hierarchy;

namespace LivingAtlas.Tests;

public sealed class ScaleDiagnosticsServiceTests
{
	[Fact]
	public void GetMapDiagnostics_ComputesRepresentedPhysicalSize()
	{
		MapDocument map = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(50.0, 40.0));

		MapScaleDiagnostics diagnostics = ScaleDiagnosticsService.GetMapDiagnostics(map);

		Assert.Equal(MapScaleType.City, diagnostics.ScaleType);
		Assert.Equal(100.0, diagnostics.FeetPerUnit);
		Assert.Equal(new SizeD(5000.0, 4000.0), diagnostics.RepresentedPhysicalSizeFeet);
	}

	[Fact]
	public void GetMapDiagnostics_ComputesGridPhysicalSize()
	{
		MapDocument map = new MapDocument(
			Guid.NewGuid(),
			"District",
			MapScaleType.District,
			new SizeD(80.0, 60.0),
			gridSettings: new GridSettings(isEnabled: true, cellSizeMeters: 2.0, showGrid: true, snapToGrid: true));

		MapScaleDiagnostics diagnostics = ScaleDiagnosticsService.GetMapDiagnostics(map);

		Assert.Equal(2.0, diagnostics.GridCellLocalUnits);
		Assert.Equal(20.0, diagnostics.GridCellFeet);
	}

	[Fact]
	public void GetChildMapDiagnostics_ComputesExpectedChildLocalSize()
	{
		(CampaignMapProject project, MapDocument childMap) = CreateCityToDistrictProject(new SizeD(500.0, 400.0));

		ChildMapScaleDiagnostics? diagnostics = ScaleDiagnosticsService.GetChildMapDiagnostics(project, childMap);

		Assert.NotNull(diagnostics);
		Assert.Equal(new SizeD(50.0, 40.0), diagnostics.ParentFootprintLocalSize);
		Assert.Equal(new SizeD(5000.0, 4000.0), diagnostics.ParentFootprintPhysicalSizeFeet);
		Assert.Equal(new SizeD(500.0, 400.0), diagnostics.ExpectedChildLocalSize);
		Assert.False(diagnostics.HasMismatch);
	}

	[Fact]
	public void GetChildMapDiagnostics_DetectsMismatch()
	{
		(CampaignMapProject project, MapDocument childMap) = CreateCityToDistrictProject(new SizeD(480.0, 400.0));

		ChildMapScaleDiagnostics? diagnostics = ScaleDiagnosticsService.GetChildMapDiagnostics(project, childMap);

		Assert.NotNull(diagnostics);
		Assert.True(diagnostics.HasMismatch);
		Assert.NotNull(diagnostics.Warning);
	}

	[Fact]
	public void GetChildMapDiagnostics_UsesBattleDistrictCityDefaults()
	{
		Assert.Equal(5.0, MapDocument.GetDefaultFeetPerUnit(MapScaleType.BattleMap));
		Assert.Equal(10.0, MapDocument.GetDefaultFeetPerUnit(MapScaleType.District));
		Assert.Equal(100.0, MapDocument.GetDefaultFeetPerUnit(MapScaleType.City));
	}

	private static (CampaignMapProject Project, MapDocument ChildMap) CreateCityToDistrictProject(SizeD childSize)
	{
		Guid childMapId = Guid.NewGuid();
		MapDocument parentMap = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(100.0, 100.0));
		MapDocument childMap = new MapDocument(childMapId, "District", MapScaleType.District, childSize, parentMap.Id);
		MapLayer districtLayer = new MapLayer(Guid.NewGuid(), "Districts", MapLayerType.Districts);
		districtLayer.AddObject(new DistrictShape(
			Guid.NewGuid(),
			"Linked District",
			districtLayer.Id,
			new[]
			{
				new PointD(10.0, 20.0),
				new PointD(60.0, 20.0),
				new PointD(60.0, 60.0),
				new PointD(10.0, 60.0)
			},
			childMapId: childMapId));
		parentMap.AddLayer(districtLayer);
		parentMap.AddChildMapId(childMapId);
		CampaignMapProject project = new CampaignMapProject(Guid.NewGuid(), "Project", parentMap.Id, new[] { parentMap, childMap });
		return (project, childMap);
	}
}
