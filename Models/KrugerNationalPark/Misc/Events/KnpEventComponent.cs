using KrugerNationalPark.Agents;
using Mars.Components.Services.Events;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc.Events
{
    public class KnpEventComponent : EventComponent<Visitor>
    {
        private readonly EventsCollection EventsCollection;

        public KnpEventComponent(Visitor entity) : base(entity)
        {
            MarsEventHandler.Instance.RegisterHandler<KnpEvent>(entity, HandleEvent);

            EventsCollection = new EventsCollection();
            EventsCollection.setFileName("events_handled_" + entity.ID + ".geojson");
        }

        private void HandleEvent(KnpEvent e)
        {
            Entity.EventReceived += 1;
            var distance = e.Position.DistanceInMTo(Entity.Position);
            if (distance >= 500) return;

            Entity.EventPossibleRelevant += 1;
            // @todo: what number is good, or layer with probabilities?
            if (Entity.State != VisitorState.Driving) return;
            // 1. determine our position
            var remainingDistance = Entity.VehicleHandle.RemainingDistanceOnEdge;

            // if the next intersection is closer than our break distance, 
            // don't look for the animal and keep driving
            // @todo: this removed the hassle of determining the next edge and position the car there,
            //        but maybe this is better for us anyway? discuss!
            if (remainingDistance > Visitor.InsertAnimalSightingDistanceAhead)
            {
                Entity.EventHandled += 1;
                Entity.Car.Driver.BrakingActivated = true;

                // 4. enter braking state 
                Entity.State = VisitorState.Braking;

                // log event
                // todo: call write to file only after sim has finished, not always.
                EventsCollection.Add(e);
                EventsCollection.TearDown();
            }
        }
    }
}