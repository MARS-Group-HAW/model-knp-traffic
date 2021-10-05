using KrugerNationalPark.Layers;
using Mars.Interfaces.Environments;
using SOHCarModel.Model;
using SOHCarModel.Steering;
using SOHDomain.Steering.Handles;

namespace KrugerNationalPark.Agents
{
    public class KnpCarSteeringHandle : CarSteeringHandle
    {
        private readonly KnpStreetLayer _carLayer;

        public KnpCarSteeringHandle(KnpStreetLayer carLayer, ISpatialGraphEnvironment environment, Car car) : base(
            environment, car)
        {
            _carLayer = carLayer;
        }

        /// <summary>
        ///     Provides an entry point for specialized types to provide some extra logic into
        ///     the movement operation before the
        ///     <see
        ///         cref="VehicleSteeringHandle{TSteeringCapable,TPassengerCapable,TSteeringHandle,TPassengerHandle}.CalculateDrivingDistance" />
        ///     is called.
        /// </summary>
        protected override double HandleCustom(SpatialGraphExploreResult exploreResult, double deceleration)
        {
            var type = Vehicle.Driver.GetType().Name;
            return deceleration;
        }

        private double HandleWildlifeAhead(double deceleration, double speedElephantAhead, double distanceElephantAhead)
        {
            //Console.WriteLine("deceleration in: " + deceleration);

            // Calculate the full stop speed change when wildlife was detected
            var speedChange = VehicleAccelerator.CalculateSpeedChange(Vehicle.Velocity, SpeedLimit,
                distanceElephantAhead, speedElephantAhead);

            // Is used when the movement is performed
            var outv = speedChange < deceleration ? speedChange : deceleration;

            //Console.WriteLine("deceleration in: " + outv);
            return outv;
        }

        private bool IsWildlifeAhead(out double speedElephant, out double distanceElephant)
        {
            // @Thomas: Use this to define your condition when the wildlife is ahead
            /* var elephantLayer = _carLayer.ElephantLayer;
            var enumerable = elephantLayer.Environment.Explore(Vehicle.Position, 300, 1);
            
            //TODO Check for wildlife in the area by exploring elephants + rule set about how to react

            // Did we explore any elephant within 100 meter then wildlife detected
            // maybe the exploration should be within a cone of the car

            distanceElephant = -999;
            speedElephant = -999;


            var elephant = enumerable.FirstOrDefault();
            if (elephant != null)
            {
                speedElephant = 5;
                distanceElephant = Distance.Haversine(elephant.Position.PositionArray, Vehicle.Position.PositionArray);

                var driver = Vehicle.Driver;
                
                if (driver is Tourist tourist) {
                    tourist.ElephantAhead(elephant);
                }
                
                
                Console.WriteLine("Elephant ahead in: " + distanceElephant + " m");
                return true;
            } */
            distanceElephant = -999;
            speedElephant = -999;

            return false;
        }
    }
}