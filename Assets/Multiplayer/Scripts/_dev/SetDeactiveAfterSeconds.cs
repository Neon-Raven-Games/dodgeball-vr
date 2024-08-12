using System.Collections;
using UnityEngine;

public class SetDeactiveAfterSeconds : MonoBehaviour
{
    public float delaySeconds;

    public IEnumerator DeactivateAfterSeconds()
    {
        yield return new WaitForSeconds(delaySeconds);
        gameObject.SetActive(false);
    }
}