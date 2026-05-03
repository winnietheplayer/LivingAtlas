using System;
using System.Collections.Generic;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Domain.Maps.Objects;

public sealed class PointOfInterest : MapObject
{
	public PointD Position { get; private set; }

	public string IconKey { get; }

	public PointOfInterest(Guid id, string name, Guid layerId, PointD position, string iconKey, IEnumerable<string>? tags = null, string? styleKey = null)
		: base(id, name, MapObjectType.PointOfInterest, layerId, tags, styleKey)
	{
		if (string.IsNullOrWhiteSpace(iconKey))
		{
			throw new ArgumentException("Point of interest icon key cannot be empty.", "iconKey");
		}
		Position = position;
		IconKey = iconKey;
	}

	public void MoveBy(PointD delta)
	{
		Position = new PointD(Position.X + delta.X, Position.Y + delta.Y);
	}
}
