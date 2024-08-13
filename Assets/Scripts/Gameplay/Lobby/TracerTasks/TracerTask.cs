using UnityEngine;
using UnityEngine.Events;

public abstract class TracerTask : MonoBehaviour
{
    public UnityEvent taskActions;
    public abstract void Execute();
}
