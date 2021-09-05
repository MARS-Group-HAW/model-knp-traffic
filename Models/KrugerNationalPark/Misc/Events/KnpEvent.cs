using System;
using Mars.Components.Services.Events;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc.Events
{
    public class KnpEvent : MarsEvent
    {
        public readonly Position Position;
        public readonly double Duration = 1800.0;
        public readonly DateTime StartTime;
        public readonly DateTime EndTime;

        public double Radius = 100.0;

        public KnpEvent(double lat, double lon, DateTime startTime) : this(Position.CreatePosition(lon, lat), startTime)
        {
        }

        public KnpEvent(Position pos, DateTime startTime)
        {
            Position = pos;
            StartTime = startTime;
            EndTime = startTime.AddSeconds(Duration);
        }
    }
}