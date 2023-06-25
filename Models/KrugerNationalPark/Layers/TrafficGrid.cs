using System;
using System.IO;
using Mars.Components.Layers;
using Mars.Interfaces.Layers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace KrugerNationalPark.Layers;

/// <summary>The <see cref="TrafficGrid"/> tracks the movement of agents on the <see cref="KnpRoadNetwork"/>.</summary>
public class TrafficGrid : RasterLayer, ISteppedActiveLayer
{
    public void Tick()
    {
    }

    public void PreTick()
    {
    }

    /// <summary>Routine of the <see cref="TrafficGrid"/> at the end of each simulation step.</summary>
    /// <remarks>At the end of the simulation, the collected data are written to a GeoJSON file.</remarks>
    public void PostTick()
    {
        if (GetCurrentTick() % 1 == 0 ||  GetCurrentTick() == 1 || GetCurrentTick() == Context.MaxTicks)
        {
            Console.WriteLine($"{GetCurrentTick()}/{Context.MaxTicks}");
        }
        
        if (GetCurrentTick() == Context.MaxTicks)
        {
            WriteMovementHeatMapToGeoJson();
        }
    }
    
    
    /// <summary>Writes the traffic heatmap to a GeoJSON file.</summary>
    private void WriteMovementHeatMapToGeoJson()
    {
        var featureCollection = new FeatureCollection();
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(4326);

        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var gridCellValue = this[x, y];
                
                if (gridCellValue == NoDataValue)
                {
                    continue;
                }
                
                if (gridCellValue == 0)
                {
                    continue;
                }
                
                // p4      p3
                // + ---- +
                // |      |
                // |      |
                // + ---- + 
                // p1      p2
                var polygon = geometryFactory.CreatePolygon(new[] {
                    new Coordinate(LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y), // p1
                    new Coordinate(LowerLeft.X + CellWidth * x + CellWidth, LowerLeft.Y + CellHeight * y), // p2
                    new Coordinate(LowerLeft.X + CellWidth * x + CellWidth, LowerLeft.Y + CellHeight * y + CellHeight), // p3
                    new Coordinate( LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y + CellHeight), // p4
                    new Coordinate(LowerLeft.X + CellWidth * x, LowerLeft.Y + CellHeight * y), // p1
                });
                var attributesTable = new AttributesTable { { "density", gridCellValue } };
                featureCollection.Add(new Feature(polygon, attributesTable));
            }
        }
        
        var featureCollectionAsGeoJson = new GeoJsonWriter().Write(featureCollection);
        File.WriteAllText($"{GetType().Name}.geojson", featureCollectionAsGeoJson);
    }
}