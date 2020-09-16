using System.IO;
using Newtonsoft.Json.Linq;

namespace Tsubasa.DataManager
{
    public class JsonManager
    {
        /// <summary>
        ///     Read the JObject from the local JSON file
        /// </summary>
        /// <param name="path"> path to the json file </param>
        /// <returns> The JObject that resulted from the parsed json file</returns>
        public JObject readJSON(string path)
        {
            //Read the file at the given path
            var jObject = JObject.Parse(File.ReadAllText(path));
            return jObject;
        }
    }
}