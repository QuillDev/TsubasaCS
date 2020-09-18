using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tsubasa.Services.Music_Services;
using Victoria;

namespace Tsubasa.Helper.MusicSearch
{
    public class TsubasaSearch
    {
        private readonly LavaNode _lavaNode;
        private readonly SpotifyService _spotifyService;
        private readonly YoutubeScraperService _youtubeScraperService;

        //Create TsubasaSearch using the lavanode
        public TsubasaSearch(LavaNode lavaNode, SpotifyService spotifyService,
            YoutubeScraperService youtubeScraperService)
        {
            _lavaNode = lavaNode;
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
            return await FormatQueryAsync(query);
        }

        private async Task<List<Task<string>>> FormatQueryAsync(string query)
        {
            var trackTasks = new List<Task<string>>();
            //Checks if it's a legit soundcloud url
            if (query.Contains("soundcloud.com"))
            {
                //add the query and return
                trackTasks.Add(Task.FromResult(query));
                return trackTasks;
            }

            //if it's a youtube playlist get the videos id
            if (query.Contains("&list="))
            {
                //parse the id and add it to track tasks
                trackTasks.Add(Task.FromResult(query.Split("&list=")[1]));
                return trackTasks;
            }

            if (query.Contains("spotify.com/playlist/"))
            {
                //TODO Pickup here tomorrow and implement dynamic loading on long playlists!

                //save a copy of the query and name it id
                var id = query;

                //if the query contains /playlist/ split the id off of it
                if (id.Contains("/playlist/"))
                    id = id.Split("/playlist/")[1];

                //if the url contains the si tag, split it off
                if (id.Contains("?si="))
                    id = id.Split("?si=")[0];

                //check if the string is null or empty after parsing
                if (string.IsNullOrEmpty(id))
                    throw new Exception("Given id from string was invalid.");

                //add the track tasks from the spotify search to the trackTasks list
                trackTasks.AddRange(await _spotifyService.PlaylistToYoutubeAsync(id));

                return trackTasks;
            }

            //TODO add other scrapers twitch/spotify/etc..
            //Grab the resulting youtube url
            var youtubeResult = _youtubeScraperService.ScrapeResultPageAsync(query);

            //add to trackurls and push it
            trackTasks.Add(youtubeResult);
            return trackTasks;
        }
    }
}