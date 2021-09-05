using Mars.Components.Services.Events;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Environments;
using SOHDomain.Steering.Capables;

namespace KrugerNationalPark.Misc.Events
{
    public abstract class EventComponent<T> : IEventComponent
    {
        public EventComponent(T entity)
        {
            Entity = entity;
        }

        public T Entity { get; set; }
        public void HandleEvent(MarsEvent marsEvent)
        {
            // please implement this method in the child class
            throw new System.NotImplementedException();
        }
    }
}