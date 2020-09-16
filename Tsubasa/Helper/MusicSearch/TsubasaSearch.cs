using System;
using System.Threading.Tasks;

namespace Tsubasa.Helper.YoutubeSearch
{
    namespace Tsubasa.Helper.YoutubeSearch
    {
        public class TsubasaSearch
        {
            public static async Task<Task> TsubasaSearchAsync(string query)
            {
                if (query.Contains("soundcloud.com")) return Task.CompletedTask;

                //TODO add other scrapers twitch/spotify/etc..
                var youtubeResult = TsubasaScraper.ScrapeWithQueryAsync(query);

                //wait for requests to complete
                await Task.WhenAll(youtubeResult);

                //TODO add the song to the queue
                Console.WriteLine(youtubeResult.Result.Title);

                return Task.CompletedTask;
            }
        }
    }
}