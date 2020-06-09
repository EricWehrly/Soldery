using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chip : Grabbable
{
    void Start()
    {
        OnReleased += Chip_OnReleased;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Chip now colliding with " + collision.gameObject);
    }

    private void Chip_OnReleased(object sender, System.EventArgs e)
    {
        Debug.Log("I am RELEASED!");
        // if we're 'on' the final game, re-attach to the final game ...
        // spawn pins
    }
}
