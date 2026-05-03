namespace LivingAtlas.Editor.Viewport;

/// <summary>
/// Immutable snapshot of a Camera2D's position and zoom level.
/// </summary>
public sealed record CameraState(double OffsetX, double OffsetY, double Zoom);
