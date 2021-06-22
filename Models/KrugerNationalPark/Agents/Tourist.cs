using System;
using System.Diagnostics.CodeAnalysis;
using KrugerNationalPark.Layers;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Steering.Common;

namespace KrugerNationalPark.Agents
{
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    public class Tourist : IAgent<TouristLayer>, ICarSteeringCapable
    {
        public void Init(TouristLayer layer)
        {
            
            Console.WriteLine("Tourist init");
            
            var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
            car.Environment = layer.StreetEnvironment;
            car.TouristLayer = layer;
            
            Car = car;
            Car.Mass = MyMass;

            car.TryEnterDriver(this, out var handle);

            var start = layer.StreetEnvironment.NearestNode(Position);
            layer.StreetEnvironment.Insert(car, start);
            var goal = layer.StreetEnvironment.GetRandomNode();
            handle.Route = layer.StreetEnvironment.FindRoute(start, goal);

            VehicleHandle = handle;
        }

        [PropertyDescription(Name = "my_mass")]
        public double MyMass { get; set; }

        public CarSteeringHandle VehicleHandle { get; set; }

        public void Tick()
        {
            VehicleHandle.Move();
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
    }
}