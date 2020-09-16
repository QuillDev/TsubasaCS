using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json.Linq;
using Victoria;
using Victoria.EventArgs;
using Victoria.Responses.Rest;

namespace Tsubasa.Helper.MusicSearch
{
    public class TsubasaSearch
    {
        
        private readonly LavaNode _lavaNode;
        
        //Create TsubasaSearch using the lavanode
        public TsubasaSearch(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
        }
        /// <summary>
        /// Gets the url of a song to use with LavaNode.SearchAsync()
        /// </summary>
        /// <param name="query">the query to search for</param>
        /// <returns> a formatted query url</returns>
        public static async Task<List<string>> GetSongUrlAsync( string query )
        {
            return await FormatQueryAsync(query);
        }

        private static async Task<List<string>> FormatQueryAsync(string query)
        {
            //Create list for track urls
            var trackUrls = new List<string>();
            
            //Checks if it's a legit soundcloud url
            if (query.Contains("soundcloud.com") || query.Contains("youtube.com")) 
            {
                //add the query and return
                trackUrls.Add(query);
                return trackUrls;
            }
            
            
            
            if (query.Contains("spotify.com/playlist/"))
            {
                Console.WriteLine("Spotify Detected");
                //try to get the playlist id from the query
                var playlistid = query.Split("spotify.com/playlist/");
                
                //throw an exception if the string isn't formatter correctly
                if(playlistid.Length < 2)
                    throw new Exception($"Could not find an acceptable playlist id using query: {query}");
                
                //try to request data from the given playlist using the WebJson request lib
                var playlistJson = await WebJson.RequestJsonAsync($"http://tsubasa-yt-scraper.herokuapp.com/api/s2y?q={playlistid[1]}");
                
                //log jarray
                var jArray = new JArray(JArray.Parse(playlistJson["data"]?.ToString() ?? throw new Exception("Null dataset")));

                foreach (var token in jArray)
                {
                    trackUrls.Add(token["results"]?[0]?["video"]?["url"]?.ToString());
                }

                return trackUrls;
            }
            
            //TODO add other scrapers twitch/spotify/etc..
            //Grab the resulting youtube url
            var youtubeResult= await TsubasaScraper.ScrapeWithQueryAsync(query);
            
            //add to trackurls and push it
            trackUrls.Add(youtubeResult.Url);
            return trackUrls;
        }
    }
}