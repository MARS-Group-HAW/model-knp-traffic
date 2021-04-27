using System;
using KrugerNationalPark.Layers;
using Mars.Common;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using NetTopologySuite.Geometries;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Output;
using SOHDomain.Steering.Common;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Agents
{
    public class Tourist : IAgent<TouristLayer>, ICarSteeringCapable, ITripSavingAgent
    {
        public void Init(TouristLayer layer)
        {
            
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
        }

        [PropertyDescription(Name = "source")] 
        public Geometry SourceGeometry { get; set; }

        [PropertyDescription(Name = "destination")]
        public Geometry TargetGeometry { get; set; }
        
        [PropertyDescription(Name = "my_mass")]
        public double MyMass { get; set; }

        public CarSteeringHandle VehicleHandle { get; set; }

        public void Tick()
        {
            VehicleHandle.Move();
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