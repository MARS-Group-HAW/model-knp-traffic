using KrugerNationalPark.Layers;
using Mars.Interfaces.Annotations;
using SOHCarModel.Model;

namespace KrugerNationalPark.Agents;

/// <summary>
/// The <see cref="KnpCar"/> represents a standard vehicle that can be used by agents to move on the
/// <see cref="KnpRoadNetwork"/>.
/// </summary>
public class KnpCar : Car
{
    public KnpCar()
    {
        TrafficCode = "south-african";
    }

    [PropertyDescription] 
    public KnpRoadNetwork KnpRoadNetwork { get; set; }

}