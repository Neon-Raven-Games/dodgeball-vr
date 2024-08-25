using UnityEngine;

namespace Gameplay.InGameEvents
{
    public class EnemyEvent : InGameEvent
    {
        [SerializeField] public EnemyEventData balanceData;
        
        public override void InitializeEvent(InGameEvent newEvent, EventBalanceData balanceData)
        {
            
        }

        public override void InvokeEvent()
        {
        }

        public override void SimulateEvent()
        {
            Debug.Log("Simulating enemy event");
        }
    }
}