using UnityEngine;
using UnityEngine.Serialization;

public class EyeBrowData : ScriptableObject
    {
        public Expressions expression;

        [Range(1.35f, 1.41f)]
        public float browHeight;
  
        [Range(-30f, 70f)]
        public float browRotation;

    }

public class RotateX : MonoBehaviour
{
    // 30 degrees
    [SerializeField] private float startZ;
    // -30 degrees
    [SerializeField] private float endZ;
    private float _timeStep;
    private bool _animating;
    private Transform leftTransform;
    private Transform rightTransform;
    public void Update()
    {
        if (!_animating) return;
        _timeStep += Time.deltaTime;
        Rotate(_timeStep);
        if (_timeStep >= 1)
        {
            _animating = false;
            _timeStep = 0;
        }
    }
    
    public void Rotate(float value)
    {
        var rotation = Mathf.Lerp(startZ, endZ, value);
        leftTransform.localRotation = Quaternion.Euler(0, 0, -rotation);
        leftTransform.localRotation = Quaternion.Euler(0, 0, rotation);
    }
}