using System;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc.Events
{
    public class SightingEvent : KnpEvent
    {
        public SightingEvent(double lat, double lon, DateTime startTime) : base(lat, lon, startTime)
        {
        }

        public SightingEvent(Position pos, DateTime startTime) : base(pos, startTime)
        {
        }
    }
}