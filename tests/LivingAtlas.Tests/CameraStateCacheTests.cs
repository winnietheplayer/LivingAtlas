using System;
using LivingAtlas.Editor.Viewport;
using Xunit;

namespace LivingAtlas.Tests;

public class CameraStateCacheTests
{
    [Fact]
    public void Save_And_TryRestore_RoundTripsState()
    {
        var cache = new CameraStateCache();
        var camera = new Camera2D(100, 200, 2.5);
        var mapId = Guid.NewGuid();

        cache.Save(mapId, camera);

        var restoreCamera = new Camera2D();
        bool restored = cache.TryRestore(mapId, restoreCamera);

        Assert.True(restored);
        Assert.Equal(100, restoreCamera.OffsetX);
        Assert.Equal(200, restoreCamera.OffsetY);
        Assert.Equal(2.5, restoreCamera.Zoom);
    }

    [Fact]
    public void TryRestore_UnknownMapId_ReturnsFalse()
    {
        var cache = new CameraStateCache();
        var camera = new Camera2D();
        var unknownId = Guid.NewGuid();

        bool restored = cache.TryRestore(unknownId, camera);

        Assert.False(restored);
        Assert.Equal(0, camera.OffsetX);
        Assert.Equal(0, camera.OffsetY);
        Assert.Equal(1.0, camera.Zoom);
    }

    [Fact]
    public void Clear_RemovesAllStates()
    {
        var cache = new CameraStateCache();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        cache.Save(id1, new Camera2D(10, 20, 1.5));
        cache.Save(id2, new Camera2D(30, 40, 3.0));
        Assert.Equal(2, cache.Count);

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryRestore(id1, new Camera2D()));
        Assert.False(cache.TryRestore(id2, new Camera2D()));
    }

    [Fact]
    public void Save_OverwritesPreviousState()
    {
        var cache = new CameraStateCache();
        var mapId = Guid.NewGuid();

        cache.Save(mapId, new Camera2D(10, 20, 1.0));
        cache.Save(mapId, new Camera2D(50, 60, 4.0));

        var camera = new Camera2D();
        cache.TryRestore(mapId, camera);

        Assert.Equal(50, camera.OffsetX);
        Assert.Equal(60, camera.OffsetY);
        Assert.Equal(4.0, camera.Zoom);
    }

    [Fact]
    public void Get_ReturnsStateForKnownMap()
    {
        var cache = new CameraStateCache();
        var mapId = Guid.NewGuid();

        cache.Save(mapId, new Camera2D(15, 25, 2.0));

        var state = cache.Get(mapId);

        Assert.NotNull(state);
        Assert.Equal(15, state.OffsetX);
        Assert.Equal(25, state.OffsetY);
        Assert.Equal(2.0, state.Zoom);
    }

    [Fact]
    public void Get_ReturnsNullForUnknownMap()
    {
        var cache = new CameraStateCache();

        var state = cache.Get(Guid.NewGuid());

        Assert.Null(state);
    }
}
