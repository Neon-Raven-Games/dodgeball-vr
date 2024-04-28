using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class DodgeBall : MonoBehaviour
{
    [SerializeField] private StudioEventEmitter pickupSound;
    [SerializeField] private StudioEventEmitter hitSound;
    [SerializeField] private StudioEventEmitter travelSound;
    [SerializeField] private StudioEventEmitter throwSound;

    private Team _team;
    private DevController _owner;
    private BallState _ballState;
    
    public void SetOwner(DevController owner)
    {
        _ballState = BallState.Dead;
        _owner = owner;
        _team = owner.team;
        pickupSound.Play();
    }
    
    public void SetLiveBall()
    {
        throwSound.Play();
        travelSound.Play();
        _ballState = BallState.Live;
    }

    private void SetDeadBall()
    {
        travelSound.Stop();
        _ballState = BallState.Dead;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_ballState == BallState.Live)
        {
            hitSound.Play();
            if (collision.gameObject.TryGetComponent(out DevController controller))
            {
                if (controller != _owner && controller.team != _team)
                {
                    // controller.Die();
                    // _owner.Score();
                    Debug.Log("Hit Player");
                    SetDeadBall();
                }
            }

            if (_team == Team.TeamOne && collision.gameObject.layer == LayerMask.NameToLayer("TeamTwo"))
            {
                Debug.Log("Velocity: " + GetComponent<Rigidbody>().velocity.magnitude);
                GameManager.teamOneScore++;
                GameManager.UpdateScore();
            }
            else if (_team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                GameManager.teamTwoScore++;
                GameManager.UpdateScore();
            }
        }
    }

    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
