using System.Collections.Generic;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{

    public class OriginPOCO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Position Position { get; set; }
        public List<DestinationPOCO> Destinations { get; set; }
    }
    
}