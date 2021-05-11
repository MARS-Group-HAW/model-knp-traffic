using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Layers;
using Mars.Common;
using Mars.Common.IO.Mapped;
using Mars.Numerics;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using NetTopologySuite.Geometries;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Output;
using SOHDomain.Steering.Common;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Agents
{
    public class Commuter : IAgent<TouristLayer>, ICarSteeringCapable, ITripSavingAgent
    {
        private TouristLayer _touristLayer;

        public Position Origin;
        public ISpatialNode OriginNode;
        public Position Workplace;
        public ISpatialNode WorkplaceNode;

        //public bool GoalReached = false;
        
        public DateTime ArrivalTime;
        public DateTime DepartureTime;
        
        public void Init(TouristLayer layer)
        {

            GoalReached = false;
            
            _touristLayer = layer;
            TripsCollection = new TripsCollection(layer.Context);
            
            var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
            car.Environment = layer.StreetEnvironment;
            car.TouristLayer = layer;
            
            Car = car;
            Car.Mass = MyMass;
            
            // todo: Source is a Point, no Random needed? 
            Position = SourceGeometry.RandomPositionFromGeometry();
            Origin = Position;
            
            car.TryEnterDriver(this, out var handle);

            // From given MULTIPOINT Geometry get a random POINT
            // RandomPositionFromGeometry() doesnt seem random for MULTIPOINTs?!
            var target = TargetGeometry.Coordinates;
            var length = target.Length;
            Random rnd = new Random();
            var index = rnd.Next((int) length);
            var targetCor = target[index];
            var targetPos = Position.CreatePosition(targetCor.X, targetCor.Y);

    
            OriginNode = layer.StreetEnvironment.NearestNode(Position);
            
            layer.StreetEnvironment.Insert(car, OriginNode);
            
            // for the StreetEnvironment we need an SpatialNode, not a Position. 
            // -> get nearest Node to chosen target position
            //var goal = layer.StreetEnvironment.GetRandomNode();
            WorkplaceNode = layer.StreetEnvironment.NearestNode(targetPos);

            Workplace = WorkplaceNode.Position;
            
            handle.Route = layer.StreetEnvironment.FindRoute(OriginNode, WorkplaceNode);
            
            VehicleHandle = handle;
            
            
            
            
        }

        [PropertyDescription(Name = "source")] 
        public Geometry SourceGeometry { get; set; }

        [PropertyDescription(Name = "destination")]
        public Geometry TargetGeometry { get; set; }
        
        [PropertyDescription(Name = "my_mass")]
        public double MyMass { get; set; }
        
        public bool GoalReached { get; set; }

        public double CarVelocity { get; set; }
        
        public CarSteeringHandle VehicleHandle { get; set; }

        public void Tick()
        {
            var WorkDuration = 10; // in minutes
       
            if (VehicleHandle.Route.GoalReached)
            {
                // camp reached -> now work!

                // @todo: CurrentTimePoint is Nullable (`DateTime?`) so we need t make sure
                // a value is set. We copied the `new DateTime(1999, 12, 31)` from `Models/SOHMultimodalModel/Multimodal/MultimodalAgent.cs`.
                // Is this Ok? -> our simulation is timevased and wie should *never* run in to this (we think).
                
                if (!GoalReached)
                {
                    GoalReached = true;
                    
                    ArrivalTime = _touristLayer.Context.CurrentTimePoint ?? new DateTime(1999, 12, 31);
                    DepartureTime = ArrivalTime.AddMinutes(WorkDuration);
                }

                if (DepartureTime.Subtract(_touristLayer.Context.CurrentTimePoint  ?? new DateTime(1999, 12, 31) ).Minutes < 0)
                {
                    /*var car = _touristLayer.EntityManager.Create<KnpCar>("type", "Golf");
                    car.Environment = _touristLayer.StreetEnvironment;
                    car.TouristLayer = _touristLayer;
            
                    Car = car;
                    Car.Mass = MyMass;

                    var entered = car.TryEnterDriver(this, out var handle);
                    
                    handle.Route = _touristLayer.StreetEnvironment.FindRoute(WorkplaceNode, OriginNode);
            
                    VehicleHandle = handle; */

                    Console.WriteLine("we need to leave now");
                    VehicleHandle.Route = _touristLayer.StreetEnvironment.FindRoute(WorkplaceNode, OriginNode);
                }
            }
            else
            {
                VehicleHandle.Move(); // -> will call our HandleCustom in 
            }

            CarVelocity = Car.Velocity;
            TripsCollection.Add(Position); 
        }
        
        public Guid ID { get; set; }
        
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