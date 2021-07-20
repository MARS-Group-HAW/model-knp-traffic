using System.Collections.Generic;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{
    public class DestinationPOCO
    {
        public string Name { get; set; }
        
        public string Type { get; set; }
        public Position Position { get; set; }
        
        public double Duration { get; set; }
        public double Length { get; set; }
    }
}