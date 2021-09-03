using System.ComponentModel;
using Mars.Components.Services.Events;
using Mars.Interfaces.Agents;

namespace KrugerNationalPark.Misc.Events
{
    public interface IEventComponent
    {
        public void HandleEvent(MarsEvent marsEvent);
    }
}