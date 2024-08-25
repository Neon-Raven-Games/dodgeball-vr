using UnityEngine;

namespace Gameplay.InGameEvents.Ninjas
{
    // todo:
    // No Ball in court, target off
    // No active opponents, target off
    // Running on top of eachother
    // if ball possessed in focus, sometimes breaks
    public class NinjaLackeyPhase : PhaseEvent
    {
        public override BattlePhase phase => BattlePhase.Lackey;

        public override void StartPhase()
        {
            Debug.Log("Calling start phase");
            foreach (var lackey in phaseActors)
            {
                if (lackey.isActiveAndEnabled) continue;
                lackey.SetOutOfPlay(false);
                lackey.gameObject.SetActive(true);
            }

            UpdatePhaseData();
            foreach (var evt in events) evt.InvokeEvent();
        }

        public override void ExitPhase()
        {
            base.ExitPhase();
            foreach (var lackey in phaseActors)
            {
                lackey.SetOutOfPlay(false);
                lackey.gameObject.SetActive(false);
            }
        }

        public override void InitializeEvent(InGameEvent newEvent, EventBalanceData balanceData)
        {
        }
    }
}