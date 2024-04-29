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
    [SerializeField] private StudioEventEmitter catchSound;

    private Team _team;
    private DevController _owner;
    private BallState _ballState = BallState.Dead;

    public void SetOwner(DevController owner)
    {
        if (_ballState == BallState.Live) catchSound.Play();
        else pickupSound.Play();

        _ballState = BallState.Dead;
        _owner = owner;
        _team = owner.team;
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

    // When a ball hits something, we will set the param to below:
    // 0 is other (discard?)
    // 1 is ground
    // 2 is walls
    // 3 is another player
    // 4 is the player
    private void OnCollisionEnter(Collision collision)
    {
        var param = 0;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            param = 1;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            param = 2;
        }

        if (_ballState == BallState.Live)
        {
            // todo, pass the velocity parameter on any hit
            // Debug.Log("Velocity: " + GetComponent<Rigidbody>().velocity.magnitude);


            if (collision.gameObject.TryGetComponent(out DevController controller))
            {
                if (controller != _owner && controller.team != _team)
                {
                    param = 4;
                    // controller.Die();
                    // _owner.Score();
                    Debug.Log("Hit Player");
                    SetDeadBall();
                }
            }

            if (_team == Team.TeamOne && collision.gameObject.layer == LayerMask.NameToLayer("TeamTwo"))
            {
                GameManager.teamOneScore++;
                GameManager.UpdateScore();
                SetDeadBall();
                param = 3;
            }
            else if (_team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                GameManager.teamTwoScore++;
                GameManager.UpdateScore();
                SetDeadBall();
                param = 3;
            }
        }

        if (param > 0) hitSound.Play();
    }
}