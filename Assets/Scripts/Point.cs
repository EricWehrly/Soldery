using System;
using System.Collections.Generic;
using System.Linq;
[Serializable]
struct Point
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

    public override string ToString()
    {
        return x + ", " + y;
    }
}
