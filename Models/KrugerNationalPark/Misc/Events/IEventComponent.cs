using Mars.Components.Services.Events;

namespace KrugerNationalPark.Misc.Events
{
    public interface IEventComponent
    {
        public void HandleEvent(MarsEvent marsEvent);
    }
}