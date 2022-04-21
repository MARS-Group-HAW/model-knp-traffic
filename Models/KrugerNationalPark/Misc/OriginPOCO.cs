using System.Collections.Generic;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{
    public class OriginPoco
    {
        public Poi Poi { get; set; }
        public List<DestinationPoco> Destinations { get; set; }
    }
}