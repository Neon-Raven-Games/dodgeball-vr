using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallBoundsHelper : MonoBehaviour
{
    public float backToBoundsForce = 3f;

    private int _ballLayer;
    // Start is called before the first frame update
    void Start()
    {
        _ballLayer = LayerMask.NameToLayer("Ball");
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer != _ballLayer) return;
        
        var rb = other.gameObject.GetComponent<Rigidbody>();
        var force = (transform.position - other.transform.position).normalized * backToBoundsForce;
        rb.AddForce(force);
    }
}
