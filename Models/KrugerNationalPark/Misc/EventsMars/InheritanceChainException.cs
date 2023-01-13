using System;

namespace Mars.Components.Services.Events
{
    /// <summary>
    ///     an exception thrown if there is no transitive inheritance chain between a given key and MarsEvent
    /// </summary>
    public class InheritanceChainException : ArgumentException
    {
        public InheritanceChainException(string isNotChildOfMarsEvent) : base(isNotChildOfMarsEvent)
        {
        }
    }
}