using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundConfigUi : MonoBehaviour
{
    [SerializeField] private GameObject togglePrefab;
    [SerializeField] private SoundIndex soundIndex;
    private readonly List<Toggle> _toggles = new();
    private void Start()
    {
        var configs = ConfigurationManager.GetSounds(soundIndex);
        var i = 0;
        foreach (var sound in configs)
        {
            var toggleHelper = Instantiate(togglePrefab, transform).GetComponent<ToggleHelper>();
            toggleHelper.SetSoundIndex(soundIndex);
            var toggle = toggleHelper.toggle;
            
            toggle.isOn = false;
            toggleHelper.SetText(sound.name);
            toggleHelper.SetIndex(i++);
            
            toggle.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    ConfigurationManager.SetSoundIndex(soundIndex,
                        ConfigurationManager.GetSounds(soundIndex).IndexOf(sound));

                    foreach (var t in _toggles)
                    {
                        if (t != toggle) t.isOn = false;
                    }
                }
            });

            _toggles.Add(toggle);
        }

        _toggles[0].isOn = true;
    }
}
