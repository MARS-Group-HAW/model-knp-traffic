using KrugerNationalPark.Layers;
using Mars.Interfaces.Annotations;
using SOHCarModel.Model;

namespace KrugerNationalPark.Agents
{
    /// <summary>
    ///     This class implements custom properties and the binding to the KNP related <see cref="KnpCarSteeringHandle" />.
    ///     Those logic is used for this custom car. Use this class in order to expect more parameters.
    /// </summary>
    public class KnpCar : Car
    {
        public KnpCar()
        {
            TrafficCode = "south-african";
        }

        [PropertyDescription] 
        public VisitorTravelerLayer VisitorTravelerLayer { get; set; }

    }
}
