using System;
using System.Collections.Generic;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Domain.Maps.Objects;

public sealed class MapLabel : MapObject
{
	public PointD Position { get; private set; }

	public string Text { get; private set; }

	public MapLabel(Guid id, string name, Guid layerId, PointD position, string text, IEnumerable<string>? tags = null, string? styleKey = null)
		: base(id, name, MapObjectType.MapLabel, layerId, tags, styleKey)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new ArgumentException("Label text cannot be empty.", "text");
		}
		Position = position;
		Text = text;
	}

	public void SetText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new ArgumentException("Label text cannot be empty.", "text");
		}
		Text = text.Trim();
	}

	public void MoveBy(PointD delta)
	{
		Position = new PointD(Position.X + delta.X, Position.Y + delta.Y);
	}
}
