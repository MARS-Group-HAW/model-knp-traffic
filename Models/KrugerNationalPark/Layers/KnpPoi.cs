using System.Collections.Generic;
using KrugerNationalPark.Misc;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Layers;
using System.Text.Json;

namespace KrugerNationalPark.Layers
{
    public class KnpPoi : IVectorFeature
    {

        public Position Position { get; private set; }
        
        public OriginPoco InfoList; 
        
        public VectorStructuredData VectorStructured { get; private set;  }
        public void Init(ILayer layer, VectorStructuredData data)
        {
            var centroid = data.Geometry.Centroid;
            Position = Position.CreatePosition(centroid.X, centroid.Y);
            
            VectorStructured = data;
            
            // load timings form json into structure
            // todo: this could probably be hinted directly in the GeoJSON Properties for JObject?
            var list = VectorStructured.Data["routeList"].ToString();
            InfoList = JsonSerializer.Deserialize<OriginPoco>(list);
        }
        
        
        /// <summary>
        /// 
        ///
        /// 
        /// </summary>
        /// <param name="timeLimit"></param>
        /// <returns></returns>
        
        // TODO: determine which Pois can be reached within timeLimit and return them in a list
        public List<Position> GetDestinationPoiPosition(int timeLimit)
        {
            return new List<Position>();
        }

        public void Update(VectorStructuredData data)
        {
            throw new System.NotImplementedException();
        }
    }

 
}