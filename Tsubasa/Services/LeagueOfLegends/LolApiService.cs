using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tsubasa.Helper;

namespace Tsubasa.Services.LeagueOfLegends
{
    public class LolApiService
    {
        private readonly WebRequestService _web;
        
        /// <summary>
        /// Constructor for LolApiService, gets what it needs via DI
        /// </summary>
        /// <param name="web">A WebRequestService</param>
        public LolApiService(WebRequestService web)
        {
            _web = web;
        }
        
        public async Task<string> GetLolRankDataAsync(string username)
        {
            var raw = await _web.RequestRawAsync($"https://pacific-savannah-13382.herokuapp.com/api/lol/rankedimage?q={username}");
            
            //parse string to get json object
            var json = JObject.Parse(raw);

            return json["data"]?.ToString();
        }
    }
}