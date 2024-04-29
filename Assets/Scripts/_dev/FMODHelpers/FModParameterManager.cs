using System;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

/// <summary>
/// An example of how we can use the FMODParameterManager to set parameters for studio event emitters in code.
/// </summary>
/// <example>
///  FMODParameterManager.Instance.SetIntParameter(emitter, "FS", (int) FootstepParameter.WoodStairs);
/// </example>
public enum FootstepParameter
{
    Default = 0,
    Ground = 1,
    Wall = 2,
    Player = 3,
    SelfPlayer = 4,
}

public class FMODParameterManager : MonoBehaviour
{
    /// <summary>
    /// Sets a global int parameter for FMOD.
    /// </summary>
    /// <param name="parameterName">The name of the FMOD parameter.</param>
    /// <param name="parameterValue">The value of the parameter.</param>
    public static void SetGlobalInt(string parameterName, int parameterValue) =>
        RuntimeManager.StudioSystem.setParameterByName(parameterName, parameterValue);

    /// <summary>
    /// Sets the volume of a VCA bus in FMOD, clamped between 1 and 100.
    /// </summary>
    /// <param name="vcaPath">The VCA path.</param>
    /// <param name="value">The value to set the volume, clamped between 1 and 100.</param>
    public static void SetVcaBusVolume(string vcaPath, float value) 
    {
        RuntimeManager.StudioSystem.getVCA(vcaPath, out var vca);
        value = Mathf.Clamp(value, 1, 100);
        vca.setVolume(value);
    }



    /// <summary>
    /// Sets a global float parameter for FMOD.
    /// </summary>
    /// <param name="parameterName">The name of the FMOD parameter.</param>
    /// <param name="parameterValue">The value of the parameter.</param>
    public static void SetGlobalFloat(string parameterName, float parameterValue) =>
        RuntimeManager.StudioSystem.setParameterByName(parameterName, parameterValue);

    /// <summary>
    /// Sets a float parameter for the studio event emitter in FMOD.
    /// </summary>
    /// <param name="emitter">The emitter to set the parameter for.</param>
    /// <param name="parameterName">The name of the parameter to set.</param>
    /// <param name="value">The value to set the parameter to.</param>
    public static void SetFloatParameter(StudioEventEmitter emitter, string parameterName, float value)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(emitter.EventReference
        );
        eventInstance.setParameterByName(parameterName, value);
        eventInstance.start();
        eventInstance.release();
    }
    
    /// <summary>
    /// Sets a integer parameter for the studio event emitter in FMOD.
    /// </summary>
    /// <param name="emitter">The emitter to set the parameter for.</param>
    /// <param name="parameterName">The name of the parameter to set.</param>
    /// <param name="value">The value to set the parameter to.</param> 
    public static void SetIntParameter(StudioEventEmitter emitter, string parameterName, int value)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(emitter.EventReference);
        eventInstance.setParameterByName(parameterName, value);
        eventInstance.start();
        eventInstance.release();
    }
    
    public static void SetParameter(string path, string parameterName, float value)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(path);
        eventInstance.setParameterByName(parameterName, value);
        eventInstance.start();
        eventInstance.release();
    }

    public static void SetParameter(string path, string parameterName, int value)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(path);
        eventInstance.setParameterByName(parameterName, value);
        eventInstance.start();
        eventInstance.release();
    }
}