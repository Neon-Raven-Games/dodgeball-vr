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
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public float transitionTime = 1.0f; // Time in seconds to complete one half of the animation (0 to 100 or 100 to 0)
    public float pauseTime = 0.5f;
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

    public void HitSquash(Collision collision)
    {
        var hitNormal = collision.GetContact(0).normal;
        var fromToRotation = Quaternion.FromToRotation(skinnedMeshRenderer.transform.up, hitNormal);
        skinnedMeshRenderer.transform.rotation = fromToRotation * skinnedMeshRenderer.transform.rotation;
        StartCoroutine(AnimateBlendShape());
    }

    private void SetDeadBall()
    {
        travelSound.Stop();
        _ballState = BallState.Dead;
    }

    IEnumerator AnimateBlendShape()
    {
        int blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("blendShape1.DodgeBall_Base1");
        if (blendShapeIndex == -1)
        {
            Debug.LogError("Blend shape not found!");
            yield break;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 100);
        yield return new WaitForSeconds(pauseTime);
        yield return StartCoroutine(AnimateBlendShapeValue(blendShapeIndex, 100, 0, transitionTime));
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, 0);
    }

    IEnumerator AnimateBlendShapeValue(int index, float startValue, float endValue, float duration)
    {
        float currentTime = 0;
        while (currentTime < duration)
        {
            float newValue = Mathf.Lerp(startValue, endValue, currentTime / duration);
            skinnedMeshRenderer.SetBlendShapeWeight(index, newValue);
            currentTime += Time.deltaTime;
            yield return null;
        }

        skinnedMeshRenderer.SetBlendShapeWeight(index, endValue);
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
                HitSquash(collision);
                param = 3;
            }
            else if (_team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                GameManager.teamTwoScore++;
                GameManager.UpdateScore();
                SetDeadBall();
                HitSquash(collision);
                param = 3;
            }
        }

        if (param > 0) hitSound.Play();
    }
}