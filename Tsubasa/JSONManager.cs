using System.IO;
using Newtonsoft.Json.Linq;

namespace Tsubasa
{
    public class JSONManager
    {
        /// <summary>
        ///     Reads json object from a file
        /// </summary>
        /// <param name="path">path to the file</param>
        /// <returns>the json from the file</returns>
        public JObject readJSON(string path)
        {
            var json = JObject.Parse(File.ReadAllText(path));

            return json;
        }
    }
}