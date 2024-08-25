namespace Gameplay.InGameEvents.Initialization
{
    public interface IEventFactory
    {
        InGameEvent CreateEvent();
        void InitializeEvent(InGameEvent inGameEvent);
    }
}