using System;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc.Events
{
    public class SocialMediaEvent : KnpEvent
    {
        public SocialMediaEvent(double lat, double lon, DateTime startTime) : base(lat, lon, startTime)
        {
        }

        public SocialMediaEvent(Position pos, DateTime startTime) : base(pos, startTime)
        {
        }
    }
}