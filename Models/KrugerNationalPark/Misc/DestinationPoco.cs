using System;
using Mars.Interfaces.Environments;

namespace KrugerNationalPark.Misc
{
    public class DestinationPoco
    {
        public Poi Poi { get; set; }

        /// <summary>
        ///     Duration to reach this POI  from the origin in seconds.
        /// </summary>
        public double Duration { get; set; }

        /// <summary>
        ///     Distance to reach this POI from the origin in meters.
        /// </summary>
        public double Length { get; set; }
    }
}