using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tsubasa.Services.Music_Services
{
    public class TsubasaSearch
    {
        private readonly SpotifyService _spotifyService;
        private readonly YoutubeScraperService _youtubeScraperService;
        

        //Create TsubasaSearch using the lavanode
        public TsubasaSearch(SpotifyService spotifyService, YoutubeScraperService youtubeScraperService)
        {
            _spotifyService = spotifyService;
            _youtubeScraperService = youtubeScraperService;
        }

        /// <summary>
        ///     Gets the url of a song to use with LavaNode.SearchAsync()
        /// </summary>
        /// <param name="query">the query to search for</param>
        /// <returns> a formatted query url</returns>
        public async Task<List<Task<string>>> GetSongUrlAsync(string query)
        {
            return await FormatQueryAsync(query).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Format a query into a usable list of track strings for tsubasa
        /// </summary>
        /// <param name="query">The query to find songs for </param>
        /// <returns>A list of strings to check in lava player</returns>
        private async Task<List<Task<string>>> FormatQueryAsync(string query)
        {
            var trackTasks = new List<Task<string>>();
            //Checks if it's a legit soundcloud url
            if (query.Contains("soundcloud.com") || query.Contains("twitch.tv"))
            {
                //add the query and return
                trackTasks.Add(Task.FromResult(query));
                return trackTasks;
            }
            //if it's a youtube playlist get the videos id
            if (query.Contains("?list=") || query.Contains("&list="))
            {
                //Directly load it here for testing
                trackTasks.Add(Task.FromResult(query));
                return trackTasks;
            }
            
            //if the query is a spotify playlist
            if (query.Contains("spotify.com/playlist/"))
            {

                //save a copy of the query and name it id
                var id = query;

                //if the query contains /playlist/ split the id off of it
                if (id.Contains("/playlist/"))
                {
                    id = id.Split("/playlist/")[1];
                }
                
                //if the url contains the si tag, split it off
                if (id.Contains("?si="))
                {
                    id = id.Split("?si=")[0];
                }
                
                //add the track tasks from the spotify search to the trackTasks list
                trackTasks.AddRange(await _spotifyService.PlaylistToYoutubeAsync(id));

                return trackTasks;
            }

            //TODO add other scrapers twitch/spotify/etc..
            //Grab the resulting youtube url
            var youtubeResult = _youtubeScraperService.ScrapeResultPageAsync(query);

            //add to track urls and push it
            trackTasks.Add(youtubeResult);
            return trackTasks;
        }
    }
}