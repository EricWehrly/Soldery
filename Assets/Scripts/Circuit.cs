using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class Circuit
{
    private const int RAYCAST_IGNORE_LAYER = 2;

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

        Vector3 originPosition = getLocalPosition(Origin);
        Vector3 destinationPosition = getLocalPosition(Destination);
        // Debug.Log("Local calculation is from " + originPosition + " to " + destinationPosition);

        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, originPosition);

        Vector3 secondPoint = getSecondLinePoint(originPosition);
        // getNextLinePoint(originPosition, Origin.forward * -1f);
        
        // We really wanna go 'backward' ... not sure why, but this is easier than flipping all the pins >_>
        Vector3 secondPosition = originPosition + (Origin.forward * -.1f);
        lineRenderer.SetPosition(1, secondPoint);
        // 'forward' to 'edge'
        // switch axes toward target, but not at

        Vector3 thirdPoint = new Vector3(originPosition.x, originPosition.y,
            destinationPosition.z);
        lineRenderer.SetPosition(lineRenderer.positionCount - 2, thirdPoint);
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, destinationPosition);

        // addMeshToLineRenderer(lineRenderer);

        Rendered = true;
    }

    private void addMeshToLineRenderer(LineRenderer lineRenderer)
    {
        MeshCollider meshCollider = lineRenderer.gameObject.AddComponent<MeshCollider>();
        Mesh mesh = new Mesh();
        lineRenderer.BakeMesh(mesh, true);
        meshCollider.sharedMesh = mesh;
    }

    private Vector3 getSecondLinePoint(Vector3 originLocalPosition)
    {
        float maxDistance = 1f;
        RaycastHit hitInfo;

        Origin.gameObject.layer = RAYCAST_IGNORE_LAYER;

        var rayOrigin = new Vector3(Origin.position.x, Origin.position.y + .001f, Origin.position.z);
        Ray ray = new Ray(rayOrigin, Origin.forward * -1f);
        bool isHit = Physics.Raycast(ray, out hitInfo, maxDistance);
        // Debug.DrawRay(rayOrigin, Origin.forward * -1f, Color.red, 999999);

        // bool isHit = Origin.GetComponent<Collider>().Raycast(ray, out hitInfo, maxDistance);

        /*
        if (isHit)
        {
            var hitObject = hitInfo.collider.gameObject;
            Debug.Log("Hit from " + Origin.position + " to " + hitInfo.normal);
            Debug.Log("From: " + Origin.parent.name + " " + Origin.name);
            Debug.Log("To: " + hitObject.transform.parent.name + " " +
                hitInfo.collider.gameObject.name);
        }
        */

        Origin.gameObject.layer = 0;

        if (isHit)
        {
            var forward = Origin.forward * -1f * (hitInfo.distance);
            return originLocalPosition + forward;
        }
        // This is definitely incorrect, need to handle this ...
        else return new Vector3(0, 0, 0);
    }

    private Vector3 getNextLinePoint(Vector3 lastLinePoint, Vector3 direction)
    {
        // need to establish maxes for axes

        var rayResult = Physics.Raycast(lastLinePoint, direction);
        Debug.Log(rayResult);

        return new Vector3(0, 0, 0);
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
}
