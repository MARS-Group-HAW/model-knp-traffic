using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using Mars.Common;
using Mars.Components.Agents.Trips;
using Mars.Numerics;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using NetTopologySuite.Geometries;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Steering.Common;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Agents
{
    public class Tourist : IAgent<TouristLayer>, ICarSteeringCapable, ITripSavingAgent
    {
        private TouristLayer _touristLayer;
        
        public DateTime ArrivalTime;
        public DateTime DepartureTime;

        private KnpCar AnimalSighting;
        
        public int ElephantCounter { set; get; }

        private HashSet<Guid> KnownElephants = new HashSet<Guid>();
        
        public void Init(TouristLayer layer)
        {
            _touristLayer = layer;
            ElephantCounter = 0;
            State = TouristState.Driving;
            
            //Console.WriteLine("Tourist init");

            TripsCollection = new TripsCollection(layer.Context);
            
            var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
            car.Environment = layer.StreetEnvironment;
            car.TouristLayer = layer;
            
            Car = car;
            Car.Mass = MyMass;
            
            // todo: Source is a Point, no Random needed? 
            Position = SourceGeometry.RandomPositionFromGeometry();
            
            car.TryEnterDriver(this, out var handle);

            // From given MULTIPOINT Geometry get a random POINT
            // RandomPositionFromGeometry() doesnt seem random for MULTIPOINTs?!
            var target = TargetGeometry.Coordinates;
            var length = target.Length;
            Random rnd = new Random();
            var index = rnd.Next((int) length);
            var targetCor = target[index];
            var targetPos = Position.CreatePosition(targetCor.X, targetCor.Y);

            var start = layer.StreetEnvironment.NearestNode(Position);
            layer.StreetEnvironment.Insert(car, start);
            
            // for the StreetEnvironment we need an SpatialNode, not a Position. 
            // -> get nearest Node to chosen target position
            //var goal = layer.StreetEnvironment.GetRandomNode();
            var goal = layer.StreetEnvironment.NearestNode(targetPos);
            handle.Route = layer.StreetEnvironment.FindRoute(start, goal);

            VehicleHandle = handle;
            
            
            
            //_touristLayer.StreetEnvironment.Insert()
            
        }

        [PropertyDescription(Name = "source")] 
        public Geometry SourceGeometry { get; set; }

        [PropertyDescription(Name = "destination")]
        public Geometry TargetGeometry { get; set; }
        
        [PropertyDescription(Name = "my_mass")]
        public double MyMass { get; set; }
        
        public double CarVelocity { get; set; }

        /// <summary>
        /// if the agent is on its way to work (FALSE) or on the way back home (TRUE).
        /// </summary>
        public TouristState State { get; set; }

        public CarSteeringHandle VehicleHandle { get; set; }

        public void Tick()
        {
            //Console.WriteLine("Tourist position: " + Position);

            // just for debugging: what is the distance to nearest elephant:
            //var enumerable2 = _touristLayer.ElephantLayer.Environment.Explore(Position);
            //var elephant2 = enumerable2.FirstOrDefault();
            //var distanceElephant = Distance.Haversine(elephant2.Position.PositionArray, Position.PositionArray);
            //Console.WriteLine("Distance to nearest elephant:" + distanceElephant);
            
            // Look for nearest elephant for counting
            
            /* var enumerable = _touristLayer.ElephantLayer.Environment.Explore(Position, 300, 1);
            var elephant = enumerable.FirstOrDefault();
            if (elephant != null)
            {
                if (KnownElephants.Add(elephant.ID))
                {
                    ElephantCounter += 1;
                }
            } */

            var LookDuration = 15;
            
            // reaction time + halting distance: kmh/10*3 + (kmh/10)^2
            // max speed in all of KNP ist 50km/h -> we should safely brake for an object 33m ahead of us?
            var insertAnimalSightingDistanceAhead = 33.0;
            
            
            // we are driving around and wait for an anima sighting event
            if (State == TouristState.Driving)
            {
                // throw dice...
                Random rnd = new Random();

                //if (rnd.NextDouble() > 0.5)
                if (_touristLayer.Context.CurrentTick == 1400)                
                {
                    // 1. determine our position
                    var remainingDistance = VehicleHandle.RemainingDistanceOnEdge;
                    
                    // if the next intersection is closer than our break distance, 
                    // don't look for the animal and keep driving
                    // @todo: this removed the hassly of determining the next edge and position the car there,
                    //        but maybe this is better for us anyway? discuss!
                    if (remainingDistance > insertAnimalSightingDistanceAhead)
                    {
                        // 2. Create our car to force braking
                        AnimalSighting = _touristLayer.EntityManager.Create<KnpCar>("type", "Golf");
                        AnimalSighting.Environment = _touristLayer.StreetEnvironment;
                        AnimalSighting.TouristLayer = _touristLayer;

                        var edge = VehicleHandle.Route[0].Edge; // <- current edge of our car
                        
                        // 3. insert our baking trigger into the graph
                        // @todo: we should check if between our position and the pos where we insert the car the road is empty
                        // -> so we don't block an commuter ahead of us e.g.
                        _touristLayer.StreetEnvironment.Insert(AnimalSighting, edge,
                            Car.PositionOnCurrentEdge + insertAnimalSightingDistanceAhead);
                        
                        // 4. enter braking state 
                        State = TouristState.Braking;
                    }
                }
            }
            else if (State == TouristState.Braking)
            {
                if (Car.Velocity == 0)
                {
                    // we are at a stand now, start timer to remove AnimalSighting "car"
                    ArrivalTime = _touristLayer.Context.CurrentTimePoint.GetValueOrDefault();
                    DepartureTime = ArrivalTime.AddMinutes(LookDuration);
                    State = TouristState.Looking;
                }
            } else if (State == TouristState.Looking)
            {
                if (DepartureTime.Subtract(_touristLayer.Context.CurrentTimePoint.GetValueOrDefault()).Minutes < 0)
                {
                    _touristLayer.StreetEnvironment.Remove(AnimalSighting);
                    State = TouristState.Driving;
                }
            }
            
            
            // Always call Move, since braking is "handled" by the AnimalSighting car ahead
            VehicleHandle.Move(); // -> will call our HandleCustom in 

            CarVelocity = Car.Velocity;
            TripsCollection.Add(Position);
        }
        
        public Guid ID { get; set; }


        public void ElephantAhead(Elephant elephant)
        {
            if (KnownElephants.Add(elephant.ID))
            {
                ElephantCounter += 1;
            }
        }

        public Position Position
        {
            get => Car.Position;
            set => Car.Position = value;
        }
        
        public void Notify(PassengerMessage passengerMessage)
        {
            
        }

        public bool OvertakingActivated { get; }
        public Car Car { get; set; }
        public bool CurrentlyCarDriving => true;
        public int StableId { get; }
        

        
        public TripsCollection TripsCollection { get; set; }
    }
}