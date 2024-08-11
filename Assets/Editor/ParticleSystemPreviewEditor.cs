using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParticleSystem))]
public class ParticleSystemPreviewEditor : Editor
{
    private bool _isPreviewing = false;

    void OnEnable()
    {
        EditorApplication.update += Update;
    }

    void OnDisable()
    {
        hasPlayed = false;
        EditorApplication.update -= Update;
    }

    private bool hasPlayed;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ParticleSystem particleSystem = (ParticleSystem)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);

        _isPreviewing = GUILayout.Toggle(_isPreviewing, "Preview", "Button");
        if (_isPreviewing)
        {
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.loop = false;
        }
        else if (!_isPreviewing && particleSystem.isPlaying)
        {
            particleSystem.Stop();
            
        }
    }

    private bool _reload = true;
    private void Update()
    {
        ParticleSystem particleSystem = (ParticleSystem)target;
        if (AnimationMode.InAnimationMode())
        {
            if (hasPlayed && !particleSystem.gameObject.activeInHierarchy)
            {
                _reload = true;
            }
            
            if (!particleSystem.isPlaying && _reload && particleSystem.gameObject.activeInHierarchy)
            {
                particleSystem.Play();
                _reload = false;
            }

            // Stop the particle system if it's done playing
            if (particleSystem.time >= particleSystem.main.duration - 0.01f)
            {
                particleSystem.Stop();
                particleSystem.time = 0;
                hasPlayed = true;
            }
        }
        else
        {
            if (particleSystem.isPlaying)
            {
                particleSystem.Stop();
            }
        }

        // SceneView.RepaintAll();
    }
}