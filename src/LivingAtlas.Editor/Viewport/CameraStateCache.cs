using System;
using System.Collections.Generic;

namespace LivingAtlas.Editor.Viewport;

/// <summary>
/// In-memory cache of camera states keyed by MapDocument.Id.
/// Camera states are session-only and are not persisted to JSON.
/// </summary>
public sealed class CameraStateCache
{
    private readonly Dictionary<Guid, CameraState> _states = new();

    /// <summary>
    /// Saves the current camera state for the given map.
    /// </summary>
    public void Save(Guid mapId, Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera, nameof(camera));
        _states[mapId] = new CameraState(camera.OffsetX, camera.OffsetY, camera.Zoom);
    }

    /// <summary>
    /// Tries to restore a previously saved camera state for the given map.
    /// Returns true if a state was found and restored; false otherwise.
    /// </summary>
    public bool TryRestore(Guid mapId, Camera2D camera)
    {
        ArgumentNullException.ThrowIfNull(camera, nameof(camera));
        if (_states.TryGetValue(mapId, out var state))
        {
            camera.SetView(state.OffsetX, state.OffsetY, state.Zoom);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the saved camera state for the given map, or null if none exists.
    /// </summary>
    public CameraState? Get(Guid mapId)
    {
        return _states.TryGetValue(mapId, out var state) ? state : null;
    }

    /// <summary>
    /// Removes all saved camera states.
    /// </summary>
    public void Clear()
    {
        _states.Clear();
    }

    /// <summary>
    /// Returns the number of saved camera states.
    /// </summary>
    public int Count => _states.Count;
}
