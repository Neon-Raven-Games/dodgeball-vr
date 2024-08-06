using System;
using System.Collections;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

public class DodgeBall : MonoBehaviour
{
    [SerializeField] private GameObject hitParticle;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public float transitionTime = 1.0f; // Time in seconds to complete one half of the animation (0 to 100 or 100 to 0)
    public float pauseTime = 0.5f;
    internal Team _team;
    public BallState _ballState = BallState.Dead;
    private Rigidbody _rb;
    [SerializeField] private float maxVelocity = 10f; // The velocity at which the volume should be maximum
    [SerializeField] private float minVolume = 0.1f; // Minimum volume

    [SerializeField] private float maxVolume = 1f;

    private ThrowHandle _throwHandle;

    // todo, we can bind to this if we want to in the throw lab for immediate return
    public Action<int> ballNotLive;
    internal int index;
    public Action<int, Vector3, Vector3> throwTrajectory;
    private int ballLayer;
    private Actor ownerActor;

    public void Start()
    {
        GetComponent<ThrowHandle>().onFinalTrajectory += HandleThrowTrajectory;
        hitParticle.transform.SetParent(null);
        ballLayer = LayerMask.NameToLayer("Ball");
    }

    private void OnEnable()
    {
        _rb = GetComponent<Rigidbody>();
        var config = ConfigurationManager.GetThrowConfiguration();
        _throwHandle = GetComponent<ThrowHandle>();
        _throwHandle.SetConfigForDevice(Device.UNSPECIFIED, config);
        _throwHandle.SetConfigForDevice(Device.OCULUS_TOUCH, config);
    }

    public void HandleThrowTrajectory(Vector3 velocity)
    {
        throwTrajectory?.Invoke(index, transform.position, velocity);
    }

    public void SetOwner(Actor actor)
    {
        ownerActor = actor;
        PlaySound(SoundIndex.Pickup);
        _ballState = BallState.Possessed;
        _team = actor.team;
    }

    [SerializeField] private GameObject currentParticle;

    public void SetParticleActive(bool active) =>
        currentParticle.SetActive(active);

    [SerializeField] private AudioSource audioSource;

    public void SetLiveBall()
    {
        PlaySound(SoundIndex.Throw);
        _ballState = BallState.Live;
    }

    private void PlaySound(SoundIndex sound)
    {
        var volume = maxVolume;

        if (sound == SoundIndex.Hit || sound == SoundIndex.Throw)
        {
            var normalizedVelocity = Mathf.Clamp01(_rb.velocity.magnitude / maxVelocity);
            volume = Mathf.Lerp(minVolume, maxVolume, normalizedVelocity);
        }

        audioSource.volume = volume;
        audioSource.PlayOneShot(ConfigurationManager.GetIndexedSound(sound));
    }

    public void HitSquash(Collision collision)
    {
        var hitNormal = collision.GetContact(0).normal;
        var fromToRotation = Quaternion.FromToRotation(skinnedMeshRenderer.transform.up, hitNormal);
        skinnedMeshRenderer.transform.rotation = fromToRotation * skinnedMeshRenderer.transform.rotation;
        hitParticle.transform.position = collision.GetContact(0).point;
        hitParticle.SetActive(true);
        StartCoroutine(AnimateBlendShape());
    }

    private void SetDeadBall()
    {
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
        hitParticle.SetActive(false);
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
            var parentActor = collision.gameObject.GetComponentInParent<Actor>();
            if (!parentActor) collision.gameObject.GetComponent<Actor>();
            if (parentActor == ownerActor)
            {
                Debug.Log($"Found actors equal: {parentActor.gameObject.name}, {ownerActor.gameObject.name}");
                return;
            }
            
            if (_team == Team.TeamOne && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                Debug.Log($"Team One Friendly Fire, is this our rig? {parentActor.gameObject.name}");
                SetDeadBall();
                HitSquash(collision);
                param = 3;
            }
            else if (_team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamTwo"))
            {
                Debug.Log("Team Two Friendly Fire");
                SetDeadBall();
                HitSquash(collision);
                param = 3;
            }
            else if (_team == Team.TeamTwo && collision.gameObject.layer == LayerMask.NameToLayer("TeamOne"))
            {
                SetDeadBall();
                HitSquash(collision);
                parentActor.SetOutOfPlay(true);
                GameManager.teamTwoScore++;
                GameManager.UpdateScore();
                param = 3;
            }
            else if (_team == Team.TeamOne && collision.gameObject.layer == LayerMask.NameToLayer("TeamTwo"))
            {
                SetDeadBall();
                HitSquash(collision);
                parentActor.SetOutOfPlay(true);
                GameManager.teamOneScore++;
                GameManager.UpdateScore();
                param = 3;
            }

            if (_ballState == BallState.Dead) ballNotLive?.Invoke(param);
        }

        // todo, find out what this is hitting on the player
        if (param > 0)
        {
            PlaySound(SoundIndex.Hit);
        }
    }

    public void Throw()
    {
        _throwHandle.OnDetach();
        SetLiveBall();
        SetParticleActive(true);
    }

    public void Grab(Actor actor, GameObject hand)
    {
        SetOwner(actor);
        SetParticleActive(false);
        if (!_rb.isKinematic) _rb.velocity = Vector3.zero;
        if (hand) _throwHandle.OnAttach(hand, hand);
    }
}