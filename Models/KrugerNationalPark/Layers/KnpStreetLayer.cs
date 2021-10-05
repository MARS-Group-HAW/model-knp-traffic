using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Agents;
using Mars.Components.Environments;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using Mars.Interfaces.Model;
using Mars.Interfaces.Model.Options;
using SOHDomain.Graph;

namespace KrugerNationalPark.Layers
{
    /// <summary>
    ///     This class represents the agent layer implementation for the <see cref="KnpCar" /> and keeps references
    ///     to all other required layer e.g., the <see cref="ElephantLayer" />
    /// </summary>
    public class KnpStreetLayer : StreetLayer
    {
        public ISpatialGraphEnvironment StreetEnvironment { get; set; }


        public override bool InitLayer(LayerInitData layerInitData, RegisterAgent registerAgentHandle,
            UnregisterAgent unregisterAgentHandle)
        {
            base.InitLayer(layerInitData, registerAgentHandle, unregisterAgentHandle);

            StreetEnvironment = new SpatialGraphEnvironment(new SpatialGraphOptions
            {
                GraphImports = new List<Input>
                {
                    new()
                    {
                        File = layerInitData.LayerInitConfig.File,
                        InputConfiguration = new InputConfiguration
                        {
                            // TODO: wenn ich das richtig verstehe, macht das aus jeder kante 
                            // -> hin und <- rückweg
                            // wahrscheinlich brauchen wir das, für generelle straßen, nur wenn "oneway"
                            // true ist, wollen wir das nicht?!
                            IsBiDirectedImport = true,
                            Modalities = new HashSet<SpatialModalityType> {SpatialModalityType.CarDriving}
                        }
                    }
                },
                NodeIndex = true
            });


            // Export StreetLayer as GeoJSON
            //var geoJson = SpatialGraphHelper.ToGeoJson(StreetEnvironment);
            //File.WriteAllText("streetLayer.geojson", geoJson);

            return true;
        }

        /// <summary>
        ///     TODO: (?) does not take acceleration of cars into account.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <param name="timeLimit"></param>
        /// <returns></returns>
        public Route FindRoute(ISpatialNode start, ISpatialNode goal, double timeLimit)
        {
            var currentNode = start;
            var prevEdge = currentNode.OutgoingEdges.Values.ToList()[0]; // 
            var prevNode = prevEdge.From;
            var rt = new Route();

            // build route with edges until return time is reached
            do
            {
                var outEdges = currentNode.OutgoingEdges.Values.ToList();
                var outEdgesCount = outEdges.Count;

                //var newOutEdges = new List<ISpatialEdge>();

                // TODO: die lastEdge scheint keine OutGoing edge der "Nächsten" node zu sein. 
                // das ist uns unklar und nicht erwartungskonform!

                var uTurnEdges = new List<ISpatialEdge>();

                // tripTime == 0 -> erster durchlauf, keine kante entfernen
                // outEdges.Count == 1 -> kein andere option als den selben weg zurückzufahren 
                //
                // remove "returning edge" identified on the node, since the edges are uniue in each direction
                // this removal prevents u-turn behaviour of agents
                // todo: to discuss, allow u-turn on larger street segments (like >10km e.g.)
                if (rt.Count > 0 && outEdges.Count != 1)
                {
                    var newOutEdges = new List<ISpatialEdge>();

                    // find edge, leading back to the last origin
                    for (var i = 0; i < outEdgesCount; i++)
                    {
                        var e = outEdges[i];
                        if (e.To.Equals(prevNode))
                            uTurnEdges.Add(e);
                        else
                            newOutEdges.Add(e);
                    }

                    outEdges = newOutEdges;
                }

                // randomize all remaining edges to create random behaviour of agents
                // in selecting their route
                var rnd = new Random();
                outEdges = outEdges.OrderBy(item => rnd.Next()).ToList();

                // append u-turn edges as "fall back" at the end of the list
                // -> will be checked last.
                // this is necessary since the check from the previous segemnt might identified the current origin
                // as valid, but only, if we drive back the same segment we came from
                outEdges.AddRange(uTurnEdges);

                // select next route segment that adheres to time constraint
                var segmentFound = false;
                outEdgesCount = outEdges.Count; // re calculate, returning edge *might* be removed!
                for (var i = 0; i < outEdgesCount; i++)
                {
                    prevEdge = outEdges[i];


                    var edgeDuration = prevEdge.Length / prevEdge.MaxSpeed;

                    // edge leads to this node
                    // from this node we have to be able to reach out goal within the time limit
                    var targetNode = prevEdge.To;
                    var tmpRoute = StreetEnvironment.FindRoute(targetNode, goal);
                    var routeDuration = GetRouteDuration(tmpRoute);

                    if (routeDuration + edgeDuration <= timeLimit)
                    {
                        // route edge is Okay to drive on
                        rt.Add(prevEdge);
                        currentNode = prevEdge.To;
                        timeLimit -= edgeDuration;
                        prevNode = prevEdge.From;

                        segmentFound = true;
                        break;
                    }
                }

                // no valid segment for next was found -> route can't be found
                // if we do not abort, we would be stuck in a endless loop
                if (!segmentFound) throw new ArgumentException("No viable route to goal within timeLimit.");
            } while (!currentNode.Equals(goal));

            return rt;
        }

        /// <summary>
        ///     Determines the complete duration it takes to drive a route.
        /// </summary>
        /// <param name="rt"></param>
        /// <returns>duration in seconds</returns>
        private double GetRouteDuration(Route rt)
        {
            var duration = 0.0;

            if (rt is null) return duration;

            foreach (var edgeStop in rt)
            {
                var edge = edgeStop.Edge;
                // TODO: MARS method is broken (see next line for correct calculation)
                //tripTime += lastEdge.TravelTime; 
                duration += edge.Length / edge.MaxSpeed;
            }

            return duration;
        }
    }
}