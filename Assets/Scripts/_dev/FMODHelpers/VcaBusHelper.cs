using System;
using System.Collections;
using System.Collections.Generic;
using _dev;
using UnityEngine;
using UnityEngine.UI;

public enum VcaBusType
{
    Sfx,
    Music,
}

public class VcaBusHelper : MonoBehaviour
{
    [SerializeField] internal VcaBusType vcaBusType;
    public string vcaBusName;
    [SerializeField] private Slider slider;

    [Range(0, 1)] [SerializeField] private float defaultVolume = 0.75f;
    public float DefaultVolume => defaultVolume;

    public void UpdateSlider(float value)
    {
        if (slider != null) 
            slider.SetValueWithoutNotify(value);
    }
    
    public void Initialize(float value)
    {
        defaultVolume = value;
        Initialize();
    }

    public void Initialize()
    {
        SetVcaVolume(defaultVolume);
        slider.value = defaultVolume;
    }

    private void OnEnable()
    {
        UpdateSliderFromManager();
    }

    private void UpdateSliderFromManager()
    {
        if (vcaBusType == VcaBusType.Music) UpdateSlider(SoundManager.MusicVolume);
        if (vcaBusType == VcaBusType.Sfx) UpdateSlider(SoundManager.SfxVolume);
    }

    public float GetNormalizedVcaBusVolume() =>
        GetVcaBusVolume() / 100;

    public float GetVcaBusVolume()
    {
        FMODUnity.RuntimeManager.GetVCA($"vca:/{vcaBusName}").getVolume(out var vol);
        return vol;
    }

    public void SetNormalizedVcaVolume(float normalizedVolume) =>
        SoundManager.SetVcaBusVolume(vcaBusType, normalizedVolume * 10);

    public void SetVcaVolume(float volume)
    {
        // todo, set this up whenever we have the FMOD project
        Debug.Log("Setting " + vcaBusType + " to " + volume);
        defaultVolume = volume;
        // FMODUnity.RuntimeManager.GetVCA($"vca:/{vcaBusName}").setVolume(volume);
    }

    // FMODParameterManager.SetVcaBusVolume($"vca:/{vcaBusName}", volume);
}