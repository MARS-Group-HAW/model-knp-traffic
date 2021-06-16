using System.Collections.Generic;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{

    public class RouteInfoListPOCO
    {
        public List<RouteInfoPOCO> RouteInfoList { get; set; }
    }
    
    public class RouteInfoPOCO
    {
        public Position Origin { get; set; }
        public Position Destination { get; set; }
        
        public double Duration { get; set; }
        public double Length { get; set; }
    }
}