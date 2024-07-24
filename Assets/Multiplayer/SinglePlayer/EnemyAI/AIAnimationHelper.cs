using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAnimationHelper : MonoBehaviour
{
    [SerializeField] private DodgeballAI dodgeballAI;
    // Start is called before the first frame update
    public void Throw()
    {
        dodgeballAI.ThrowBall();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
