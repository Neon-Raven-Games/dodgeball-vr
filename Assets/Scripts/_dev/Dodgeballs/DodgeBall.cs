using System.Collections;
using FMODUnity;
using Unity.Template.VR.Multiplayer;
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

    // todo, make this enum
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


            if (collision.gameObject.TryGetComponent(out DevController controller))
            {
                if (controller != _owner && controller.team != _team)
                {
                    HitSquash(collision);
                    SetDeadBall();
                    
                    param = 4;
                    // controller.Die();
                    // _owner.Score();
                    
                    HitOppositeTeam(controller);
                    Debug.Log($"Hit Player! {_owner.team} hit {controller.team}!");
                }
            }

            if (_owner != controller && _team == Team.TeamOne && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                Debug.Log("Team One Friendly Fire");
                SetDeadBall();
                HitSquash(collision);
                FriendlyFire();
                param = 3;
            }
            else if (_owner != controller && _team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamTwo"))
            {
                Debug.Log("Team Two Friendly Fire");
                SetDeadBall();
                HitSquash(collision);
                FriendlyFire();
                param = 3;
            }
        }

        if (param > 0) hitSound.Play();
    }

    private void SetNetDeadBall()
    {
        _owner.networkPlayer.RPC_TargetHit(Team.None, Team.None, GetComponent<NetDodgeball>().index);
    }
    private void FriendlyFire()
    {
        if (_ballState != BallState.Live) return;
        _owner.networkPlayer.RPC_TargetHit(_owner.team, _owner.team, GetComponent<NetDodgeball>().index);
    }

    private void HitOppositeTeam(DevController controller)
    {
        _owner.networkPlayer.RPC_TargetHit(_owner.team, controller.team, GetComponent<NetDodgeball>().index);
    }
}