using System;
using KrugerNationalPark.Layers;
using KrugerNationalPark.Misc;
using Mars.Common;
using Mars.Interfaces.Agents;
using Mars.Interfaces.Annotations;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using NetTopologySuite.Geometries;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Steering.Common;
using Position = Mars.Interfaces.Environments.Position;

namespace KrugerNationalPark.Agents
{
    /// <summary>
    /// Commuter who starts at a gate, travels to a specified Camp for a specified duration of work.
    /// Configuration from scheduler CSV file (will only by "spawned" by CommuterSchedulingLayer.cs).
    /// </summary>
    public class Commuter : IAgent<StreetLayer>, ICarSteeringCapable
    {
        #region Properties

        public Guid ID { get; set; }
        private CommuterState State { get; set; }
        
        /// <summary>
        /// Needed fo "removing" the agent and preventing further tick() call to it.
        /// </summary>
        [PropertyDescription] 
        public UnregisterAgent UnregisterHandle { get; set; }

        private ISpatialNode _originNode;
        private ISpatialNode _workplaceNode;
        
        /// <summary>
        /// Format: WKT Point (`POINT (31.482268 -24.979422)`).
        /// </summary>
        [PropertyDescription(Name = "source")] 
        public Geometry SourceGeometry { get; set; }

        /// <summary>
        /// WKT Multipoint with variable amount of target points. On is chosen by random.
        /// Example: "MULTIPOINT (31.53493 -25.460457, 31.591958 -24.994678)
        /// 
        /// </summary>
        [PropertyDescription(Name = "destination")]
        public Geometry TargetGeometry { get; set; }

        /// <summary>
        /// Duration of work at the camp, in minutes.
        /// </summary>
        [PropertyDescription(Name = "workDuration")]
        private double WorkDuration { get; set; }
        
        /// <summary>
        /// The agent's arrival time at work, and departure time from work (each in hours)
        /// </summary>
        private DateTime _arrivalTime;
        private DateTime _departureTime;

        /// <summary>
        /// The agent's reference to the KNP traffic network
        /// </summary>
        private StreetLayer _streetLayer;

        /// <summary>
        ///  Position of our car/agent on the map.
        /// </summary>
        public Position Position
        {
            get => Car.Position;
            set => Car.Position = value;
        }

        public CarSteeringHandle VehicleHandle { get; set; }

        public bool OvertakingActivated { get; }
        public Car Car { get; set; }
        public bool CurrentlyCarDriving => true;

        public double CarVelocity { get; set; }

        #endregion

        #region Initialization

        public void Init(StreetLayer layer)
        {
            State = CommuterState.GoingToWork;
            _streetLayer = layer;

            var car = layer.EntityManager.Create<KnpCar>("type", "Golf");
            car.Environment = layer.StreetEnvironment;
            car.KnpStreetLayer = layer;
            Car = car;

            // todo: Source is a Point, no Random needed?
            Position = SourceGeometry.RandomPositionFromGeometry();

            car.TryEnterDriver(this, out var handle);

            // From given MULTIPOINT Geometry get a random POINT
            // @todo: RandomPositionFromGeometry() doesnt seem random for MULTIPOINTs?!
            var target = TargetGeometry.Coordinates;
            var length = target.Length;
            var rnd = new Random();
            var index = rnd.Next(length);
            var targetCor = target[index];
            var targetPos = Position.CreatePosition(targetCor.X, targetCor.Y);

            _originNode = layer.StreetEnvironment.NearestNode(Position);
            layer.StreetEnvironment.Insert(car, _originNode);

            // for the StreetEnvironment we need a SpatialNode, not a Position.
            // -> get nearest Node to chosen target position
            _workplaceNode = layer.StreetEnvironment.NearestNode(targetPos);

            handle.Route = layer.StreetEnvironment.FindRoute(_originNode, _workplaceNode);
            VehicleHandle = handle;
        }

        #endregion

        #region Tick

        public void Tick()
        {
            if (VehicleHandle.Route.GoalReached)
            {
                if (State == CommuterState.GoingToWork)
                {
                    // we reached our working destination
                    State = CommuterState.Working;
                    _arrivalTime = _streetLayer.Context.CurrentTimePoint.GetValueOrDefault();
                    _departureTime = _arrivalTime.AddMinutes(WorkDuration);
                }
                else if (State == CommuterState.Working && _departureTime
                    .Subtract(_streetLayer.Context.CurrentTimePoint.GetValueOrDefault()).TotalMinutes < 0)
                {
                    // finished working -> go back to origin gate
                    State = CommuterState.GoingHome;
                    VehicleHandle.Route = _streetLayer.StreetEnvironment.FindRoute(_workplaceNode, _originNode);
                }
                else if (State == CommuterState.GoingHome)
                {
                    // 1. remove car from graph
                    //_streetLayer.StreetEnvironment.Remove(Car);

                    // 2. unregister agent, it will no longer receive any tick() calls
                    // this agent reached it's goal and is no longer relevant in the sim context so we can remove it.
                    //UnregisterHandle.Invoke(_streetLayer, this);
                }
            }
            else
            {
                // agent calls its movement handle (associated with its car) to perform a movement
                VehicleHandle.Move();
                
                // TODO: can this be moved into the else block?
                CarVelocity = Car.Velocity;
            }
        }

        #endregion

        #region Methods

        public void Notify(PassengerMessage passengerMessage)
        {
        }

        #endregion
    }
}