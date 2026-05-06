using System;
using System.Collections.Generic;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Editor.Hierarchy;

public sealed record ParentRoadOverlay(
	Guid SourceRoadAreaId,
	Guid SourceLayerId,
	string Name,
	IReadOnlyList<PointD> ProjectedPolygonPoints,
	string StyleKey,
	string RoadKind,
	string? FillTextureAssetId,
	double TextureTileSizeMeters);
