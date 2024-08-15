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
    
    // todo, use new layer to override hand/arm for signage
    public static readonly int HandSign = Animator.StringToHash("HandSign");
    
    // Start is called before the first frame update
    public void Throw()
    {
        dodgeballAI.ThrowBall();
    }
    
    public void ShadowStep()
    {
        if (shadowStep != null) shadowStep.InitialShadowStepFinished();
        if (dodgeballAI is NinjaAgent ninja) ninja.InitialShadowStepFinished();
    }

    public void Substitution()
    {
        if (dodgeballAI is NinjaAgent ninja) ninja.InitialSubstitutionFinished();
    }

    // Update is called once per frame
    private void Update()
    {
        
    }
}
