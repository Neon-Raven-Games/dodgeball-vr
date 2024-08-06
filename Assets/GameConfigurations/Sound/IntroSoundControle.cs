using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroSoundControle : MonoBehaviour
{
    [SerializeField] private Toggle muteBot;
    [SerializeField] private Toggle skipIntro;
    private void OnEnable()
    {
        muteBot.isOn = ConfigurationManager.botMuted;
        skipIntro.isOn = ConfigurationManager.skipIntro;
    }
}
