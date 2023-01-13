using System;
using Mars.Interfaces.Agents;

namespace Mars.Components.Services.Events
{
    /// <summary>
    ///     the interface definition for creating an event handler in MARS
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        ///     subscribes entities to event types
        /// </summary>
        /// <param name="entity"> the subscribing entity</param>
        /// <param name="handler"> a reference to the entity-side function to be called when a relevant event occurs</param>
        /// <typeparam name="T"> a specialization of a MARS event which the <param name="entity"></param> requests to subscribe to</typeparam>
        public void RegisterHandler<T>(IEntity entity, Action<T> handler) where T : MarsEvent;

        /// <summary>
        ///     unsubscribes entities to event types
        /// </summary>
        /// <param name="entity"> the unsubscribing entity</param>
        /// <typeparam name="T"> a specialization of a MARS event from which the <param name="entity"></param> requests to unsubscribe from</typeparam>
        public void UnregisterHandler<T>(IEntity entity) where T : MarsEvent;

        /// <summary>
        ///     executes all delegates on the thread that owns the control's underlying window handle
        /// </summary>
        /// <param name="marsEvent"> a delegate that contains a method to be called in the control's thread context</param>
        public void Invoke(MarsEvent marsEvent);
    }
}