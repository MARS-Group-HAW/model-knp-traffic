using System.Collections.Generic;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{
    public class OriginPOCO
    {
        public Poi Poi { get; set; }
        public List<DestinationPOCO> Destinations { get; set; }
    }
}