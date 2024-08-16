using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAnimationHelper : MonoBehaviour
{
    [SerializeField] private DodgeballAI dodgeballAI;
    [SerializeField] private ShadowStep shadowStep;
    public static readonly int SSpecialOne = Animator.StringToHash("SpecialOne");
    public static readonly int SSpecialTwo = Animator.StringToHash("SpecialTwo");
    public static readonly int SSpecialOneExit = Animator.StringToHash("ShadowDash_SpinThrow_L");
    
    public ColorLerp colorLerp;
    // todo, use new layer to override hand/arm for signage
    public static readonly int HandSign = Animator.StringToHash("HandSign");
    
    // Start is called before the first frame update
    public void Throw()
    {
        if (dodgeballAI is NinjaAgent)
            Debug.Log("animation throw");
        dodgeballAI.ThrowBall();
    }

    public float colorLerpValue;

    private void Update()
    {
        if (colorLerp) colorLerp.lerpValue = colorLerpValue;
    }
    
    public void ShadowStep()
    {
        if (shadowStep != null) shadowStep.InitialShadowStepFinished();
        if (dodgeballAI is NinjaAgent ninja) ninja.InitialShadowStepFinished();
    }
}
