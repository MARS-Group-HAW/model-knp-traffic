using System.Collections.Generic;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{

    public class OriginPOCO
    {
        public string OriginName { get; set; }
        public string OriginCampType { get; set; }
        public Position Origin { get; set; }
        public List<DestinationPOCO> RouteInfoList { get; set; }
    }
    
    public class DestinationPOCO
    {
        public string DestinationName { get; set; }
        
        public string DestinationCampType { get; set; }
        public Position Destination { get; set; }
        
        public double Duration { get; set; }
        public double Length { get; set; }
    }
}