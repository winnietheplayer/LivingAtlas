using System;
using LivingAtlas.Domain.Geometry;

namespace LivingAtlas.Editor.Hierarchy;

public static class ChildMapPreviewTransform
{
	public static PointD ChildToParent(PointD childPoint, RectD parentBounds, SizeD childSize)
	{
		ValidateChildSize(childSize);
		double num = childPoint.X / childSize.Width;
		double num2 = childPoint.Y / childSize.Height;
		return new PointD(parentBounds.Left + num * parentBounds.Size.Width, parentBounds.Top + num2 * parentBounds.Size.Height);
	}

	public static PointD ParentToChild(PointD parentPoint, RectD parentBounds, SizeD childSize)
	{
		ValidateChildSize(childSize);
		if (parentBounds.Size.Width <= 0.0)
		{
			throw new ArgumentOutOfRangeException("parentBounds", parentBounds, "Parent bounds width must be positive.");
		}
		if (parentBounds.Size.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("parentBounds", parentBounds, "Parent bounds height must be positive.");
		}
		double num = (parentPoint.X - parentBounds.Left) / parentBounds.Size.Width;
		double num2 = (parentPoint.Y - parentBounds.Top) / parentBounds.Size.Height;
		return new PointD(num * childSize.Width, num2 * childSize.Height);
	}

	private static void ValidateChildSize(SizeD childSize)
	{
		if (childSize.Width <= 0.0)
		{
			throw new ArgumentOutOfRangeException("childSize", childSize, "Child map width must be positive.");
		}
		if (childSize.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("childSize", childSize, "Child map height must be positive.");
		}
	}
}
