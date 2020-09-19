using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace Tsubasa.Services.Music_Services
{
    public class SpotifyService
    {
        //the spotify client likely via Dependency Injection
        private readonly SpotifyClient _spotifyClient;
        private readonly YoutubeScraperService _youtubeScraper;

        //Constructor for spotify client
        public SpotifyService(SpotifyClient spotifyClient, YoutubeScraperService youtubeScraper)
        {
            _spotifyClient = spotifyClient;
            _youtubeScraper = youtubeScraper;
        }

        /// <summary>
        ///     Get a playlist from spotify using it's identifier
        /// </summary>
        /// <param name="playlistId">the id of the spotify playlist</param>
        /// <returns>A list of FullTracks from the playlist</returns>
        private async Task<List<FullTrack>> GetTracksFromPlaylistAsync(string playlistId)
        {

            //get the playlist
            var playlist = await _spotifyClient.Playlists.GetItems(playlistId);

            //create a list for the parsed tracks to go into
            var fullTracks = new List<FullTrack>();
            
            //check if the playlist has no items
            if (playlist.Items == null)
            {
                return fullTracks;
            }
            
            //iterate through all items in the playlist
            foreach (var item in playlist.Items)
            {
                //if track is a full track, consider it so
                if (item.Track is FullTrack track)
                {
                    //add the tracks to the full tracks list
                    fullTracks.Add(track);
                }
            }
            
            return fullTracks;
        }

        /// <summary>
        /// Converts a list of tracks to a list of queries for user with the youtube scraper
        /// </summary>
        /// <param name="tracks">List of FullTracks</param>
        /// <returns>List of strings with appropriate youtube queries for the tracks</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private async Task<List<string>> TracksToQueriesAsync(List<FullTrack> tracks)
        {
            //Run the task on a background thread and then save it to query list, then return it
            var queryList = await Task.Run(() =>
            {
                //make sure tracks does not come in as null
                if (tracks == null) throw new ArgumentNullException(nameof(tracks));

                //create list for queries
                var queries = tracks.Select(track => $"{track.Name} {track.Artists[0].Name}").ToList();

                return queries;
            });

            return queryList;
        }

        public async Task<List<Task<string>>> PlaylistToYoutubeAsync(string playlistId)
        {
            var sw = new Stopwatch();
            sw.Start();
            //List of queries from the spotify playlist
            var queries = await TracksToQueriesAsync(await GetTracksFromPlaylistAsync(playlistId).ConfigureAwait(false)).ConfigureAwait(false);

            //create list of tasks 
            var conversionTasks = queries.Select(query => _youtubeScraper.ScrapeResultPageAsync(query)).ToList();

            return conversionTasks;
        }
    }
}