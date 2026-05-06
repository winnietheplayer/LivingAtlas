using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Creation;

namespace LivingAtlas.Tests;

public sealed class RoadAreaDrawingSessionTests
{
	[Fact]
	public void Finish_RequiresAtLeastThreePoints()
	{
		RoadAreaDrawingSession session = new RoadAreaDrawingSession();
		session.AddPoint(new PointD(0.0, 0.0));
		session.AddPoint(new PointD(100.0, 0.0));

		Assert.False(session.CanFinish);
		Assert.Throws<InvalidOperationException>(() => session.Finish(TestData.CreateCityMap()));
	}

	[Fact]
	public void Finish_CreatesRoadAreaCommandAndClearsSession()
	{
		MapDocument map = TestData.CreateCityMap();
		MapLayer streets = TestData.CreateLayer(name: "Streets", layerType: MapLayerType.Streets);
		map.AddLayer(streets);
		RoadAreaDrawingSession session = new RoadAreaDrawingSession();
		session.AddPoint(new PointD(0.0, 0.0));
		session.AddPoint(new PointD(100.0, 0.0));
		session.AddPoint(new PointD(100.0, 30.0));

		var command = session.Finish(map, streets.Id);

		RoadArea roadArea = Assert.IsType<RoadArea>(command.MapObject);
		Assert.Equal(streets.Id, roadArea.LayerId);
		Assert.Equal(3, roadArea.PolygonPoints.Count);
		Assert.False(session.IsDrawing);
		Assert.Null(session.PreviewPoint);
	}

	[Fact]
	public void Cancel_ClearsPreviewAndPoints()
	{
		RoadAreaDrawingSession session = new RoadAreaDrawingSession();
		session.AddPoint(new PointD(0.0, 0.0));
		session.UpdatePreviewPoint(new PointD(10.0, 10.0));

		session.Cancel();

		Assert.Empty(session.Points);
		Assert.Null(session.PreviewPoint);
		Assert.False(session.IsDrawing);
	}
}
