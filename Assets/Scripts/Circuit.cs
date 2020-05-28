using System.Collections.Generic;
using UnityEngine;

public class Circuit
{
    private static Transform _mainBoard;
    private static GameObject _original;
    private static List<Circuit> Circuits = new List<Circuit>();

    public readonly GameObject Origin;
    public readonly GameObject Destination;
    public readonly GameObject LineRendererObject;
    public readonly List<Vector2> NodeList;

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
        // create a new gameobject
        // give it a line renderer
        // find the path from origin to destination
        // mark all the points ...

        // GameObject.Instantiate<>
        var lines = GameObject.Instantiate(_original, _mainBoard);
        var lineRenderer = lines.GetComponent<LineRenderer>();

        lineRenderer.positionCount = 3;

        Vector3 originPosition = getLocalPosition(origin);
        Vector3 destinationPosition = getLocalPosition(destination);
        // Debug.Log("Local calculation is from " + originPosition + " to " + destinationPosition);

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, originPosition);
        Vector3 secondPoint = new Vector3(originPosition.x, originPosition.y,
            destinationPosition.z);
        // lineRenderer.SetPosition(1, secondPoint);
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, destinationPosition);
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

    private class CircuitNodes
    {
        public readonly GameObject Origin;
        public readonly GameObject Destination;
        public readonly List<Vector2> NodeList;

        public CircuitNodes(GameObject origin, GameObject destination, List<Vector2> nodeList)
        {
            Origin = origin;
            Destination = destination;
            NodeList = nodeList;
        }
    }
}
