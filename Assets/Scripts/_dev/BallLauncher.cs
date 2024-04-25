using System.Collections;
using System.Collections.Generic;
using CloudFine.ThrowLab;
using UnityEngine;
using UnityEngine.UI;

public class BallLauncher : MonoBehaviour
{
    [SerializeField] private float secondsDelay = 2f;
    [SerializeField] private float launchForce = 1000f;
    [SerializeField] private bool despawnAfterSeconds = true;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private GameObject ballSpawnPoint;
    [SerializeField] private DodgeballLab lab;
    [SerializeField] private Slider delaySlider;
    [SerializeField] private Slider forceSlider;
    private bool _launching;
    
    public void UpdateUI()
    {
        forceSlider.value = launchForce / 100f;
        delaySlider.value = secondsDelay;
    }
    
    public void AdjustLaunchForce(float force)
    {
        launchForce = force * 100f;
    }
    
    public void AdjustSecondsDelay(float seconds)
    {
        secondsDelay = seconds;
    }
    
    public void StartLaunching()
    {
        _launching = true;
        StartCoroutine(LaunchRoutine());        
    }
    
    public void StopLaunching()
    {
        _launching = false;
    }
    
    private IEnumerator LaunchRoutine()
    {
        yield return new WaitForSeconds(2);
        while (_launching)
        {
            yield return new WaitForSeconds(secondsDelay);
            var go = Instantiate(ballPrefab, transform.position, transform.rotation);
            var ballHandle = go.GetComponent<ThrowHandle>();

            if (despawnAfterSeconds) ballHandle.onDetachFromHand += () => AttachDeactivateScript(go);

            lab.SetThrowableConfig(ballHandle);
            ballHandle.gameObject.SetActive(false);
            ballHandle.transform.position = ballSpawnPoint.transform.position;
            ballHandle.transform.rotation = ballSpawnPoint.transform.rotation;
            ballHandle.gameObject.SetActive(true);
            go.GetComponent<Rigidbody>().AddForce(ballSpawnPoint.transform.forward * launchForce);
        }
    }
    private void AttachDeactivateScript(GameObject go)
    {
        var delay = go.AddComponent<SetDeactiveAfterSeconds>();
        delay.delaySeconds = 8f;
        delay.StartCoroutine(delay.DeactivateAfterSeconds());
    }
    // Update is called once per frame
    void Update()
    {
    }
}