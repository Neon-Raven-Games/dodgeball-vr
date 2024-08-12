using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateBotEyeLids : MonoBehaviour
{
    private float _endBlendShapeValue = 31;

    [SerializeField] private float animTime;
    [SerializeField] private float animSpeed;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private int blendShapeIndex;
    public void StartAnimatingEyes() => StartCoroutine(AnimateEyesRoutine());

    private IEnumerator AnimateEyesRoutine()
    {
        GetComponent<RobotHelper>().StartTyping(".....");
        var elapsedTime = 0f;
        while (elapsedTime < animTime)
        {
            elapsedTime += Time.deltaTime * animSpeed;
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, Mathf.Lerp(0, _endBlendShapeValue, elapsedTime));
            yield return null;
        }
        GetComponent<RobotHelper>().StartTyping("-.-");
    }

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }
}