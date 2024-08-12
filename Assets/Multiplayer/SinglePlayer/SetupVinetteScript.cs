using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class SetupVignetteScript : MonoBehaviour
{
    public Camera vrCamera;
    public LayerMask postProcessLayerMask;

    void Start()
    {
        // Create and configure the post-processing volume
        GameObject postProcessVolumeGO = new GameObject("PostProcessingVolume");
        PostProcessVolume volume = postProcessVolumeGO.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;

        PostProcessProfile profile = PostProcessProfile.CreateInstance<PostProcessProfile>();
        volume.profile = profile;

        // Add Vignette effect
        Vignette vignette;
        if (!profile.TryGetSettings(out vignette))
        {
            vignette = profile.AddSettings<Vignette>();
        }

        vignette.mode.Override(VignetteMode.Classic);
        vignette.color.Override(Color.black);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.8f);
        vignette.roundness.Override(1f);
        vignette.rounded.Override(true);

        // Add Post-process layer to the VR camera
        PostProcessLayer postProcessLayer = vrCamera.gameObject.AddComponent<PostProcessLayer>();
        postProcessLayer.volumeLayer = postProcessLayerMask;
        postProcessLayer.Init(new PostProcessResources());
        postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        postProcessLayer.fog.enabled = false;
        postProcessLayer.volumeTrigger = vrCamera.transform;
    }
}