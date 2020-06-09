using UnityEngine;

// TODO: Assign pin script to all scene clickable scripts, make scene clickable abstract, make pin inherit it
public class SceneClickable : MonoBehaviour
{
    private static GameObject _firstConnection;
    private static Transform _lineParent;

    public Material MouseOverMaterial;
    public Material HighlightMaterial;
    public GameObject ConnectorLine;
    public bool highlight = true;   // so we don't have to recompile

    private MeshRenderer _meshRenderer;
    private Material _initialMaterial;

    void Start()
    {
        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
        _initialMaterial = _meshRenderer.material;

        if (MouseOverMaterial == null)
        {
            // System.Console.Error("No highlight material defined.");
            Debug.Log("No highlight material defined.");
        }

        GameObject mainBoard = GameObject.Find("Main Board");
        _lineParent = mainBoard.transform;
    }

    private void OnMouseOver()
    {
        if (highlight) _meshRenderer.material = MouseOverMaterial;
    }

    private void OnMouseExit()
    {
        _meshRenderer.material = _initialMaterial;
    }

    private void OnMouseDown()
    {
        Debug.Log("Firstconnection started as " + _firstConnection);

        if (_firstConnection == gameObject)
        {
            changeMaterial(_firstConnection, _initialMaterial);
            _firstConnection = null;
        }
        else if (_firstConnection != null && _firstConnection.transform.parent != gameObject.transform.parent)
        {
            // drawLines(_firstConnection.transform, gameObject.transform);
            changeMaterial(_firstConnection, _initialMaterial);
            new Circuit(_firstConnection.transform, gameObject.transform)
                .Render();
            _firstConnection = null;
        }
        else
        {
            _firstConnection = gameObject;
            changeMaterial(_firstConnection, HighlightMaterial);
        }

        if (_firstConnection == null) Debug.Log("First connection is null.");
        else Debug.Log("First connection ended as " + _firstConnection + " at " + _firstConnection.transform.position);
    }

    private void changeMaterial(GameObject targetObject, Material newMaterial)
    {
        MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
        meshRenderer.material = newMaterial;
    }

    // later we're going to need to make sure lines don't intersect ...
    private void drawLines(Transform origin, Transform destination)
    {
        Debug.Log("From " + origin +
            " at world " + origin.position +
            " to " + destination +
            " at world " + destination.position);

        GameObject connectorObject = Instantiate(ConnectorLine, _lineParent);
        LineRenderer connectorLines = connectorObject.GetComponent<LineRenderer>();

        Vector3 originPosition = getLocalPosition(origin);
        Vector3 destinationPosition = getLocalPosition(destination);
        // Debug.Log("Local calculation is from " + originPosition + " to " + destinationPosition);

        // connectorLines.positionCount = 2;
        connectorLines.SetPosition(0, originPosition);
        Vector3 secondPoint = new Vector3(originPosition.x, originPosition.y,
            destinationPosition.z);
        connectorLines.SetPosition(1, secondPoint);
        connectorLines.SetPosition(2, destinationPosition);

        connectorObject.SetActive(true);
    }

    private Vector3 getLocalPosition(Transform childObject)
    {
        return getLocalSimple(childObject);
    }

    private Vector3 getLocalSimple(Transform childObject)
    {
        Vector3 result = rotatePointAroundPivot(
                childObject.localPosition, Vector3.zero, childObject.parent.localRotation.eulerAngles);

        // TODO: while parent != mainboard ...
        result += childObject.parent.localPosition;

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
