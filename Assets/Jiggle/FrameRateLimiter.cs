using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateLimiter : MonoBehaviour
{
    public float MaxFrames = 9999;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (MaxFrames < 4)
            MaxFrames = 4;

        if (MaxFrames < 60)
        {
            int timeToWait = (int)((1000.0 / MaxFrames) - (Time.deltaTime*1000));
            if (timeToWait > 0)
                System.Threading.Thread.Sleep(timeToWait);
        }
	}
}
