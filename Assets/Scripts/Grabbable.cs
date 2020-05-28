using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grabbable : MonoBehaviour
{
    // on mouse down, if an object is not grabbed, grab this
    // on mouse up, if an object is grabbed, release it

    // on update, if this is grabbed, lerp it towards the camera raycast

    private static GameObject grabbedObject;

    private void OnMouseDown()
    {
        if(grabbedObject == null)
        {
            grabbedObject = gameObject;
        }
    }
    
    void Update()
    {
        if(grabbedObject == gameObject && Input.GetMouseButtonUp(0))
        {
            Debug.Log("Releasing " + gameObject.name);
        }
    }
}
