
namespace KrugerNationalPark.Misc.Events
{
    public class SightingEvent : KnpEvent
    {
        public readonly double lat;
        public readonly double lon;

        public SightingEvent(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
        }
    }
}