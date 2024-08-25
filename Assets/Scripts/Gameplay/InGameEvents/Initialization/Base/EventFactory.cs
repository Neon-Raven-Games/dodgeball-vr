using System.Collections.Generic;

namespace Gameplay.InGameEvents.Initialization
{
    public abstract class EventFactory
    {
        public abstract List<InGameEvent> CreateEvents(BattlePhase phase);
    
        public virtual void InitializeEvent(InGameEvent inGameEvent)
        {
            // Common initialization logic, if needed
        }
    }
}