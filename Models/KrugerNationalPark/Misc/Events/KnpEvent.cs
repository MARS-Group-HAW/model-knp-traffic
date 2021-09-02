using System;
using Mars.Components.Services.Events;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc.Events
{
    public class KnpEvent : MarsEvent
    {
        public readonly double Lat;
        public readonly double Lon;
        public readonly double Duration = 1800.0;
        public readonly DateTime StartTime;
        public readonly DateTime EndTime;

        public KnpEvent(double lat, double lon, DateTime startTime)
        {
            Lat = lat;
            Lon = lon;
            StartTime = startTime;
            EndTime = startTime.AddSeconds(Duration);
        }

        public KnpEvent(Position pos, DateTime startTime) : this(pos.Latitude, pos.Longitude, startTime)
        {
        }
    }
}