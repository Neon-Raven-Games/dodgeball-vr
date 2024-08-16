using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MaterialPropertyColors
{
    public Color originalAlbedoColor;
    public Color originalRimColor;
    public float originalSmoothness;
}

public class ColorLerp : MonoBehaviour
{
    [SerializeField] private int playerBodyMaterialIndex;
    // todo, pool materials for better performance
    [SerializeField] private Renderer[] renderers;
    private Material[] _materialCopies;
    [SerializeField] private Color targetColor;
    
    [HideInInspector] [SerializeField] internal float lerpValue;

    private MaterialPropertyColors[] _originalColors;
    private float _previousLerpValue;
    private const float _TARGET_SMOOTHNESS = 1f;
    private static readonly int _SMoothness = Shader.PropertyToID("_Smoothness");
    private static readonly int _SRimColor = Shader.PropertyToID("_RimColor");

    private bool _initialized;
    public event Action onMaterialsLoaded;

    private async void Start()
    {
        await InitializeMaterialsAsync();
        _previousLerpValue = lerpValue;
        _initialized = true;
        onMaterialsLoaded?.Invoke();
    }

    private async UniTask InitializeMaterialsAsync()
    {
        int totalMaterials = 0;
        foreach (var rend in renderers) totalMaterials += rend.sharedMaterials.Length;
        if (totalMaterials == 0)
        {
            Debug.LogWarning($"There were zero renderers assigned in color lerp for {gameObject.name}. " +
                             $"Please assign renderers or remove this script.");
            return;
        }
        
        _materialCopies = new Material[totalMaterials];
        _originalColors = new MaterialPropertyColors[totalMaterials];
        var index = 0;
        foreach (var rend in renderers)
        {
            var materials = rend.materials;

            for (var i = 0; i < materials.Length; i++, index++)
            {
                _materialCopies[index] = new Material(materials[i]);
                materials[i] = _materialCopies[index];

                _originalColors[index] = new MaterialPropertyColors
                {
                    originalAlbedoColor = _materialCopies[index].color,
                    originalRimColor = _materialCopies[index].HasProperty(_SRimColor) ? _materialCopies[index].GetColor(_SRimColor) : Color.magenta,
                    originalSmoothness = _materialCopies[index].HasProperty(_SMoothness) ? _materialCopies[index].GetFloat(_SMoothness) : -1
                };
            await UniTask.Yield();
            }
            if (rend is MeshRenderer) rend.materials = materials;
            else if (rend is SkinnedMeshRenderer) rend.sharedMaterials = materials;
            await UniTask.Yield();
        }
    }

    private void LateUpdate()
    {
        if (!_initialized) return;
        if (Mathf.Abs(lerpValue - _previousLerpValue) > Mathf.Epsilon)
        {
            LerpColors();
            _previousLerpValue = lerpValue;
        }
    }

    private void LerpColors()
    {
        for (int i = 0; i < _materialCopies.Length; i++)
        {
            // albedo is generic color
            _materialCopies[i].color = Color.Lerp(_originalColors[i].originalAlbedoColor, targetColor, lerpValue);
            
            // rim color is specific to shader
            if (_materialCopies[i].HasProperty(_SRimColor)) _materialCopies[i].SetColor(_SRimColor, Color.Lerp(_originalColors[i].originalRimColor, targetColor, lerpValue)); 
            
            // smoothness is specific to shader
            if (_originalColors[i].originalSmoothness >= 0 && _materialCopies[i].HasProperty(_SMoothness))
                _materialCopies[i].SetFloat(_SMoothness, Mathf.Lerp(_originalColors[i].originalSmoothness, _TARGET_SMOOTHNESS, lerpValue));
        }
    }

    public Material GetPlayerMaterial()
    {
        return _materialCopies[playerBodyMaterialIndex];
    }
}