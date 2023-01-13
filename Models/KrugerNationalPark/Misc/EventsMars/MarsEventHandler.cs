using System;
using System.Collections.Generic;
using Mars.Interfaces.Agents;

namespace Mars.Components.Services.Events
{
    /// <summary>
    ///     an event handler that stores and manages subscriptions, unsubscription, and invocations of MARS events
    /// </summary>
    public sealed class MarsEventHandler : IEventHandler
    {
        private static readonly Lazy<MarsEventHandler> lazy =
            new Lazy<MarsEventHandler>(() => new MarsEventHandler());

        public static MarsEventHandler Instance
        {
            get { return lazy.Value; }
        }

        /// <summary>
        ///     dictionary that contains event types and entities subscribed to them
        /// </summary>
        private readonly Dictionary<Type, List<Tuple<Guid, Action<MarsEvent>>>> _handlers;

        /// <summary>
        ///     upon initialization, a dictionary is created to manage event types and entities subscribed to them
        /// </summary>
        private MarsEventHandler()
        {
            _handlers = new Dictionary<Type, List<Tuple<Guid, Action<MarsEvent>>>> { };
        }

        /// <summary>
        ///     generates the inheritance chain between <param name="child"></param> and <param name="parent"></param>
        /// </summary>
        /// <param name="child"> sub-event</param>
        /// <param name="parent"> parent event</param>
        /// <returns>an enumerable containing the inheritance chain between <param name="child"></param> and <param name="parent"></param></returns>
        /// <exception cref="ArgumentNullException">
        ///     thrown if one of both input parameters are null
        /// </exception>
        private static IEnumerable<Type> GetInheritanceChain(Type child, Type parent)
        {
            _ = child ?? throw new ArgumentNullException(nameof(child));
            _ = parent ?? throw new ArgumentNullException(nameof(parent));

            for (var type = child; type != parent; type = type?.BaseType)
            {
                yield return type;
            }

            yield return parent;
        }

        /// <summary>
        ///     subscribes entities to event types
        /// </summary>
        /// <param name="entity"> the subscribing entity</param>
        /// <param name="handler"> a reference to the entity-side function to be called when a relevant event occurs</param>
        /// <typeparam name="T"> a specialization of a MARS event which the <param name="entity"></param> requests to subscribe to</typeparam>
        /// <exception cref="EntityAlreadySubscribedException">
        ///     thrown if the entity is already subscribed to <typeparam name="T"></typeparam>
        /// </exception>
        public void RegisterHandler<T>(IEntity entity, Action<T> handler) where T : MarsEvent
        {
            var type = typeof(T);
            void Wrapper(MarsEvent e) => handler(e as T);

            if (_handlers.ContainsKey(type))
            {
                if (_handlers[type].Exists(e => e.Item1.Equals(entity.ID)))
                {
                    throw new EntityAlreadySubscribedException("Entity is already subscribed to event type");
                }

                var newTuple = Tuple.Create(entity.ID, (Action<MarsEvent>)Wrapper);
                _handlers[type].Add(newTuple);
            }
            else
            {
                var emptyList = new List<Tuple<Guid, Action<MarsEvent>>>
                {
                    Tuple.Create(entity.ID, (Action<MarsEvent>)Wrapper)
                };
                _handlers.Add(type, emptyList);
            }
        }

        /// <summary>
        ///     unsubscribes entities to event types
        /// </summary>
        /// <param name="entity"> the unsubscribing entity</param>
        /// <typeparam name="T"> a specialization of a MARS event from which the <param name="entity"></param> requests to unsubscribe from</typeparam>
        /// <exception cref="ArgumentNullException">
        ///     thrown if _handlers[type] == null
        /// </exception>
        /// <exception cref="KeyNotFoundException">
        ///     thrown if the type of <typeparam name="T"></typeparam> is not a contained in _handlers
        /// </exception>
        /// <exception cref="EntityNotSubscribedException">
        ///     thrown if <param name="entity"></param> is not subscribed to the event type it requests to unsubscribe from
        /// </exception>
        public void UnregisterHandler<T>(IEntity entity) where T : MarsEvent
        {
            _ = entity ?? throw new ArgumentNullException(nameof(entity));
            var type = typeof(T);

            if (!_handlers.ContainsKey(type))
            {
                throw new KeyNotFoundException(nameof(type));
            }

            if (!_handlers[type].Exists(e => e.Item1.Equals(entity.ID)))
            {
                throw new EntityNotSubscribedException("Entity is not subscribed to event type");
            }

            _handlers[type]?.Remove(_handlers[type].Find(t => t.Item1 == entity.ID));
        }

        /// <summary>
        ///     executes all delegates on the thread that owns the control's underlying window handle
        /// </summary>
        /// <param name="marsEvent"> a delegate that contains a method to be called in the control's thread context</param>
        /// <exception cref="InheritanceChainException">
        ///     thrown if there is no transitive inheritance chain between <param name="marsEvent"></param> and MarsEvent
        /// </exception>
        public void Invoke(MarsEvent marsEvent)
        {
            _ = marsEvent ?? throw new ArgumentNullException(nameof(marsEvent));

            foreach (var type in GetInheritanceChain(marsEvent.GetType(), typeof(MarsEvent)))
            {
                if (_handlers.ContainsKey(type) && _handlers[type] != null)
                {
                    _handlers[type].ForEach(e => e.Item2.Invoke(marsEvent));
                }
            }
        }
    }
}