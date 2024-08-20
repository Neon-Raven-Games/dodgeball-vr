using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCurveTestingScript : MonoBehaviour
{

    public float speed;
    public Animator anim;
    public CombinedCurve movementCurve;

    public GameObject root;

    private void Update()
    {
        var time = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
        var position = movementCurve.Evaluate(time);
        root.transform.localPosition = position * speed;
        Debug.Log("Position: " + position);
    }
}


[Serializable]
public struct Vector3Keyframe
{
    public float time;
    public Vector3 value;
}