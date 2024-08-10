using System;
using System.Collections.Generic;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetBallPossessionHandler : MonoBehaviour
{
    [SerializeField] private List<DodgeballIndex> dodgeballs = new();
    
    // todo, this used vr HandSide. Had to exclude for server build. Make an enums folder and use our own
    [SerializeField] public HandSide handSide;
    private readonly Dictionary<BallType, DodgeballIndex> _dodgeballDictionary = new();
    private HandPose _handPose;
    private GameObject _currentDodgeball;
    private bool _updatePose;
    private Animator _anim;
    private static readonly int _SGrabbing = Animator.StringToHash("Grabbing");
    public Vector3 BallPosition => _currentDodgeball.transform.position;

    public void Start()
    {
        foreach (var dodgeball in dodgeballs) _dodgeballDictionary[dodgeball.type] = dodgeball;
        _anim.enabled = true;
    }

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _anim.enabled = false;
    }

    private NetBallPossession _possession;

    public void UpdatePossession(int id)
    {
        var netPossession = ServerOwnershipManager.GetBallPossession(id, handSide);

        if (_possession == netPossession) return;
        _possession = netPossession;
        var ballType = ServerOwnershipManager.GetBallType(id, handSide);
        SetBallType(ballType);
    }

    public void SetBallType(BallType type)
    {
        if (!_anim) return;

        if (type == BallType.None) _possession = NetBallPossession.None;
        else _possession = handSide == 0 ? NetBallPossession.LeftHand : NetBallPossession.RightHand;

        if (_currentDodgeball && (type == BallType.None || _dodgeballDictionary[type].dodgeball != _currentDodgeball))
            _currentDodgeball.SetActive(false);
        if (type == BallType.None)
        {
            _currentDodgeball = null;
            _anim.SetBool(_SGrabbing, false);
            return;
        }

        _anim.SetBool(_SGrabbing, true);
        _currentDodgeball = _dodgeballDictionary[type].dodgeball;
        _currentDodgeball.SetActive(true);
    }
}