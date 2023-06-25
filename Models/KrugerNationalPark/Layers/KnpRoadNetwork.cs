using System;
using System.Collections.Generic;
using System.Linq;
using KrugerNationalPark.Agents;
using KrugerNationalPark.Misc;
using Mars.Components.Environments;
using Mars.Interfaces.Environments;
using SOHDomain.Graph;

namespace KrugerNationalPark.Layers;

/// <summary>The <see cref="KnpRoadNetwork"/> holds the road network of the KNP on which agents can move.</summary>
/// <remarks>Requires appropriate vector data for initialization. Configurable via config.json.</remarks>
public class KnpRoadNetwork : SpatialGraphMediatorLayer
{

    /// <summary>
    /// Computes a travel route for a <see cref="Visitor"/> agent that satisfies the given constraints.
    /// </summary>
    /// <param name="from">Origin of the trip.</param>
    /// <param name="to">Destination of the trip.</param>
    /// <param name="timeLimit">Maximum duration of the trip.</param>
    /// <returns>The computed route encapsulated in a Route object.</returns>
    public Route FindVisitorRoute(ISpatialNode from, ISpatialNode to, double timeLimit)
    {
        var visitorAccessPermissions = new List<string> { RoadAccess.Public };
        return FindRoute(from, to, timeLimit, edge => visitorAccessPermissions.Contains((string)edge.Attributes["ACCESS"]));
    }
        
    /// <summary>
    /// Computes a travel route for a <see cref="OsvTourGuide"/> agent that satisfies the given constraints.
    /// </summary>
    /// <param name="from">Origin of the trip.</param>
    /// <param name="to">Destination of the trip.</param>
    /// <param name="timeLimit">Maximum duration of the trip.</param>
    /// <returns>The computed route encapsulated in a Route object.</returns>
    public Route FindOsvRoute(ISpatialNode from, ISpatialNode to, double timeLimit)
    {
        // at the moment it's not clear if we want to import Private marked routes or not
        var osvAccessPermissions = new List<string> {RoadAccess.Public, RoadAccess.Staff};
        return FindRoute(from, to, timeLimit, edge => osvAccessPermissions.Contains((string) edge.Attributes["ACCESS"]));
    }

    /// <summary>
    /// Computes a travel route for a <see cref="Visitor"/> agent or <see cref="OsvTourGuide"/> agent that satisfies the
    /// given constraints.
    /// </summary>
    /// <param name="start">Origin of the trip.</param>
    /// <param name="goal">Destination of the trip.</param>
    /// <param name="timeLimit">Maximum duration of the trip.</param>
    /// <param name="filter">Additional constraints on the trip.</param>
    /// <returns></returns>
    public Route FindRoute(ISpatialNode start, ISpatialNode goal, double timeLimit, Func<ISpatialEdge, bool> filter = null)
    {
        var currentNode = start;
        var prevEdge = currentNode.OutgoingEdges.Values.ToList()[0];
        var prevNode = prevEdge.From;
        var rt = new Route();

        // build route with edges until return time is reached
        do
        {
            //var outEdges = currentNode.OutgoingEdges.Values.ToList();
                
            var nextEdges = filter != null
                ? currentNode.OutgoingEdges.Where(pair => filter(pair.Value))
                : currentNode.OutgoingEdges;
                
            var outEdges = (from kvp in nextEdges where true select kvp.Value).ToList();

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
            outEdges = outEdges.OrderBy(_ => rnd.Next()).ToList();

            // append u-turn edges as "fall back" at the end of the list
            // -> will be checked last.
            // this is necessary since the check from the previous segment might identified the current origin
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
                    
                // @todo: FindRoute hat nur Filter für Attribute? Keinen Filter für Modalität?
                var tmpRoute = Environment.FindRoute(targetNode, goal, PathHeuristics.Shortest, filter);
                    
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
            // TODO Fix route computation logic so that this exception becomes unreachable.
            if (!segmentFound) throw new ArgumentException("No viable route to goal within timeLimit.");
        } while (!currentNode.Equals(goal));

        return rt;
    }

    /// <summary>Computes the duration of the given route.</summary>
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