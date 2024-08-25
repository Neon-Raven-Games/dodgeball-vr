using UnityEngine;

namespace Gameplay.InGameEvents.Ninjas
{
    public class NinjaBossEvent : EnemyEvent
    {
        // this is the base class for our ninja boss events
        protected readonly GameObject ninjaBoss;
        public NinjaBossEvent(GameObject boss, EnemyEventData data)  
        {
            ninjaBoss = boss;
            balanceData = data;
        }

        public override void InvokeEvent()
        {
            Debug.Log("Invoking boss event");   
            balanceData.UpdateEventCooldownAndDuration(InvokeEvent);
        }
    }
}