using System;
using Cysharp.Threading.Tasks;
using Hands.SinglePlayer.EnemyAI;
using UnityEngine;

public class ShadowStepUtility : Utility<ShadowStepUtilityArgs>, IUtility
{
    private readonly Animator _animator;
    private static DodgeballAI _ai;
    private bool _isShadowStepping;
    private float lastShadowStepTime = -Mathf.Infinity;


    public ShadowStepUtility(ShadowStepUtilityArgs args, AIState state, DodgeballAI ai) : base(args, state)
    {
        _ai = ai;
        _animator = ai.animator;
    }

    public override float Execute(DodgeballAI ai)
    {
        return -1f;
    }


    public override float Roll(DodgeballAI ai)
    {
        return -1f;

    }
}