using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class Circuit
{
    private static readonly object syncLock = new object();

    private static Transform _mainBoard;
    private static GameObject _original;
    private static List<Circuit> Circuits = new List<Circuit>();

    public readonly float Distance;
    public readonly Transform Origin;
    public readonly Transform Destination;
    // public readonly GameObject LineRendererObject;
    public readonly List<Vector2> NodeList;

    public bool Rendered { get; private set; }

    static Circuit()
    {
        _mainBoard = GameObject.Find("Main Board").transform;
        _original = Resources.Load<GameObject>("Circuit");

        // Because Unity is being a pain:
        Application.targetFrameRate = 60;
        Debug.Log("Target frame rate: " + Application.targetFrameRate);
        Debug.Log("vSync: " + QualitySettings.vSyncCount);
    }

    public Circuit(Transform origin, Transform destination)
    {
        Origin = origin;
        Destination = destination;

        Distance = Vector2.Distance(destination.localPosition, origin.localPosition);
        Debug.Log("Distance: " + Distance);
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

        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, originPosition);
        Vector3 secondPoint = new Vector3(originPosition.x, originPosition.y,
            destinationPosition.z);
        lineRenderer.SetPosition(1, secondPoint);
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, destinationPosition);

        Rendered = true;
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
