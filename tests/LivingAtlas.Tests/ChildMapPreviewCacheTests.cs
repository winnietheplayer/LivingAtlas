using LivingAtlas.Assets;
using LivingAtlas.Desktop.Services;
using LivingAtlas.Desktop.ViewModels;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Tests;

public sealed class ChildMapPreviewCacheTests
{
	[Fact]
	public void GetOrCreate_ReturnsSameEntryForUnchangedChildMap()
	{
		(CampaignMapProject project, MapDocument childMap) = CreateProjectWithChildMap(new SizeD(100.0, 80.0));
		using var cache = new ChildMapPreviewCache();

		ChildMapPreviewCacheEntry? first = cache.GetOrCreate(project, childMap.Id, TextureAssetCatalog.Empty);
		ChildMapPreviewCacheEntry? second = cache.GetOrCreate(project, childMap.Id, TextureAssetCatalog.Empty);

		Assert.NotNull(first);
		Assert.Same(first, second);
	}

	[Fact]
	public void Invalidate_RemovesCachedEntry()
	{
		(CampaignMapProject project, MapDocument childMap) = CreateProjectWithChildMap(new SizeD(100.0, 80.0));
		using var cache = new ChildMapPreviewCache();

		ChildMapPreviewCacheEntry? first = cache.GetOrCreate(project, childMap.Id, TextureAssetCatalog.Empty);
		cache.Invalidate(childMap.Id);
		ChildMapPreviewCacheEntry? second = cache.GetOrCreate(project, childMap.Id, TextureAssetCatalog.Empty);

		Assert.NotNull(first);
		Assert.NotNull(second);
		Assert.NotSame(first, second);
	}

	[Fact]
	public void GetOrCreate_RespectsMaxPreviewLongSide()
	{
		(CampaignMapProject project, MapDocument childMap) = CreateProjectWithChildMap(new SizeD(1000.0, 500.0));
		using var cache = new ChildMapPreviewCache();

		ChildMapPreviewCacheEntry? entry = cache.GetOrCreate(project, childMap.Id, TextureAssetCatalog.Empty);

		Assert.NotNull(entry);
		Assert.Equal(ChildMapPreviewCache.DefaultMaxPreviewLongSidePixels, entry.PixelSize.Width);
		Assert.Equal(256, entry.PixelSize.Height);
		Assert.NotEmpty(entry.PngBytes);
	}

	[Fact]
	public void GetOrCreate_MissingChildMapReturnsNull()
	{
		MapDocument rootMap = TestData.CreateCityMap();
		CampaignMapProject project = TestData.CreateProject(rootMap);
		using var cache = new ChildMapPreviewCache();

		Assert.Null(cache.GetOrCreate(project, Guid.NewGuid(), TextureAssetCatalog.Empty));
	}

	[Fact]
	public void MapViewportViewModel_DisablesChildMapPreviewsByDefault()
	{
		MapDocument map = TestData.CreateCityMap();

		var viewModel = new MapViewportViewModel(map, TestData.CreateProject(map), TextureAssetCatalog.Empty);

		Assert.False(viewModel.ShowChildMapPreviews);
	}

	private static (CampaignMapProject Project, MapDocument ChildMap) CreateProjectWithChildMap(SizeD childSize)
	{
		MapDocument rootMap = TestData.CreateCityMap();
		MapDocument childMap = new MapDocument(Guid.NewGuid(), "Child", MapScaleType.District, childSize, rootMap.Id);
		MapLayer layer = new MapLayer(Guid.NewGuid(), "Roads", MapLayerType.Streets);
		layer.AddObject(new RoadLine(
			Guid.NewGuid(),
			"Preview Road",
			layer.Id,
			new[]
			{
				new PointD(5.0, 5.0),
				new PointD(Math.Max(6.0, childSize.Width - 5.0), Math.Max(6.0, childSize.Height - 5.0))
			}));
		childMap.AddLayer(layer);
		rootMap.AddChildMapId(childMap.Id);
		CampaignMapProject project = new CampaignMapProject(Guid.NewGuid(), "Preview Project", rootMap.Id, new[] { rootMap, childMap });
		return (project, childMap);
	}
}
