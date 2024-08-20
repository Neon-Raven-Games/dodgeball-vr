using System;
using System.Collections.Generic;
using Unity.Template.VR.Multiplayer;
using UnityEngine;
using Random = UnityEngine.Random;

public enum SoundIndex
{
    Throw,
    Hit,
    Pickup
}

public class ConfigurationManager : MonoBehaviour
{
    private static ConfigurationManager _instance;
    
    [SerializeField] private List<AudioClip> throwSounds;
    [SerializeField] private List<AudioClip> hitSounds;
    [SerializeField] private List<AudioClip> pickupSounds;
    [SerializeField] public List<ThrowConfiguration> throwConfigurations;
    
    public static int throwSoundIndex;
    public static int hitIndex;
    public static int pickupIndex;
    public static int throwConfigIndex;
    public static bool botMuted;
    public static bool skipIntro;

    public void MuteBot(bool muted)
    {
        if (botMuted == muted) return;
        botMuted = muted;
        PlayerPrefs.SetInt("BotMuted", muted ? 1 : 0);
    }
    
    public void SkipIntro(bool skip)
    {
        if (skipIntro == skip) return;
        skipIntro = skip;
        PlayerPrefs.SetInt("SkipIntro", skip ? 1 : 0);
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        botMuted = PlayerPrefs.GetInt("BotMuted", 0) == 1;
        skipIntro = PlayerPrefs.GetInt("SkipIntro", 0) == 1;
        GC.Collect(0);
    }
    
    
    public static ThrowConfiguration GetThrowConfiguration()
    {
        return _instance.throwConfigurations[throwConfigIndex];
    }
    
    public static ThrowConfiguration GetThrowConfiguration(int index)
    {
        return _instance.throwConfigurations[index];
    }
    public static List<ThrowConfiguration> GetThrowConfigurations()
    {
        return _instance.throwConfigurations;
    }
    
    public static AudioClip GetIndexedSound(SoundIndex sound)
    {
        switch (sound)
        {
            case SoundIndex.Throw:
                return _instance.throwSounds[throwSoundIndex];
            case SoundIndex.Hit:
                return _instance.hitSounds[hitIndex];
            case SoundIndex.Pickup:
                return _instance.pickupSounds[pickupIndex];
            default:
                return null;
        }
    }
    public static AudioClip GetIndexedSound(SoundIndex sound, int i)
    {
        switch (sound)
        {
            case SoundIndex.Throw:
                return _instance.throwSounds[i];
            case SoundIndex.Hit:
                return _instance.hitSounds[i];
            case SoundIndex.Pickup:
                return _instance.pickupSounds[i];
            default:
                return null;
        }
    } 
    public static AudioClip GetSound(SoundIndex index)
    {
        switch (index)
        {
            case SoundIndex.Throw:
                return _instance.throwSounds[Random.Range(0, _instance.throwSounds.Count)];
            case SoundIndex.Hit:
                return _instance.hitSounds[Random.Range(0, _instance.hitSounds.Count)];
            case SoundIndex.Pickup:
                return _instance.pickupSounds[Random.Range(0, _instance.pickupSounds.Count)];
            default:
                return null;
        }
    }

    
    public static void SetSoundIndex(SoundIndex soundIndex, int index)
    {
        switch (soundIndex)
        {
            case SoundIndex.Throw:
                throwSoundIndex = index;
                break;
            case SoundIndex.Hit:
                hitIndex = index;
                break;
            case SoundIndex.Pickup:
                pickupIndex = index;
                break;
        }
    }

    public static List<AudioClip> GetSounds(SoundIndex soundIndex)
    {
        switch (soundIndex)
        {
            case SoundIndex.Throw:
                return _instance.throwSounds;
            case SoundIndex.Hit:
                return _instance.hitSounds;
            case SoundIndex.Pickup:
                return _instance.pickupSounds;
            default:
                return null;
        } 
    }
}
