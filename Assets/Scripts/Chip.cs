using UnityEngine;

public class Chip : Grabbable
{
    void Start()
    {
        OnGrabbed += Chip_OnGrabbed;
        OnReleased += Chip_OnReleased;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Chip now colliding with " + collision.gameObject);
    }

    private void Chip_OnGrabbed(object sender, System.EventArgs e)
    {
        var parent = gameObject.transform.parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.GetComponent<Pin>() != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void Chip_OnReleased(object sender, System.EventArgs e)
    {
        Debug.Log("I am RELEASED!");
        // TODO: if we're 'on' the final game, re-attach to the final game ...

        var parent = gameObject.transform.parent;

        parent.position = transform.position;
        transform.localPosition = Vector3.zero;

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.GetComponent<Pin>() != null)
            {
                child.gameObject.SetActive(true);
                // child.gameObject.enabl
                // enable
                // child.gameObject.
            }
        }
    }
}
