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
        lock (syncLock)
        {
            var orderedList = Circuits.OrderByDescending(circuit => circuit.Distance);

            foreach (Circuit circuit in orderedList)
            {
                if (!circuit.Rendered) circuit.Render();
            }
        }
    }

    public void Render()
    {
        var lines = GameObject.Instantiate(_original, _mainBoard);
        lines.name = "Circuit " + circuitNumber++;
        var lineRenderer = lines.GetComponent<LineRenderer>();

        var gridPosition = CollisionMatrix.getPositionInCollisionMatrix(Origin.position);
        var gridDestination = CollisionMatrix.getPositionInCollisionMatrix(Destination.position);

        Vector3 originPosition = getLocalPosition(Origin);
        Vector3 destinationPosition = getLocalPosition(Destination);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, originPosition);

        // move 'forward' from pin, until you hit something
        var nextGridPoint = getNextLinePoint(
            new Point(gridPosition.Item1, gridPosition.Item2),
            new Point(-1, 0), lineRenderer, gridDestination);
        var nextPosition = CollisionMatrix.convertGridSpaceToObjectSpace(nextGridPoint.x, nextGridPoint.y);
        nextPosition.z = originPosition.z;
        if (nextGridPoint.x == gridDestination.Item1) nextPosition.x = destinationPosition.x;
        addLineRendererPoint(lineRenderer, nextPosition);
        CollisionMatrix.drawRayToCollisionMatrixPoint(nextGridPoint.x, nextGridPoint.y);

        // then turn, heading toward our destination
        var thirdPoint = getNextLinePoint(nextGridPoint, new Point(0, 1), lineRenderer, gridDestination);
        var thirdPosition = CollisionMatrix.convertGridSpaceToObjectSpace(thirdPoint.x, thirdPoint.y);
        thirdPosition.x = nextPosition.x;
        if (nextGridPoint.y == gridDestination.Item2) nextPosition.z = destinationPosition.z;
        addLineRendererPoint(lineRenderer, thirdPosition);
        CollisionMatrix.drawRayToCollisionMatrixPoint(thirdPoint.x, thirdPoint.y);

        // addLineRendererPoint(lineRenderer, getLocalPosition(Destination));

        Rendered = true;
    }

    private void addLineRendererPoint(LineRenderer lineRenderer, Vector3 point)
    {
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
    }

    private Point getNextLinePoint(Point currentPoint, Point direction, LineRenderer line, (int, int) gridDestination)
    {
        currentPoint = currentPoint + direction;

        // TODO: extract collision block to separate method ...
        while (CollisionMatrix.InBounds((currentPoint.x, currentPoint.y))
            && (CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == null
            || CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == Origin.gameObject))
        {
            if (direction.x > 0 && currentPoint.x >= gridDestination.Item1) break;
            if (direction.x < 0 && currentPoint.x <= gridDestination.Item1) break;
            if (direction.y > 0 && currentPoint.y >= gridDestination.Item2) break;
            if (direction.y < 0 && currentPoint.y <= gridDestination.Item2) break;

            CollisionMatrix.matrix[currentPoint.x, currentPoint.y] = line.gameObject;
            currentPoint = currentPoint + direction;
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
