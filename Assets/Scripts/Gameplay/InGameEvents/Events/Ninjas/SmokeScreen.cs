using Hands.SinglePlayer.EnemyAI.Watchers;

namespace Gameplay.InGameEvents.Ninjas
{
    public class SmokeScreenEvent : EnemyEvent
    {
        private readonly NinjaWatcher _ninjaWatcher;

        public SmokeScreenEvent(NinjaWatcher ninjaWatcher, EnemyEventData eventData)
        {
            _ninjaWatcher = ninjaWatcher;
            balanceData = eventData;
        }

        public override void InitializeEvent(InGameEvent newEvent, EventBalanceData balanceData)
        {
            base.InitializeEvent(newEvent, balanceData);
        }

        public override void InvokeEvent()
        {
            balanceData.UpdateEventCooldownAndDuration(SmokeScreen);
        }

        private void SmokeScreen()
        {
            if (!PhaseManager.CanSpawnTeam(Team.TeamTwo)) return;
            _ninjaWatcher.smokeBombDuration = balanceData.EventDuration;
            _ninjaWatcher.SmokeBomb();
            balanceData.UpdateEventCooldownAndDuration(SmokeScreen);
        }
    }
}