using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDisableHelper : MonoBehaviour
{
    [SerializeField] private float disableTime = 1f;

    private void OnEnable()
    {
        StartCoroutine(StartDisableRoutine());
    }

    private IEnumerator StartDisableRoutine()
    {
        yield return new WaitForSeconds(disableTime);
        gameObject.SetActive(false);
    }
}
