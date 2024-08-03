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
    private Team _team;
    public BallState _ballState = BallState.Dead;
    private Rigidbody _rb;
    [SerializeField] private float maxVelocity = 10f; // The velocity at which the volume should be maximum
    [SerializeField] private float minVolume = 0.1f; // Minimum volume

    [SerializeField] private float maxVolume = 1f;

    // todo, we can bind to this if we want to in the throw lab for immediate return
    public Action<int> ballNotLive;
    internal int index;
    public Action<int, Vector3, Vector3> throwTrajectory;

    public void Start()
    {
        _rb = GetComponent<Rigidbody>();
        GetComponent<ThrowHandle>().onFinalTrajectory += HandleThrowTrajectory;
        hitParticle.transform.SetParent(null);
    }

    private void OnEnable()
    {
        var config = ConfigurationManager.GetThrowConfiguration();
        var handle = GetComponent<ThrowHandle>();
        handle.SetConfigForDevice(Device.UNSPECIFIED, config);
        handle.SetConfigForDevice(Device.OCULUS_TOUCH, config);
    }

    public void HandleThrowTrajectory(Vector3 velocity)
    {
        throwTrajectory?.Invoke(index, transform.position, velocity);
    }

    public void SetOwner(Team team)
    {
        PlaySound(SoundIndex.Pickup);
        _ballState = BallState.Possessed;
        _team = team;
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
                Debug.Log("Team two score!");
                var parentActor = collision.gameObject.GetComponentInParent<Actor>();
                if (!parentActor) collision.gameObject.GetComponent<Actor>();
                if (parentActor) parentActor.SetOutOfPlay(true);
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
                var parentActor = collision.gameObject.GetComponentInParent<Actor>();
                if (!parentActor) collision.gameObject.GetComponent<Actor>();
                if (parentActor) parentActor.SetOutOfPlay(true);
                SetDeadBall();
                HitSquash(collision);
                GameManager.teamOneScore++;
                GameManager.UpdateScore();
                param = 3;
            }

            if (_ballState == BallState.Dead) ballNotLive?.Invoke(param);
        }

        if (param > 0) PlaySound(SoundIndex.Hit);
    }
}