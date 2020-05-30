using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class Circuit
{
    private const int RAYCAST_IGNORE_LAYER = 2;

    private static readonly object syncLock = new object();
    private const float STEP_AMOUNT = .01f;

    private static bool[,] collisionMatrix;
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

        mapCollisionMatrix();
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
            var orderedList = Circuits.OrderBy(circuit => circuit.Distance);

            foreach (Circuit circuit in orderedList)
            {
                if (!circuit.Rendered) circuit.Render();
            }
        }
    }

    public void Render()
    {
        var lines = GameObject.Instantiate(_original, _mainBoard);
        var lineRenderer = lines.GetComponent<LineRenderer>();

        var gridPosition = getPositionInCollisionMatrix(Origin.position);

        Vector3 originPosition = getLocalPosition(Origin);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, originPosition);

        var nextX = nextCollidingXIndex(gridPosition.Item1, gridPosition.Item2);
        if (nextX != -1)
        {
            lineRenderer.positionCount++;
            var newX = STEP_AMOUNT * (gridPosition.Item1 - nextX);
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(
                originPosition.x - newX,
                originPosition.y,
                originPosition.z
                ));

            var nextZ = getNextCollidingZIndex(nextX, gridPosition.Item2);
            if (nextZ != -1)
            {
                lineRenderer.positionCount++;
                var newZ = STEP_AMOUNT * (nextZ - gridPosition.Item2);
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(
                    originPosition.x - newX,
                    originPosition.y,
                    originPosition.z + newZ
                    ));
            }
        }

        Rendered = true;
    }

    private int nextCollidingXIndex(int xIndex, int zIndex)
    {
        while (xIndex > 0)
        {
            xIndex -= 1;
            if (collisionMatrix[xIndex, zIndex] == true)
            {
                return xIndex;
            }
            collisionMatrix[xIndex, zIndex] = true;
        }

        return xIndex;
    }

    private int getPrevCollidingZIndex(int xIndex, int zIndex)
    {
        while (zIndex > 0 && zIndex < collisionMatrix.GetLength(1))
        {
            zIndex -= 1;
            if (collisionMatrix[xIndex, zIndex] == true)
            {
                return zIndex;
            }
            collisionMatrix[xIndex, zIndex] = true;
        }

        return zIndex;
    }

    private int getNextCollidingZIndex(int xIndex, int zIndex)
    {
        while (zIndex < collisionMatrix.GetLength(1) - 1)
        {
            zIndex++;
            if (collisionMatrix[xIndex, zIndex] == true)
            {
                return zIndex;
            }
            collisionMatrix[xIndex, zIndex] = true;
        }

        return zIndex;
    }

    public void old_Render()
    {
        var lines = GameObject.Instantiate(_original, _mainBoard);
        var lineRenderer = lines.GetComponent<LineRenderer>();

        Vector3 originPosition = getLocalPosition(Origin);
        Vector3 destinationPosition = getLocalPosition(Destination);
        // Debug.Log("Local calculation is from " + originPosition + " to " + destinationPosition);

        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, originPosition);

        Vector3 secondPoint = getSecondLinePoint(originPosition);
        if (secondPoint != Vector3.zero)
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(1, secondPoint);

            var thirdLinePoint = getNextLinePoint(secondPoint);
            if (thirdLinePoint != Vector3.zero)
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 3, thirdLinePoint);
            } else
            {
                Debug.Log("No third line point.");
            }
        }
        // 'forward' to 'edge'
        // switch axes toward target, but not at

        Vector3 thirdPoint = new Vector3(originPosition.x, originPosition.y,
            destinationPosition.z);
        lineRenderer.SetPosition(lineRenderer.positionCount - 2, thirdPoint);
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, destinationPosition);

        addMeshToLineRenderer(lineRenderer);

        Rendered = true;
    }

    private void addMeshToLineRenderer(LineRenderer lineRenderer)
    {
        var initialWidth = lineRenderer.startWidth;
        lineRenderer.SetWidth(initialWidth * 5, initialWidth * 5);

        MeshCollider meshCollider = lineRenderer.gameObject.AddComponent<MeshCollider>();
        Mesh mesh = new Mesh();
        // lineRenderer.BakeMesh(mesh, Camera.main, true);
        lineRenderer.BakeMesh(mesh);
        meshCollider.sharedMesh = mesh;

        lineRenderer.SetWidth(initialWidth, initialWidth);
    }

    private Vector3 getSecondLinePoint(Vector3 originLocalPosition)
    {
        float maxDistance = 1f;
        RaycastHit hitInfo;

        // Origin.gameObject.layer = RAYCAST_IGNORE_LAYER;

        var rayOrigin = new Vector3(Origin.position.x, Origin.position.y + .001f, Origin.position.z);
        var rayDirection = Origin.forward * -1f;
        // We really wanna go 'backward' ... not sure why, but this is easier than flipping all the pins >_>
        Ray ray = new Ray(rayOrigin, rayDirection);
        bool isHit = Physics.Raycast(ray, out hitInfo, maxDistance);
        // Debug.DrawRay(rayOrigin, Origin.forward * -1f, Color.red, 999999);

        // Origin.gameObject.layer = 0;

        if (isHit)
        {
            var forward = Origin.forward * -1f * (hitInfo.distance);
            return originLocalPosition + forward;
        }
        // This is definitely incorrect, need to handle this ...
        else
        {
            Debug.DrawRay(rayOrigin, rayDirection, Color.white, 999999);
            return Vector3.zero;
        }
    }

    private Vector3 getNextLinePoint(Vector3 lastLinePoint)
    {
        // need to establish maxes for axes

        var lastLineWorldPoint = Origin.position + lastLinePoint;

        float maxDistance = 1f;
        RaycastHit hitInfo;

        var rayOrigin = lastLinePoint;
        rayOrigin.Scale(_mainBoard.lossyScale);
        rayOrigin += _mainBoard.position;
        var rayDirection = Origin.right * -1f;
        Ray ray = new Ray(rayOrigin, rayDirection);
        bool isHit = Physics.Raycast(ray, out hitInfo, maxDistance);

        if (isHit)
        {
            var forward = Origin.right * -1f * (hitInfo.distance);
            return lastLinePoint + forward;
        }
        // This is definitely incorrect, need to handle this ...
        else
        {
            Debug.DrawRay(rayOrigin, rayDirection, Color.red, 999999);
            return Vector3.zero;
        }
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

    private static void mapCollisionMatrix()
    {
        RaycastHit raycastHit;
        var rayDirection = Vector3.down;
        var renderer = _mainBoard.transform.Find("PCB").GetComponent<Renderer>();
        var x = renderer.bounds.min.x;
        var y = _mainBoard.position.y + 1f;
        var z = renderer.bounds.min.z;

        int horizontalPositionCount = Mathf.CeilToInt((renderer.bounds.max.x - renderer.bounds.min.x) / STEP_AMOUNT);
        int verticalPositionCount = Mathf.CeilToInt((renderer.bounds.max.z - renderer.bounds.min.z) / STEP_AMOUNT);
        collisionMatrix = new bool[horizontalPositionCount, verticalPositionCount];

        for (var zIndex = 0; zIndex < verticalPositionCount; zIndex++)
        {
            for (var xIndex = 0; xIndex < horizontalPositionCount; xIndex++)
            {
                var rayOrigin = new Vector3(x, y, z);
                bool isHit = Physics.Raycast(new Ray(rayOrigin, rayDirection), out raycastHit, 2f);

                if (isHit)
                {
                    // Debug.Log(raycastHit.collider.gameObject.name);

                    if(raycastHit.collider.gameObject.name == "PCB")
                    {
                        // Debug.DrawRay(rayOrigin, rayDirection, Color.green, 999999);
                        collisionMatrix[xIndex, zIndex] = false;
                    } else
                    {
                        Debug.DrawRay(rayOrigin, rayDirection, Color.red, 999999);
                        collisionMatrix[xIndex, zIndex] = true;
                    }
                }

                x += STEP_AMOUNT;
            }
            z += STEP_AMOUNT;
            x = renderer.bounds.min.x;
        }
    }

    private static (int, int) getPositionInCollisionMatrix(Vector3 transformWorldPosition)
    {
        var renderer = _mainBoard.transform.Find("PCB").GetComponent<Renderer>();

        var xIndex = Mathf.CeilToInt((transformWorldPosition.x - renderer.bounds.min.x) / STEP_AMOUNT);
        var zIndex = Mathf.CeilToInt((transformWorldPosition.z - renderer.bounds.min.z) / STEP_AMOUNT);
        return (xIndex, zIndex);
    }
}
