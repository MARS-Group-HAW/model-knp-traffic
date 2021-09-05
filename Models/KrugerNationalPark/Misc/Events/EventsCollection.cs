using System;
using System.Collections.Generic;
using System.Linq;
using Mars.Interfaces;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ServiceStack.Text;

namespace KrugerNationalPark.Misc.Events
{
    public class EventsCollection
    {
        private readonly GeoJsonWriter _geoJsonWriter;

        private string fileName = "events.geojson";

        public EventsCollection()
        {
            _geoJsonWriter = new GeoJsonWriter();
        }

        public void setFileName(string f)
        {
            fileName = f;
        }
        
        public List<KnpEvent> Result { get; } = new();
        
        public void Add(KnpEvent e)
        {
            Result.Add(e);
        }

        public void TearDown()
        {
            var collection = new FeatureCollection();

            foreach (var e in Result)
            {
                var geometry = new Point(e.Position.X, e.Position.Y);

                // start|end_time have no effect on kepler.gl
                var dict = new Dictionary<string, object>
                {
                    {"creation_id", e.ID.ToString()},
                    {"event_type",  e.GetType().FullName},
                    {"radius",      e.Radius},
                    {"start_time",  (int) e.StartTime.ToUnixTime()},
                    {"end_time",    (int) e.EndTime.ToUnixTime()},
                };
                
                var f = new Feature(geometry, new AttributesTable(dict));
                collection.Add(f);
            }
            
            var json = _geoJsonWriter.Write(collection);
            System.IO.File.WriteAllText(fileName, json);
        }
    }
}