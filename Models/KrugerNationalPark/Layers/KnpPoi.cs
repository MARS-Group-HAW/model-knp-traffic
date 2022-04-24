using System;
using System.Collections.Generic;
using System.Text.Json;
using KrugerNationalPark.Misc;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;

namespace KrugerNationalPark.Layers
{
    public class KnpPoi : IVectorFeature
    {
        public OriginPoco infoList;

        public Position Position { get; private set; }

        public string Name { get; private set; }

        public string Type { get; private set; }

        public VectorStructuredData VectorStructured { get; private set; }

        public void Init(ILayer layer, VectorStructuredData data)
        {
            var centroid = data.Geometry.Centroid;
            Position = Position.CreatePosition(centroid.X, centroid.Y);

            VectorStructured = data;

            Name = VectorStructured.Data["name"].ToString();
            Type = VectorStructured.Data["type"].ToString();


            // load timings form json into structure
            // todo: this could probably be hinted directly in the GeoJSON Properties for JObject?
            var list = VectorStructured.Data["routeList"].ToString();
            infoList = JsonSerializer.Deserialize<OriginPoco>(list);
        }

        public void Update(VectorStructuredData data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="timeLimit"></param>
        /// <param name="allowedTypes"></param>
        /// <returns></returns>

        // TODO: determine which Pois can be reached within timeLimit and return them in a list
        public List<DestinationPoco> GetDestinationPois(double timeLimit, List<string> allowedTypes = null)
        {
            List<DestinationPoco> results = new();

            foreach (var d in infoList.Destinations)
            {
                // exclude POIs exceeding time limit
                if (d.Duration > timeLimit) continue;

                // if only special types are requested, search only for wanted
                if (allowedTypes != null && !allowedTypes.Contains(d.Poi.Type)) continue;

                results.Add(d);
            }

            return results;
        }
    }
}
