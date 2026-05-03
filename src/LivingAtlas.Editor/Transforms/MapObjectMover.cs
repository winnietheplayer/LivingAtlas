using System;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Transforms;

public static class MapObjectMover
{
	public static void MoveBy(MapObject mapObject, PointD delta)
	{
		ArgumentNullException.ThrowIfNull(mapObject, "mapObject");
		if (!(mapObject is DistrictShape districtShape))
		{
			if (!(mapObject is RoadLine roadLine))
			{
				if (!(mapObject is PointOfInterest pointOfInterest))
				{
					if (!(mapObject is MapLabel mapLabel))
					{
						throw new NotSupportedException("Unsupported map object type '" + mapObject.GetType().Name + "'.");
					}
					mapLabel.MoveBy(delta);
				}
				else
				{
					pointOfInterest.MoveBy(delta);
				}
			}
			else
			{
				roadLine.MoveBy(delta);
			}
		}
		else
		{
			districtShape.MoveBy(delta);
		}
	}
}
