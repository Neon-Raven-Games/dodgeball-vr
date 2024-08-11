using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAnimationHelper : MonoBehaviour
{
    [SerializeField] private DodgeballAI dodgeballAI;
    [SerializeField] private GameObject particleEffect;
    // Start is called before the first frame update
    public void Throw()
    {
        dodgeballAI.ThrowBall();
    }
    
    public void ShadowStep()
    {
        particleEffect.SetActive(true);
        particleEffect.GetComponent<ParticleSystem>().Play();
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}
