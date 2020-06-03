using System;

[Serializable]
struct Point : IEquatable<Point>
{
    public int x;
    public int y;

    public Point(int xValue, int yValue)
    {
        x = xValue;
        y = yValue;
    }

    public static Point operator +(Point a, Point b)
    {
        return new Point(a.x + b.x, a.y + b.y);
    }

    public static Point operator -(Point a, Point b)
    {
        return new Point(a.x - b.x, a.y - b.y);
    }
    public static bool operator ==(Point a, Point b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Point a, Point b)
    {
        return a.x != b.x && a.y != b.y;
    }

    public bool Equals(Point other)
    {
        return x == other.x && y == other.y;
    }

    public override string ToString()
    {
        return x + ", " + y;
    }
}
