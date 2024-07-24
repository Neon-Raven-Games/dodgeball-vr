using System;
using System.Collections;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public class DodgeBall : MonoBehaviour
{
    [SerializeField] private AudioSource pickupSound;
    [SerializeField] private AudioSource hitSound;
    [SerializeField] private AudioSource travelSound;
    [SerializeField] private AudioSource throwSound;
    [SerializeField] private AudioSource catchSound;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public float transitionTime = 1.0f; // Time in seconds to complete one half of the animation (0 to 100 or 100 to 0)
    public float pauseTime = 0.5f;
    private Team _team;
    public BallState _ballState = BallState.Dead;
    
    public Action<int> ballNotLive;
    internal int index;
    public Action<int, Vector3, Vector3> throwTrajectory;

    public void Start()
    {
        GetComponent<ThrowHandle>().onFinalTrajectory += HandleThrowTrajectory;
    }
    
    public void HandleThrowTrajectory(Vector3 velocity)
    {
        throwTrajectory?.Invoke(index, transform.position, velocity);
    }
    public void SetOwner(Team team)
    {
        if (_ballState == BallState.Live) catchSound.Play();
        else pickupSound.Play();

        _ballState = BallState.Possessed;
        _team = team;
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

    private void OnCollisionEnter(Collision collision)
    {
        
        var param = 0;
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            param = 1;
            SetDeadBall();
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            param = 2;
            SetDeadBall();
        }

        if (_ballState == BallState.Live)
        {
            // todo, pass the velocity parameter on any hit
            // Debug.Log("Velocity: " + GetComponent<Rigidbody>().velocity.magnitude);


            // todo, we need to make a networked script to hold data for the player 
            // if (collision.gameObject.TryGetComponent(out DevController controller))
            // {
            //     if (controller != _owner && controller.team != _team)
            //     {
            //         HitSquash(collision);
            //         SetDeadBall();
            //         
            //         param = 4;
            //         // controller.Die();
            //         // _owner.Score();
            //         
            //         // HitOppositeTeam(controller);
            //         Debug.Log($"Hit Player! {_owner.team} hit {controller.team}!");
            //     }
            // }

            // todo, find a way to gracefully exclude ai colliders from this check
            if (_team == Team.TeamOne && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                return;
                Debug.Log("Team One Friendly Fire");
                SetDeadBall();
                HitSquash(collision);
                // FriendlyFire();
                param = 3;
            }
            else if (_team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamTwo"))
            {
                return;
                Debug.Log("Team Two Friendly Fire");
                SetDeadBall();
                HitSquash(collision);
                // FriendlyFire();
                param = 3;
            }
            else if (_team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                Debug.Log("Team two score!");
                collision.gameObject.GetComponentInParent<Actor>().SetOutOfPlay(true);
                SetDeadBall();
                HitSquash(collision);
                GameManager.teamTwoScore++;
                GameManager.UpdateScore();
                // Score(Team.TeamTwo);
                param = 3;
            }
            else if (_team == Team.TeamOne && collision.gameObject.layer == LayerMask.NameToLayer("TeamTwo"))
            {
                Debug.Log("Team One score!");
                collision.gameObject.GetComponentInParent<Actor>().SetOutOfPlay(true);
                SetDeadBall();
                HitSquash(collision);
                GameManager.teamOneScore++;
                GameManager.UpdateScore();
                param = 3;
            }
            if (_ballState == BallState.Dead) ballNotLive?.Invoke(param);
        }

        if (param > 0) hitSound.Play();
    }
}