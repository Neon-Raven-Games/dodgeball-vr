using System.Collections;
using System.Collections.Generic;
using CloudFine.ThrowLab.UI;
using UnityEngine;

public enum ThrowLabPageType
{
    VelocitySmoothing,
    Physics,
    Other
}
public class ThrowLabPage : MonoBehaviour
{
    public ThrowLabPageType pageType;

    [SerializeField] private UIThrowConfiguration throwConfiguration;
    // Start is called before the first frame update
    private void OnEnable()
    {
        var config = ConfigurationManager.GetThrowConfiguration();
        if (pageType == ThrowLabPageType.VelocitySmoothing)
        {
            throwConfiguration.InitializeVelocitySmoothing(config);
        }
        else if (pageType == ThrowLabPageType.Physics)
        {
            throwConfiguration.InitializePhysics(config);
        }
        else if (pageType == ThrowLabPageType.Other)
        {
            throwConfiguration.InitializeOther(config);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
