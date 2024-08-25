using System.Collections.Generic;
using Gameplay.InGameEvents.Initialization;
using UnityEngine;

namespace Gameplay.InGameEvents.Ninjas
{
    public class NinjaPhaseManager : PhaseManager
    {
        [SerializeField] BattlePhase battlePhase;
        public NinjaEventFactory ninjaEventFactory;

        public override void Initialize()
        {
            base.Initialize();

            var lackeyPhase = new NinjaLackeyPhase();
            lackeyPhase.phaseCurve = ninjaEventFactory.lackeyPhaseCurve;
            lackeyPhase.phaseActors = new List<Actor>();
            lackeyPhase.phaseActors.AddRange(ninjaEventFactory.lackeys);
            lackeyPhase.events = ninjaEventFactory.CreateEvents(BattlePhase.Lackey);
            phases[BattlePhase.Lackey] = lackeyPhase;

            var bossPhase = new NinjaBossPhase();
            bossPhase.boss = ninjaEventFactory.ninjaBoss;
            bossPhase.phaseActors = playerTeamAi;
            bossPhase.phaseCurve = ninjaEventFactory.bossPhaseCurve;
            bossPhase.events = ninjaEventFactory.CreateEvents(BattlePhase.Boss);
            // phases[BattlePhase.Boss] = bossPhase;
        }
    }
}