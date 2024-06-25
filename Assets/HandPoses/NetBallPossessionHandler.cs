using System.Collections.Generic;
using System.Linq;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NetBallPossessionHandler : MonoBehaviour
{
    [SerializeField] private List<DodgeballIndex> dodgeballs = new();
    
    private readonly Dictionary<BallType, DodgeballIndex> _dodgeballDictionary = new();
    
    private HandPose _handPose;
    private GameObject _currentDodgeball;
    private Transform[] _fingerTransforms;
    private bool _updatePose;
    private Animator _anim;
    private static readonly int _SGrabbing = Animator.StringToHash("Grabbing");

    public void Start()
    {
        _anim = GetComponent<Animator>();
        _fingerTransforms = GetComponentsInChildren<Transform>().Where(x => x.name.StartsWith("Bind")).ToArray();
        foreach (var dodgeball in dodgeballs)
            _dodgeballDictionary[dodgeball.type] = dodgeball;
    }

    public void SetBallType(BallType type)
    {
        if (!_anim) return;
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


    private void ApplyHandPose()
    {
        if (!_handPose) return;
        for (int i = 0; i < _fingerTransforms.Length && i < _handPose.fingerTransforms.Length; i++)
        {
            _fingerTransforms[i].localPosition = _handPose.fingerTransforms[i].localPosition;
            _fingerTransforms[i].localRotation = _handPose.fingerTransforms[i].localRotation;
        }
    }
    
    public void Editor_SetBallType(BallType type)
    {
        if (_currentDodgeball && (type == BallType.None || _dodgeballDictionary[type].dodgeball != _currentDodgeball))
            _currentDodgeball.SetActive(false);
        if (type == BallType.None)
        {
            _currentDodgeball = null;
            ApplyHandPose();
            return;
        }
        _currentDodgeball = _dodgeballDictionary[type].dodgeball;
        _currentDodgeball.SetActive(true);
        
        ApplyHandPose();
    }
}