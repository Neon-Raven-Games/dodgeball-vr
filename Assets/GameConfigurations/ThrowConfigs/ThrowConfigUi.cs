using System.Collections.Generic;
using CloudFine.ThrowLab;
using UnityEngine;
using UnityEngine.UI;

public class ThrowConfigUi : MonoBehaviour
{
    [SerializeField] private LabManager labManager;
    [SerializeField] private GameObject togglePrefab;
    private readonly List<Toggle> _toggles = new();
    [SerializeField] private bool active;

    public List<GameObject> labPages;
    private int _labPageIndex = -1;
    public void ShareConfig()
    {
        ConfigurationAPI.ShipNewData();
    }
    
    public void ThrowLabPageIndexed(int index)
    {
        if (index == _labPageIndex) return;
        if (_labPageIndex != -1) labPages[_labPageIndex].SetActive(false);
        labPages[index].SetActive(true);
        _labPageIndex = index;
    }

    private void Start()
    {
        var configs = ConfigurationManager.GetThrowConfigurations();
        labManager.throwConfigurations = configs;
        if (!active)
        {
            labManager.Initialize();
            return;
        }
        var i = 0;
        foreach (var config in configs)
        {
            var toggleHelper = Instantiate(togglePrefab, transform).GetComponent<ToggleHelper>();
            var toggle = toggleHelper.toggle;

            toggle.isOn = false;
            toggleHelper.SetText(config.name);
            toggleHelper.SetIndex(i++);
            ConfigurationAPI.GetItemThreaded("ThrowConfig", "Configurations", config.name, toggleHelper.InitializeFromDatabase);
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
        _toggles[ConfigurationManager.throwConfigIndex].isOn = true;
    }
}