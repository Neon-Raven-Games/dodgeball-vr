using UnityEngine;

public class NetIKTargetHelper : MonoBehaviour
{
    private Animator _animator;
    private Vector2 _moveInputAxis;
    private static readonly int _SYAxis = Animator.StringToHash("yAxis");
    private static readonly int _SXAxis = Animator.StringToHash("xAxis");
    
    public void Start() => _animator = GetComponent<Animator>();


    public void SetAxis(Vector2 getMoveInput)
    {
        if (getMoveInput == _moveInputAxis) return;
        _moveInputAxis = getMoveInput;
        _animator.SetInteger(_SXAxis, (int)getMoveInput.x);
        _animator.SetInteger(_SYAxis, (int)getMoveInput.y);
    }
}