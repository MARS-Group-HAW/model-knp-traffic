using System.Linq;
using Mars.Common;
using Mars.Components.Layers;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Layers
{
    public class VectorWaterLayer : VectorLayer
    {
        public Position ExploreClosestFullPotentialField(double lat, double lon, double maxDistance)
        {
            var vector = Region(new[] { lon, lat }, maxDistance).FirstOrDefault();

            return vector?.VectorStructured.Geometry.Coordinate.ToPosition();
        }
    }
}