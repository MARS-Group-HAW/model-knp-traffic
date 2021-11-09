using System.Collections.Generic;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{
    public class OriginPoco
    {
        public string OriginName { get; set; }
        public string OriginCampType { get; set; }
        public Position Origin { get; set; }
        public List<DestinationPoco> RouteInfoList { get; set; }
    }
}