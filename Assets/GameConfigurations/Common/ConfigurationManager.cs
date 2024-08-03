using System.Collections;
using System.Collections.Generic;
using Unity.Template.VR.Multiplayer;
using UnityEngine;

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
