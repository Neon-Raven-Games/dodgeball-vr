using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DevOnStartEvent : MonoBehaviour
{
    [SerializeField] private UnityEvent onGameStart;
    private void Start() => onGameStart.Invoke();
}
