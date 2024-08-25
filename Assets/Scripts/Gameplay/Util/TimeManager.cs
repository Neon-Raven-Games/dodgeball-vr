using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Util
{
    public static class TimerManager
    {
        public static SortedList<float, List<Action>> Timers => _STimers;
        
        private static readonly SortedList<float, List<Action>> _STimers = new();
        private const float _EPSILON = 0.0001f;

        public static void AddTimer(float time, Action action)
        {
            time += Time.time;
            
            if (_STimers.ContainsKey(time)) _STimers[time].Add(action);
            else _STimers.Add(time, new List<Action> { action });
        }

        private static void InvokeTimers()
        {
            while (_STimers.Count > 0 && _STimers.Keys[0] <= Time.time + _EPSILON)
            {
                float time = _STimers.Keys[0];
                _STimers[time].ForEach(x => x.Invoke());
                _STimers.RemoveAt(0);
            }
        }

        public static void Update() =>
            InvokeTimers();

        public static void ClearTimers() =>
            _STimers.Clear();
    }
}