using UnityEngine;

public class Pin : MonoBehaviour
{
    private static Transform _powerConnector;
    private static Material _poweredMaterial;

    private Transform PowerConnectorPinPlus;
    private Transform PowerConnectorPinMinus;

    public Transform ConnectionDestination;

    private void Awake()
    {
        // handleStaticAssignments();

        // connectStaticConnectors();

        if(enabled && ConnectionDestination != null)
        {
            new Circuit(transform, ConnectionDestination);

            // if name contains power
            // extend materials
            // second material is "powered" ...
        }
    }

    private void Start()
    {
        Circuit.RenderCircuits();
    }

    private void handleStaticAssignments()
    {
        if (_poweredMaterial == null)
        {
            // var mainBoard = GameObject.FindGameObjectWithTag("Mainboard");

            // _poweredMaterial = PoweredMaterial;
            _poweredMaterial = Resources.Load<Material>("Powered");
        }

        if(_powerConnector == null)
        {
            var mainboard = GameObject.Find("Main Board");
            _powerConnector = mainboard.transform.Find("Rear Connections").Find("Power Connector");
        }

        if (_powerConnector != null)
        {
            if (PowerConnectorPinPlus == null)
            {
                PowerConnectorPinPlus = _powerConnector.Find("Small Power +");
                PowerConnectorPinPlus.GetComponent<MeshRenderer>().material = _poweredMaterial;
            }
            if (PowerConnectorPinMinus == null)
            {
                PowerConnectorPinMinus = _powerConnector.Find("Small Power -");
                PowerConnectorPinMinus.GetComponent<MeshRenderer>().material = _poweredMaterial;
            }
        }
    }

    private void connectStaticConnectors()
    {
        if (gameObject.name.Contains("Power +") && PowerConnectorPinPlus != null)
        {
            new Circuit(transform, PowerConnectorPinPlus);
        }
        else if (gameObject.name.Contains("Power -") && PowerConnectorPinMinus != null)
        {
            new Circuit(transform, PowerConnectorPinMinus);
        }
    }
}
