using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Circuit
{
    private static readonly object syncLock = new object();

    private static Transform _mainBoard;
    private static GameObject _original;
    private static List<Circuit> Circuits = new List<Circuit>();

    public readonly float Distance;
    public readonly Transform Origin;
    public readonly Transform Destination;
    // public readonly GameObject LineRendererObject;
    public readonly List<Vector3> NodeList;

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
            // var orderedList = Circuits.OrderBy(circuit => circuit.Distance);
            var orderedList = Circuits.OrderByDescending(circuit => circuit.Distance);

            foreach (Circuit circuit in orderedList)
            {
                if (!circuit.Rendered) circuit.Render();
            }
        }
    }

    private static int circuitNumber = 0;

    public void Render()
    {
        var lines = GameObject.Instantiate(_original, _mainBoard);
        lines.name = "Circuit " + circuitNumber++;
        var lineRenderer = lines.GetComponent<LineRenderer>();

        var gridPosition = CollisionMatrix.getPositionInCollisionMatrix(Origin.position);

        Vector3 originPosition = getLocalPosition(Origin);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, originPosition);

        var nextPoint = getNextLinePoint(
            new Point(gridPosition.Item1, gridPosition.Item2),
            new Point(-1, 0));

        // move 'forward' from pin, until you hit something
        var secondPosition = CollisionMatrix.convertGridSpaceToObjectSpace(nextPoint.x, nextPoint.y);
        addLineRendererPoint(lineRenderer, secondPosition);
        CollisionMatrix.drawRayToCollisionMatrixPoint(nextPoint.x, nextPoint.y);

        // then turn, heading toward our destination
        var thirdPoint = getNextLinePoint(nextPoint, new Point(0, 1));
        var thirdPosition = CollisionMatrix.convertGridSpaceToObjectSpace(thirdPoint.x, thirdPoint.y);
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

    private Point getNextLinePoint(Point currentPoint, Point direction)
    {
        // Debug.Log("Getting next line point");
        var iterationCount = 0;
        currentPoint = currentPoint + direction;
        while (currentPoint.x > 1 && currentPoint.x < CollisionMatrix.matrix.GetLength(0) - 1
            && currentPoint.y > 1 && currentPoint.y < CollisionMatrix.matrix.GetLength(1) - 1
            && CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == false)
        {
            CollisionMatrix.matrix[currentPoint.x, currentPoint.y] = true;
            currentPoint = currentPoint + direction;

            iterationCount++;
        }
        // if (iterationCount < 2) Debug.Log("Iteration count only " + iterationCount);

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

        public override string ToString()
        {
            return x + ", " + y;
        }
    }
}
