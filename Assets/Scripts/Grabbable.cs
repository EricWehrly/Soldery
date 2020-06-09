using System;
using System.Collections;
using System.Collections.Generic;
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
        }
    }
    
    void Update()
    {
        if(grabbedObject == gameObject.transform && Input.GetMouseButtonUp(0))
        {
            grabbedObject = null;
            // if the object is a chip, spawn pins ...
            OnReleased.Invoke(this, null);
        }

        if(grabbedObject)
        {
            RaycastHit hitInfo;
            var screenRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(screenRay, out hitInfo);

            Vector3 newPos = Vector3.Lerp(transform.position, hitInfo.point, Time.deltaTime * 4);
            grabbedObject.position = newPos;
        }
    }

    public event EventHandler OnReleased;
}
