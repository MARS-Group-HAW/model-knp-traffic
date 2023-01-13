using System;

namespace Mars.Components.Services.Events
{
    /// <summary>
    ///     an abstract parent class of all usable events in the MARS framework
    /// </summary>
    public abstract class MarsEvent : IEvent
    {
        public Guid ID = Guid.NewGuid();
    }
}