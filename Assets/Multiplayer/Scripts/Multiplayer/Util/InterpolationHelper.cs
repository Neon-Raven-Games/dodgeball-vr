using System.Collections.Generic;
using System.Linq;
using FishNet;
using UnityEngine;

namespace Multiplayer.Scripts.Multiplayer.Util
{
    public static class InterpolationHelper
    {
        private const int _MAX_SYNC_DATA_COUNT = 10;
        private const uint _TICK_UPDATE_RATE = 10;
        private const float _SMOOTHING_FACTOR =  0.012f;

        public static void InterpolateSyncCollection(SortedDictionary<uint, Vector3> syncCollection,
            Transform transform, float smoothingFactor = 1f)
        {
            if (syncCollection.Count < 2) return;

            var ticks = new List<uint>(syncCollection.Keys);
            uint previousTick = ticks[^2];
            uint nextTick = ticks[^1];

            var previousValue = syncCollection[previousTick];
            var nextValue = syncCollection[nextTick];

            uint currentTick = InstanceFinder.NetworkManager.TimeManager.Tick;

            if (currentTick > nextTick)
            {
                // Extrapolate if currentTick is beyond nextTick
                float deltaTime = (currentTick - nextTick) / (float) _TICK_UPDATE_RATE;
                Vector3 velocity = (nextValue - previousValue) / (nextTick - previousTick);
                Vector3 extrapolatedValue = nextValue + velocity * deltaTime;

                transform.position = Vector3.Lerp(transform.position, extrapolatedValue, _SMOOTHING_FACTOR * smoothingFactor);
                // Debug.Log($"Extrapolated Value: {extrapolatedValue}, Velocity: {velocity}, DeltaTime: {deltaTime}");
                // todo, was this right?
                // syncCollection[nextTick] = transform.position;
            }
            else
            {
                float interpolationFactor = (float)(currentTick - previousTick) / (nextTick - previousTick);
                interpolationFactor = Mathf.Clamp(interpolationFactor, 0f, 1f); // Ensure factor is within range

                Vector3 interpolatedPosition = Vector3.Lerp(previousValue, nextValue, interpolationFactor);
                transform.position = interpolatedPosition;
            }

            ClearOldSyncData(syncCollection);
        }

        private static void ClearOldSyncData(SortedDictionary<uint, Vector3> syncCollection)
        {
            while (syncCollection.Count > _MAX_SYNC_DATA_COUNT)
                syncCollection.Remove(syncCollection.Keys.First());
        }
    }
}