using System;

namespace Mars.Components.Services.Events
{
    /// <summary>
    ///     an exception thrown if the entity is already subscribed to a given key
    /// </summary>
    public class EntityAlreadySubscribedException : InvalidOperationException
    {
        public EntityAlreadySubscribedException(string entityAlreadySubscribed) : base(entityAlreadySubscribed)
        {
        }
    }
}