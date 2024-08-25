
namespace Gameplay.InGameEvents
{
    public abstract class InGameEvent
    {
        public int eventLevel;
        public abstract void InitializeEvent(InGameEvent newEvent, EventBalanceData balanceData);
        public abstract void InvokeEvent();
        public abstract void SimulateEvent();
    }
}