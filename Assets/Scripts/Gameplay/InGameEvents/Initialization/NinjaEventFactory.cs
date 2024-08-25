using System;
using System.Collections.Generic;
using Gameplay.InGameEvents.Ninjas;
using Hands.SinglePlayer.EnemyAI.Watchers;
using UnityEngine;

namespace Gameplay.InGameEvents.Initialization
{
    [Serializable]
    public class NinjaEventFactory : EventFactory
    {
        [SerializeField] internal List<NinjaAgent> lackeys;

        [Header("Lackey Smoke Screen Event")] 
        [SerializeField] private NinjaWatcher ninjaWatcher;
        [SerializeField] private BalanceCurve smokeScreenCurve;
        [SerializeField] internal PhaseCurve lackeyPhaseCurve;

        [Header("Boss Battle Events")] 
        [SerializeField]
        internal GameObject ninjaBoss;
        [SerializeField] private BalanceCurve bossPatternCurve;
        [SerializeField] internal PhaseCurve bossPhaseCurve;

        public override List<InGameEvent> CreateEvents(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.Lackey:
                    return CreateLackeyEvents();
                case BattlePhase.Boss:
                    return CreateBossEvents();
                default:
                    Debug.LogWarning("Unhandled BattlePhase: " + phase);
                    return new List<InGameEvent>();
            }
        }

        private List<InGameEvent> CreateLackeyEvents()
        {
            var events = new List<InGameEvent>();
            ninjaWatcher._ninjaAgents = lackeys;
            
            var smokeScreenData = new EnemyEventData
            {
                eventLevel = 1,
                balanceCurve = smokeScreenCurve
            };
            
            var smokeScreenEvent = new SmokeScreenEvent(ninjaWatcher, smokeScreenData);
            InitializeEvent(smokeScreenEvent);
            events.Add(smokeScreenEvent);
            return events;
        }

        private List<InGameEvent> CreateBossEvents()
        {
            var events = new List<InGameEvent>();
            var bossPatternData = new EnemyEventData
            {
                eventLevel = 1,
                balanceCurve = bossPatternCurve
            };
            
            var bossEvent = new NinjaBossEvent(ninjaBoss, bossPatternData);
            
            InitializeEvent(bossEvent);
            events.Add(bossEvent);

            return events;
        }

        public override void InitializeEvent(InGameEvent inGameEvent)
        {
            base.InitializeEvent(inGameEvent);

            if (inGameEvent is SmokeScreenEvent smokeScreenEvent)
            {
                smokeScreenEvent.InitializeEvent(smokeScreenEvent, smokeScreenEvent.balanceData);
            }
            else if (inGameEvent is NinjaBossEvent bossEvent)
            {
                bossEvent.InitializeEvent(bossEvent, bossEvent.balanceData);
            }
        }
    }
}