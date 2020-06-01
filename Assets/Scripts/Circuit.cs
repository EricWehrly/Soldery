using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Circuit
{
    private static readonly object syncLock = new object();

    private static Transform _mainBoard;
    private static GameObject _original;
    private static List<Circuit> Circuits = new List<Circuit>();
    private static int circuitNumber = 0;

    public readonly float Distance;
    public readonly Transform Origin;
    public readonly Transform Destination;
    private LineRenderer lineRenderer;

    public bool Rendered { get; private set; }

    static Circuit()
    {
        _mainBoard = GameObject.Find("Main Board").transform;
        _original = Resources.Load<GameObject>("Circuit");

        // Because Unity is being a pain:
        Application.targetFrameRate = 60;
        // Debug.Log("Target frame rate: " + Application.targetFrameRate);
        // Debug.Log("vSync: " + QualitySettings.vSyncCount);
    }

    public Circuit(Transform origin, Transform destination)
    {
        Origin = origin;
        Destination = destination;

        Distance = Vector2.Distance(destination.localPosition, origin.localPosition);
        Circuits.Add(this);
    }

    public static void RenderCircuits()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        lock (syncLock)
        {
            var orderedList = Circuits.OrderByDescending(circuit => circuit.Distance);

            foreach (Circuit circuit in orderedList)
            {
                if (!circuit.Rendered) circuit.Render();
            }
        }
        var elapsedMs = watch.ElapsedMilliseconds;
        if(elapsedMs != 0) Debug.Log("Circuits took " + elapsedMs + "ms to render.");
    }

    public void Render()
    {
        var lines = GameObject.Instantiate(_original, _mainBoard);
        lines.name = "Circuit " + circuitNumber++;
        lineRenderer = lines.GetComponent<LineRenderer>();

        var gridPosition = CollisionMatrix.getPositionInCollisionMatrix(Origin.position);
        var gridDestination = CollisionMatrix.getPositionInCollisionMatrix(Destination.position);

        Vector3 originPosition = getLocalPosition(Origin);
        Vector3 destinationPosition = getLocalPosition(Destination);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, originPosition);

        // move 'forward' from pin, until you hit something
        var direction = new Point(-1, 0);
        var nextGridPoint = getNextGridPosition(
            new Point(gridPosition.Item1, gridPosition.Item2),
            direction, gridDestination);
        var nextPosition = CollisionMatrix.convertGridSpaceToObjectSpace(nextGridPoint.x, nextGridPoint.y);
        nextPosition.z = originPosition.z;
        if (nextGridPoint.x == gridDestination.Item1) nextPosition.x = destinationPosition.x;
        addLineRendererPoint(lineRenderer, nextPosition);
        CollisionMatrix.drawRayToCollisionMatrixPoint((nextGridPoint.x, nextGridPoint.y));

        // then turn, heading toward our destination
        addNextLinePoint(gridDestination, destinationPosition, direction, nextGridPoint, nextPosition);

        // addLineRendererPoint(lineRenderer, getLocalPosition(Destination));

        Rendered = true;
    }

    int depth = 0;

    // should this be 'continue to destination'?
    private void addNextLinePoint((int, int) gridDestination, Vector3 destinationPosition, 
        Point prevDirection, Point prevGridPoint, Vector3 prevPosition)
    {
        depth++;
        var direction = new Point(-1, 0);
        if (prevDirection.x == -1) direction = new Point(0, 1);

        var nextPoint = getNextGridPosition(prevGridPoint, direction, gridDestination);
        var nextPosition = CollisionMatrix.convertGridSpaceToObjectSpace(nextPoint.x, nextPoint.y);
        nextPosition.y = destinationPosition.y;
        if(prevDirection.y == 0) nextPosition.x = prevPosition.x;
        else nextPosition.z = prevPosition.z;
        if (nextPoint.x == gridDestination.Item1) nextPosition.x = destinationPosition.x;
        else if (nextPoint.y == gridDestination.Item2) nextPosition.z = destinationPosition.z;
        addLineRendererPoint(lineRenderer, nextPosition);
        // CollisionMatrix.drawRayToCollisionMatrixPoint((nextPoint.x, nextPoint.y));

        if(nextPosition == prevPosition)
        {
            Debug.Log("Repeated points. There's a problem.");
            return;
        }

        if(nextPosition != destinationPosition)
        {
            addNextLinePoint(gridDestination, destinationPosition, direction, nextPoint, nextPosition);
        }
    }

    private void addLineRendererPoint(LineRenderer lineRenderer, Vector3 point)
    {
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
    }

    private Point getNextGridPosition(Point currentPoint, Point direction, (int, int) gridDestination)
    {
        currentPoint = currentPoint + direction;

        // TODO: extract collision block to separate method ...
        while (CollisionMatrix.InBounds(currentPoint.x, currentPoint.y)
            && (CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == null
            || CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == Origin.gameObject)
            || CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == lineRenderer.gameObject)
        {

            if (direction.x > 0 && currentPoint.x >= gridDestination.Item1
            || direction.x < 0 && currentPoint.x <= gridDestination.Item1
            || direction.y > 0 && currentPoint.y >= gridDestination.Item2
            || direction.y < 0 && currentPoint.y <= gridDestination.Item2) break;

            CollisionMatrix.matrix[currentPoint.x, currentPoint.y] = lineRenderer.gameObject;
            currentPoint = currentPoint + direction;
        }
        // this is messy but whatever ...
        if (CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == Destination.gameObject)
        {
            return new Point(gridDestination.Item1, gridDestination.Item2);
        }
        /*
        if (CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == null)
        {
            Debug.Log("Hit edge of map at " + currentPoint);
        }
        else
        {
            Debug.Log("Hit " + CollisionMatrix.matrix[currentPoint.x, currentPoint.y].name + " at " + currentPoint);
        }
        */
        return clampCurrentPointToDestination(currentPoint, direction, gridDestination);
    }

    // TODO: Just use gridDestination as min and max, drop direction ...
    private static Point clampCurrentPointToDestination(Point currentPoint, Point direction, (int, int) gridDestination)
    {
        if (direction.x > 0 && currentPoint.x >= gridDestination.Item1)
        {
            currentPoint.x = gridDestination.Item1;
        }
        else if (direction.x < 0 && currentPoint.x <= gridDestination.Item1)
        {
            currentPoint.x = gridDestination.Item1;
        }
        else if (direction.y > 0 && currentPoint.y >= gridDestination.Item2)
        {
            currentPoint.y = gridDestination.Item2;
        }
        else if (direction.y < 0 && currentPoint.y <= gridDestination.Item2)
        {
            currentPoint.y = gridDestination.Item2;
        }
        else
        {
            currentPoint = currentPoint - direction;
        }

        return currentPoint;
    }

    private Vector3 getLocalPosition(Transform childObject)
    {
        // Debug.Log("Starting at " + childObject);
        Vector3 result = new Vector3(0, 0, 0);
        while(childObject.transform != null && childObject.transform != _mainBoard)
        {
            // Debug.Log("Adding position for: " + childObject);
            result += rotatePointAroundPivot(
                childObject.localPosition, Vector3.zero, childObject.parent.localRotation.eulerAngles);

            childObject = childObject.parent;
        }

        return result;
    }

    // https://answers.unity.com/questions/532297/rotate-a-vector-around-a-certain-point.html
    private Vector3 rotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        var dir = point - pivot;
        dir = Quaternion.Euler(angles) * dir;
        point = dir + pivot;

        return point;
    }

    private struct Point
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
}
