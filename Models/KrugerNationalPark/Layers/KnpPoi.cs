using System.Collections.Generic;
using KrugerNationalPark.Misc;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace KrugerNationalPark.Layers
{
    public class KnpPoi : IVectorFeature
    {

        public Position Position { get; private set; }
        
        public OriginPOCO infoList; 
        
        public VectorStructuredData VectorStructured { get; private set;  }
        public void Init(ILayer layer, VectorStructuredData data)
        {
            var centroid = data.Geometry.Centroid;
            Position = Position.CreatePosition(centroid.X, centroid.Y);
            
            VectorStructured = data;
            
            // load timings form json into structure
            // todo: this could probably be hinted directly in the GeoJSON Properties for JObject?
            var list = VectorStructured.Data["routeList"].ToString();
            infoList = JsonSerializer.Deserialize<OriginPOCO>(list);
        }
        
        
        /// <summary>
        /// 
        ///
        /// 
        /// </summary>
        /// <param name="timeLimit"></param>
        /// <returns></returns>
        
        // TODO: determine which Pois can be reached within timeLimit and return them in a list
        public List<Position> getDestinationPoiPosition(int timeLimit)
        {
            return new List<Position>();
        }

        public void Update(VectorStructuredData data)
        {
            throw new System.NotImplementedException();
        }
    }

 
}