using System;

namespace Mars.Components.Services.Events
{
    /// <summary>
    ///     thrown if an entity is not subscribed to the event type it requests to unsubscribe from
    /// </summary>
    public class EntityNotSubscribedException : InvalidOperationException
    {
        public EntityNotSubscribedException(string entityIsNotSubscribed) : base(entityIsNotSubscribed)
        {
        }
    }
}