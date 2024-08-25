using UnityEngine;

namespace Gameplay.InGameEvents.Ninjas
{
    public class NinjaBossPhase : PhaseEvent
    {
        public override BattlePhase phase => BattlePhase.Boss;
        public GameObject boss;

        public override void StartPhase()
        {
            // todo, boss intro
            SwapAiTarget(true);
            
            Debug.Log("I AM BOSS F34R M333!!");
            UpdatePhaseData();
            foreach (var evt in events) evt.InvokeEvent();
            boss.gameObject.SetActive(true);
        }

        public override void ExitPhase()
        {
            base.ExitPhase();
            SwapAiTarget(false);
            boss.gameObject.SetActive(false);
        }

        private void SwapAiTarget(bool isBoss)
        {
            foreach (var actor in phaseActors)
            {
                if (actor is DodgeballAI ai) ai.SwapActor(isBoss);
            }
        }
    }
}