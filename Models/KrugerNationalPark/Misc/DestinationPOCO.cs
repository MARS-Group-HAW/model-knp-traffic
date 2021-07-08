using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{
    public class DestinationPOCO
    {
        public string DestinationName { get; set; }
        
        public string DestinationCampType { get; set; }
        public Position Destination { get; set; }
        
        public double Duration { get; set; }
        public double Length { get; set; }
    }
}