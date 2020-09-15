using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Tsubasa
{
    public class JSONManager
    {
        
        /// <summary>
        /// Reads json object from a file
        /// </summary>
        /// <param name="path">path to the file</param>
        /// <returns>the json from the file</returns>
        public JObject readJSON(String path)
        {
            JObject json = JObject.Parse(File.ReadAllText(@path));

            return json;
        }
    }
}