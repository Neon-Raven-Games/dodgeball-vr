using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class RobotHelper : Actor
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private TextMeshPro screenText;
    [SerializeField] private Animator _anim;

    [SerializeField] private float textSpeed = 0.1f;
    public float lerpSpeed = 10f;
    public float amplitudeMultiplier = 100f; // Multiplier for the red channel value

    [SerializeField] private Material eyeMaterial;
    public Color minColor = Color.white; // Color when amplitude is at minimum
    public Color maxColor = Color.red;
    private float targetRedValue;

    // lmao
    private static readonly int _SHit = Animator.StringToHash("Hit");

    private int hitAnimationCount = 4;
    [SerializeField] private List<RobotTextAnimation> textChanges;
    private int textIndex = 0;

    public void StartTyping(string text) => StartCoroutine(TypeWriterEffect(text));

    private IEnumerator TypeWriterEffect(string text)
    {
        // lets make the typewriter effect
        screenText.text = "";
        var index = 0;
        while (index < text.Length)
        {
            screenText.text += text[index++];
            yield return new WaitForSeconds(textSpeed);
        }

        _changingText = false;
    }

    // Update is called once per frame
    private bool _changingText;
    private static readonly int _SHitNum = Animator.StringToHash("HitNum");

    [SerializeField] private List<string> ouchiesText;

    internal override void SetOutOfPlay(bool value)
    {
        Debug.Log("Setting out of play");
        base.SetOutOfPlay(false);
        _anim.SetInteger(_SHitNum, Random.Range(0, hitAnimationCount));
        _anim.SetTrigger(_SHit);
        outOfPlay = false;
        if (!_changingText) StartCoroutine(TypeWriterEffect(ouchiesText[Random.Range(0, ouchiesText.Count)]));
    }

    private float targetLerpValue;
    private void Update()
    {
        if (audioSource.isPlaying)
        {
            var spectrumData = new float[256];
            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.Rectangular);

            // Use the average of the first few spectrum values to determine the amplitude
            var amplitude = 0f;
            for (var i = 0; i < 5; i++) amplitude += spectrumData[i];
            amplitude /= 5f;
            amplitude *= amplitudeMultiplier;
            targetLerpValue = Mathf.Clamp(amplitude, 0f, 1f);

            var targetColor = Color.Lerp(minColor, maxColor, targetLerpValue);
            eyeMaterial.color = Color.Lerp(eyeMaterial.color, targetColor, Time.deltaTime * lerpSpeed);
        }
        
        if (textIndex >= textChanges.Count && !audioSource.isPlaying)
        {
            textIndex = -1;
            screenText.text = "";
        }

        if (textIndex >= 0 && textIndex < textChanges.Count && audioSource.isPlaying)
        {
            if (audioSource.time > textChanges[textIndex].playAtSeconds && !_changingText)
            {
                _changingText = true;
                StartCoroutine(TypeWriterEffect(textChanges[textIndex++].text));
            }
        }
    }
}

[Serializable]
public class RobotTextAnimation
{
    [FormerlySerializedAs("ms")] public float playAtSeconds;
    public string text;
}