using System;
using UnityEngine;

public class Grabbable : MonoBehaviour
{
    // on mouse down, if an object is not grabbed, grab this
    // on mouse up, if an object is grabbed, release it

    // on update, if this is grabbed, lerp it towards the camera raycast

    private static Transform grabbedObject;

    private void OnMouseDown()
    {
        if(grabbedObject == null)
        {
            grabbedObject = gameObject.transform;
            OnGrabbed.Invoke(this, null);
        }
    }
    
    void Update()
    {
        if(grabbedObject == gameObject.transform && Input.GetMouseButtonUp(0))
        {
            grabbedObject = null;
            OnReleased.Invoke(this, null);
        }

        if(grabbedObject)
        {
            RaycastHit hitInfo;
            var screenRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(screenRay, out hitInfo);

            // Vector3 newPos = Vector3.Lerp(transform.position, hitInfo.point, Time.deltaTime * 4);
            // grabbedObject.position = newPos;
            grabbedObject.position = hitInfo.point;
        }
    }

    public event EventHandler OnGrabbed;

    public event EventHandler OnReleased;
}
