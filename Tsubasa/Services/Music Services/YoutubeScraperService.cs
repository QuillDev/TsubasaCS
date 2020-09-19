using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tsubasa.Helper;

namespace Tsubasa.Services.Music_Services
{
    public class YoutubeScraperService
    {

        private readonly WebRequestService _webRequestService;
        /// <summary>
        /// Constructor for YoutubeScraperService
        /// </summary>
        /// <param name="webRequestService">the webRequestService to use</param>
        public YoutubeScraperService(WebRequestService webRequestService)
        {
            _webRequestService = webRequestService;
        }
        
        /// <summary>
        /// Scrape the youtube page with the given query and get any videos from it
        /// </summary>
        /// <param name="query">The query to search for ex. Highway to Hell</param>
        /// <param name="page">THe page of responses to check</param>
        /// <returns>A string that has the first result from that page</returns>
        /// <exception cref="Exception">Throws exception if data is empty</exception>
        public async Task<string> ScrapeResultPageAsync(string query, int page = 1)
        {
            //create the url using the query and the page number
            var url = @$"https://www.youtube.com/results?q={query}&page={page}";

            var html = await _webRequestService.RequestRawAsync(url);
            
            //get data from the ytInitialData section
            var data = html.Substring(html.IndexOf("ytInitialData", StringComparison.Ordinal) + 17);
            
            //get the jsonString from the player response
            var jsonString = data.Substring(0,
                data.IndexOf("window[\"ytInitialPlayerResponse\"]", StringComparison.Ordinal) - 6);
            
            //parse the json and set it to json
            var json = JObject.Parse(jsonString);

            //Get sectionLists
            var sectionLists =
                JArray.Parse(json["contents"]?["twoColumnSearchResultsRenderer"]?["primaryContents"]?[
                        "sectionListRenderer"]?["contents"]
                    ?.ToString() ?? throw new Exception("Something broke while parsing sectionLists")).ToList();

            //if sectionLists are null, throw an error
            if (sectionLists == null)
            {
                throw new Exception("Section lists were null");
            }
                

            //filter list to only include sections where the itemSelectionRenderer is found
            var filteredList = sectionLists.Where(x => HasProperty(x, "itemSectionRenderer"));

            //Create an enumerable array for the filtered list
            var enumerable = filteredList as JToken[] ?? filteredList.ToArray();

            //Iterate through the filtered list
            foreach (var item in enumerable)
            {
                //get content from the itemSelectionRenderer
                var contents = item["itemSectionRenderer"]?["contents"];

                //if content was null, throw an exception
                if (contents == null)
                {
                    throw new Exception("Content was null");
                }
                    

                //iterate through content
                foreach (var content in contents)
                {
                    if (!HasProperty(content, "videoRenderer"))
                    {
                        continue;
                    }

                    //if the content was good, return it
                    var video = ParseVideoRendererAsync(content["videoRenderer"]).Result;

                    //if the url is valid, return it 
                    if (!string.IsNullOrEmpty(video))
                    {
                        return video;
                    }
                    
                }
            }

            return null;
        }

        private async Task<string> ParseVideoRendererAsync(JToken obj)
        {
            var video = await Task.Run(() =>
            {
                //Return an empty video if the object is null
                if (obj == null) return null;

                //use conditional access to get the title and url of the video
                //var title = obj["title"]?["runs"]?[0]?["text"]?.ToString();
                var url =
                    $"https://www.youtube.com{obj["navigationEndpoint"]?["commandMetadata"]?["webCommandMetadata"]?["url"]}"
                        .ToString();

                //return the youtube video
                return url;
            });

            return video;
        }

        /// <summary>
        ///     checks if the given json contains the property
        /// </summary>
        /// <param name="obj"> the object to check</param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private bool HasProperty(JToken obj, string propertyName)
        {
            return obj.ToString().Contains($"{propertyName}");
        }
    }
}