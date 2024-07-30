using System.Collections;
using System.Collections.Generic;
using CloudFine.ThrowLab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThrowConfigUi : MonoBehaviour
{
    [SerializeField] private LabManager labManager;
    [SerializeField] private GameObject togglePrefab;
    private readonly List<Toggle> _toggles = new();

    
    public void ShareConfig()
    {
        var config = ConfigurationManager.GetThrowConfiguration();
        ConfigurationAPI.ShipUpvote(ConfigurationManager.throwConfigIndex.ToString(), config.ToJson());
    }
    
    private void Start()
    {
        var configs = ConfigurationManager.GetThrowConfigurations();
        labManager.throwConfigurations = configs;
        var i = 0;
        foreach (var config in configs)
        {
            var toggleHelper = Instantiate(togglePrefab, transform).GetComponent<ToggleHelper>();
            var toggle = toggleHelper.toggle;
            
            toggle.isOn = false;
            toggleHelper.SetText(config.name);
            toggleHelper.SetIndex(i++);
            
            toggle.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    ConfigurationManager.throwConfigIndex =
                        ConfigurationManager.GetThrowConfigurations().IndexOf(config);
                    labManager.throwConfigurations = configs;
                    labManager.ChangeConfig(ConfigurationManager.throwConfigIndex);
                    foreach (var t in _toggles)
                    {
                        if (t != toggle) t.isOn = false;
                    }
                }
            });

            _toggles.Add(toggle);
        }

        labManager.Initialize();
        _toggles[0].isOn = true;
    }

    // Update is called once per frame
    void Update()
    {
    }
}