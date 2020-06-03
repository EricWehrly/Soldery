using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Circuit
{
    private static readonly object syncLock = new object();

    private static Transform _mainBoard;
    private static GameObject _original;
    private static List<Circuit> Circuits = new List<Circuit>();
    private static int circuitCount = 0;

    public readonly float Distance;
    public readonly Transform Origin;
    public readonly Transform Destination;
    protected GameObject gameObject { get; private set; }

    private bool Rendered { get; set; }

    private List<(int, int)> GridPoints = new List<(int, int)>();
    private CollisionReason lastCollisionReason = CollisionReason.NONE;
    private GameObject lastCollidedGameObject;
    private int depth = 0;

    static Circuit()
    {
        _mainBoard = GameObject.Find("Main Board").transform;
        _original = Resources.Load<GameObject>("Circuit");

        // Because Unity is being a pain:
        Application.targetFrameRate = 60;
        // Debug.Log("Target frame rate: " + Application.targetFrameRate);
        // Debug.Log("vSync: " + QualitySettings.vSyncCount);
    }

    public static void RenderCircuits()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        lock (syncLock)
        {
            var orderedList = Circuits.OrderByDescending(circuit => circuit.Distance);

            foreach (Circuit circuit in orderedList)
            {
                if (!circuit.Rendered)
                {
                    circuit.Render();
                }
            }
        }
        var elapsedMs = watch.ElapsedMilliseconds;
        if (elapsedMs != 0) Debug.Log("Circuits took " + elapsedMs + "ms to render.");
    }

    private static Circuit fromGameObject(GameObject from)
    {
        foreach(Circuit circuit in Circuits)
        {
            if (circuit.gameObject == from) return circuit;
        }

        return null;
    }

    public Circuit(Transform origin, Transform destination)
    {
        Origin = origin;
        Destination = destination;

        Distance = Vector2.Distance(destination.localPosition, origin.localPosition);
        Circuits.Add(this);
    }

    public void Destroy()
    {
        GameObject.DestroyImmediate(gameObject);
        depth = 0;

        foreach(var gridPoint in GridPoints)
        {
            CollisionMatrix.matrix[gridPoint.Item1, gridPoint.Item2] = null;
        }
        GridPoints.Clear();

        Rendered = false;
    }

    public void Render()
    {
        gameObject = GameObject.Instantiate(_original, _mainBoard);
        gameObject.name = "Circuit " + circuitCount++;
        var lines = gameObject.GetComponent<LineRenderer>();

        var gridPosition = CollisionMatrix.getPositionInCollisionMatrix(Origin.position);
        var gridDestination = CollisionMatrix.getPositionInCollisionMatrix(Destination.position);

        Vector3 originPosition = getLocalPosition(Origin);
        Vector3 destinationPosition = getLocalPosition(Destination);
        lines.positionCount = 1;
        lines.SetPosition(0, originPosition);

        // move 'forward' from pin, until you hit something
        var direction = new Point(-1, 0);
        var nextGridPoint = getNextGridPosition(
            new Point(gridPosition.Item1, gridPosition.Item2),
            direction, gridDestination);
        var nextPosition = CollisionMatrix.convertGridSpaceToObjectSpace(nextGridPoint.x, nextGridPoint.y);
        nextPosition.z = originPosition.z;
        if (nextGridPoint.x == gridDestination.Item1) nextPosition.x = destinationPosition.x;
        addLineRendererPoint(lines, nextPosition);
        // CollisionMatrix.drawRayToCollisionMatrixPoint((nextGridPoint.x, nextGridPoint.y));

        // then turn, heading toward our destination
        continueToDestination(gridDestination, destinationPosition, direction, nextGridPoint, nextPosition, lines);

        Rendered = true;
    }

    private void continueToDestination((int, int) gridDestination, Vector3 destinationPosition,
        Point prevDirection, Point prevGridPoint, Vector3 prevPosition, LineRenderer lineRenderer)
    {
        depth++;
        Point direction = getNextDirection(prevDirection);

        Vector3 nextPosition;
        var nextPoint = getNextGridPosition(prevGridPoint, direction, gridDestination);
        nextPosition = getNextPosition(gridDestination, destinationPosition, prevDirection, prevPosition, nextPoint);
        // CollisionMatrix.drawRayToCollisionMatrixPoint((nextPoint.x, nextPoint.y));

        // TODO: if we've collided with a line, see if we can "hug" that line ...

        if (nextPosition == prevPosition)
        {
            handleRepeatedPoints(gridDestination, prevGridPoint, direction);
        } else
        {
            addLineRendererPoint(lineRenderer, nextPosition);
        }

        if(depth > 15)
        {
            Debug.Log(gameObject.name + " exceeded 15 depth.");
            return;
        }

        if (nextPosition != destinationPosition)
        {
            continueToDestination(gridDestination, destinationPosition, direction, nextPoint, nextPosition, lineRenderer);
        }
    }

    private bool handleRepeatedPoints((int, int) gridDestination, Point prevGridPoint, Point direction)
    {
        // we've reversed directions because we collided with something. reverse back and see what we last collided with
        direction = getNextDirection(direction);
        getNextGridPosition(prevGridPoint, direction, gridDestination);

        if(lastCollidedGameObject != null)
        {
            Circuit collidingCircuit = Circuit.fromGameObject(lastCollidedGameObject);
            if(collidingCircuit != null) collidingCircuit.Destroy();
        }

        return true;
    }

    private static Vector3 getNextPosition((int, int) gridDestination, Vector3 destinationPosition, Point prevDirection, Vector3 prevPosition, Point nextPoint)
    {
        Vector3 nextPosition;
        if (nextPoint.x == gridDestination.Item1 && nextPoint.y == gridDestination.Item2)
        {
            nextPosition = destinationPosition;
        }
        else
        {
            nextPosition = CollisionMatrix.convertGridSpaceToObjectSpace(nextPoint.x, nextPoint.y);
            nextPosition = clampPosition(gridDestination, destinationPosition, prevDirection, prevPosition, nextPoint, nextPosition);
        }

        return nextPosition;
    }

    private Point getNextDirection(Point prevDirection)
    {
        if (prevDirection.x == -1) return new Point(0, 1);

        else return new Point(-1, 0);
    }

    private static Vector3 clampPosition((int, int) gridDestination, Vector3 destinationPosition,
        Point prevDirection, Vector3 prevPosition, Point nextPoint, Vector3 nextPosition)
    {
        nextPosition.y = destinationPosition.y;

        if (prevDirection.y == 0) nextPosition.x = prevPosition.x;
        else nextPosition.z = prevPosition.z;

        if (nextPoint.x == gridDestination.Item1) nextPosition.x = destinationPosition.x;
        else if (nextPoint.y == gridDestination.Item2) nextPosition.z = destinationPosition.z;

        return nextPosition;
    }

    private void addLineRendererPoint(LineRenderer lineRenderer, Vector3 point)
    {
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
    }

    private Point getNextGridPosition(Point currentPoint, Point direction, (int, int) gridDestination)
    {
        currentPoint = currentPoint + direction;

        lastCollisionReason = getCollisionReason(currentPoint, direction, gridDestination);
        while (lastCollisionReason == CollisionReason.NONE)
        {
            CollisionMatrix.matrix[currentPoint.x, currentPoint.y] = gameObject.gameObject;
            GridPoints.Add((currentPoint.x, currentPoint.y));
            currentPoint = currentPoint + direction;

            lastCollisionReason = getCollisionReason(currentPoint, direction, gridDestination);
        }

        if (CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == Destination.gameObject)
        {
            return new Point(gridDestination.Item1, gridDestination.Item2);

        } else if(lastCollisionReason == CollisionReason.AT_DESTINATION)
        {
            // turn and see if something is between us and destination. if so, back up ...
            var nextPosition = getNextGridPosition(currentPoint, getNextDirection(direction), gridDestination);
            (int, int) nextGridPosition = (nextPosition.x, nextPosition.y);

            if(nextGridPosition != gridDestination && lastCollisionReason == CollisionReason.HIT_OBJECT)
            {
                // back up ...
                return currentPoint - direction - direction;
            }
        }

        return clampCurrentPointToDestination(currentPoint, direction, gridDestination);
    }

    private CollisionReason getCollisionReason(Point currentPoint, Point direction, (int, int) gridDestination)
    {
        if (!CollisionMatrix.InBounds(currentPoint.x, currentPoint.y)) return CollisionReason.OUT_OF_BOUNDS;

        if (notPassedDestination(currentPoint, direction, gridDestination)) return CollisionReason.AT_DESTINATION;

        if (!canMoveInMatrixPosition(currentPoint))
        {
            lastCollidedGameObject = CollisionMatrix.matrix[currentPoint.x, currentPoint.y];
            return CollisionReason.HIT_OBJECT;
        }

        return CollisionReason.NONE;
    }

    private bool canMoveInMatrixPosition(Point currentPoint)
    {
        return CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == null
                    || CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == Origin.gameObject
                    || CollisionMatrix.matrix[currentPoint.x, currentPoint.y] == gameObject;
    }

    private bool notPassedDestination(Point currentPoint, Point direction, (int, int) gridDestination)
    {
        return direction.x > 0 && currentPoint.x >= gridDestination.Item1
            || direction.x < 0 && currentPoint.x <= gridDestination.Item1
            || direction.y > 0 && currentPoint.y >= gridDestination.Item2
            || direction.y < 0 && currentPoint.y <= gridDestination.Item2;
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

    private enum CollisionReason
    {
        NONE,
        OUT_OF_BOUNDS,
        AT_DESTINATION,
        HIT_OBJECT
    }
}
