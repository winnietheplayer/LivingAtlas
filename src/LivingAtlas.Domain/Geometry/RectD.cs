namespace LivingAtlas.Domain.Geometry;

public readonly record struct RectD(PointD Origin, SizeD Size)
{
	public double Left => Origin.X;

	public double Top => Origin.Y;

	public double Right => Origin.X + Size.Width;

	public double Bottom => Origin.Y + Size.Height;

	public RectD(double x, double y, double width, double height)
		: this(new PointD(x, y), new SizeD(width, height))
	{
	}

	public bool Contains(PointD point)
	{
		return point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
	}
}
