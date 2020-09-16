using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Tsubasa.Helper.YoutubeSearch;

namespace Tsubasa.Helper.MusicSearch
{
    //TODO Dependency inject this maybe? static class bad says everyone online
    public static class TsubasaScraper
    {

        //TODO extend this to be able to get top 5 etc.. for selecting the song you want
        public static async Task<Video> ScrapeWithQueryAsync(string query)
        {
            //try to do scraper stuff
            try
            {
                //Get the scraped json
                var scrapedJson = await WebJson.RequestJsonAsync($"http://tsubasa-yt-scraper.herokuapp.com/api/search?q={query}");

                var videoString = "";
                foreach (var obj in scrapedJson["results"]?? throw new Exception("Results null for query"))
                {
                    //Skip this index if the video is null
                    if (obj["video"] == null) continue;
                    videoString = obj["video"].ToString();
                    break;

                }
                
                //if the length of the videostring is zero
                if (videoString.Length == 0) throw new Exception($"No matching videos for the query {query}");
                
                //get json for the first video
                var videoJson = JObject.Parse(videoString);
                
                //Get title, url, and thumbnail url
                var title = videoJson["title"]?.ToString();
                var url = videoJson["url"]?.ToString();
                var thumbnailUrl = videoJson["thumbnail_src"]?.ToString();
                
                //Return a video based on the values
                return new Video(url, title, thumbnailUrl);
            }
            catch (Exception exception)
            {
                throw new Exception("Something went wrong while parsing this request" + exception.StackTrace);
            }
        }
    }
}