using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Tests;

public sealed class CreateChildMapViewModelTests
{
	[Fact]
	public void CreateChildMapViewModel_ComputesChildSizeFromPhysicalFootprint()
	{
		MapDocument parentMap = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(1000.0, 1000.0));
		DistrictShape district = CreateDistrict(new SizeD(50.0, 40.0));

		CreateChildMapViewModel viewModel = new CreateChildMapViewModel(district, parentMap);

		Assert.Equal(MapScaleType.District, viewModel.ScaleType);
		Assert.Equal(10.0, viewModel.FeetPerUnit);
		Assert.Equal(500.0, viewModel.Width);
		Assert.Equal(400.0, viewModel.Height);
		Assert.Equal(500.0, viewModel.ComputedChildWidth);
		Assert.Equal(400.0, viewModel.ComputedChildHeight);
	}

	[Fact]
	public void CreateChildMapViewModel_ScaleTypeChangeUpdatesFeetPerUnitAndComputedSize()
	{
		MapDocument parentMap = new MapDocument(Guid.NewGuid(), "City", MapScaleType.City, new SizeD(1000.0, 1000.0));
		DistrictShape district = CreateDistrict(new SizeD(50.0, 40.0));
		CreateChildMapViewModel viewModel = new CreateChildMapViewModel(district, parentMap);

		viewModel.ScaleType = MapScaleType.BattleMap;

		Assert.Equal(5.0, viewModel.FeetPerUnit);
		Assert.Equal(1000.0, viewModel.Width);
		Assert.Equal(800.0, viewModel.Height);
	}

	private static DistrictShape CreateDistrict(SizeD size)
	{
		return new DistrictShape(
			Guid.NewGuid(),
			"District",
			Guid.NewGuid(),
			new[]
			{
				new PointD(10.0, 20.0),
				new PointD(10.0 + size.Width, 20.0),
				new PointD(10.0 + size.Width, 20.0 + size.Height),
				new PointD(10.0, 20.0 + size.Height)
			});
	}
}
