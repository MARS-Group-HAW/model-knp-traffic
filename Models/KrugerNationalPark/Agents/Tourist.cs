using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Layers;
using Mars.Common;
using Mars.Numerics;
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
        private TouristLayer _touristLayer;

        public int ElephantCounter { set; get; }

        private HashSet<Guid> KnownElephants = new HashSet<Guid>();
        
        public void Init(TouristLayer layer)
        {
            _touristLayer = layer;
            ElephantCounter = 0;
            
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
            
            
            VehicleHandle.Move(); // -> will call our HandleCustom in 
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