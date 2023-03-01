using System;
using System.IO;
using Mars.Components.Layers;
using Mars.Interfaces.Layers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace KrugerNationalPark.Layers;

public class TrafficLookingLayer : RasterLayer, ISteppedActiveLayer
{

    public void Tick()
    {
    }

    public void PreTick()
    {
    }

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
    
    
    /// <summary>
    ///     Writes the movement heat map of Kudu agents to a GeoJSON file.
    /// </summary>
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