using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace Tsubasa.Services.Music_Services
{
    public class MusicService
    {
        private readonly LavaNode _lavaNode;
        private readonly TsubasaSearch _tsubasaSearch;
        private readonly EmbedService _embedService;

        public MusicService(LavaNode lavaNode, TsubasaSearch tsubasaSearch, EmbedService embedService)
        {
            _lavaNode = lavaNode;
            _tsubasaSearch = tsubasaSearch;
            _embedService = embedService;
        }
        
        /// <summary>
        /// Joins the voice channel of the specified user if there are no conflicts
        /// </summary>
        /// <param name="user">A Socket Guild User to join the channel of</param>
        /// <returns>An Embed that relays how the connection went</returns>
        public async Task<Embed> JoinAsync(SocketGuildUser user)
        {
            //get the guild from the socket guild user
            IGuild guild = user.Guild;
            
            //if the voice channel is null tell the user they have to be in a voice channel
            if (user.VoiceChannel == null)
            {
                return await _embedService.CreateBasicEmbedAsync("Music Join", "You must be in a voice channel");
            }
                
            
            //if the guild already has a player tell the user who requested it
            if (_lavaNode.HasPlayer(guild))
            {
                return await _embedService.CreateBasicEmbedAsync("Music, Join",
                    "I can't join another voice channel until I'm disconnected.");
            }
                

            await _lavaNode.JoinAsync(user.VoiceChannel);

            return await _embedService.CreateBasicEmbedAsync("Music Join", $"Joined {user.VoiceChannel}");
        }
        
        /// <summary>
        /// Gets songs for the given query then queues them for the bo to play
        /// </summary>
        /// <param name="user">The user who called the command</param>
        /// <param name="query">The query to find matching songs for</param>
        /// <returns></returns>
        /// <exception cref="Exception"> Exception explaining what went wrong</exception>
        public async Task<Embed> PlayAsync(SocketGuildUser user, string query)
        {
            try
            {
                //Get the guild off the user
                IGuild guild = user.Guild;

                //if the user is not in a voice channel, tell them to join one!
                if (user.VoiceChannel == null)
                {
                    return await _embedService.CreateBasicEmbedAsync("Music Play", "You must be in a channel first!");
                }
                
                //if the guild doesn't have a player, join the person who requested the bot's voice channel
                if (!_lavaNode.HasPlayer(guild))
                {
                    await JoinAsync(user).ConfigureAwait(false);
                }

                //create list for holding tracks in
                var masterTrackList = new List<LavaTrack>();

                //TODO Add other sources (Twitch/etc..)
                //Search for the formatted song url
                var urls = await _tsubasaSearch.GetSongUrlAsync(query);
                
                //TODO this works but seems like SUPER SUPER slow
                //Do our iteration 
                while (urls.Any())
                {
                    //get a task when it's finished
                    var finished = await Task.WhenAny(urls);
                    urls.Remove(finished); //remove that task from the list

                    //search the nodes for the result
                    var response = await _lavaNode.SearchAsync(finished.Result);

                    //if we don't have a valid load status, skip
                    if (response.LoadStatus != LoadStatus.PlaylistLoaded &&
                        response.LoadStatus != LoadStatus.TrackLoaded)
                    {
                        continue;
                    } 
                    
                    //add to the master list for prettifying purposes
                    await LoadTracksAsync(response.Tracks, guild).ConfigureAwait(false);
                    
                    //append to master track list
                    masterTrackList.AddRange(response.Tracks);
                }

                //if no sounds were found, tell the user
                if (masterTrackList.Count == 0)
                {
                    return await _embedService.CreateBasicEmbedAsync("Music Player",
                        $"Couldn't load songs for the given query {query}.");
                }

                //make sure we've joined
                //TODO Seems to connect even when song fails, plz fix rito
                await JoinAsync(user).ConfigureAwait(false);
                
                //if there is only one song in the master track list give a different message saying so
                if (masterTrackList.Count == 1)
                {
                    return await _embedService.CreateBasicEmbedAsync("Music Player",
                        $"Loaded Song: {masterTrackList[0].Title}");
                }
                
                //if we're here we loaded more than 1 song
                return await _embedService.CreateBasicEmbedAsync("Music Player",
                    $"Loaded Song: {masterTrackList[0].Title} and {masterTrackList.Count - 1} others!");
            }

            catch (Exception e)
            {
                return _embedService.CreateErrorEmbed("Player Error",
                    $"Error: {e.Message}\n\nIf this looks like a bug, report it here!\nhttps://github.com/QuillDev/Tsubasa/issues");
            }
        }

        private async Task LoadTracksAsync(IEnumerable<LavaTrack> tracks, IGuild guild)
        {
            //get the player from the guild
            var player = _lavaNode.GetPlayer(guild);

            //iterate through all tracks
            foreach (var track in tracks)
            {
                //if the player is playing
                if (player.PlayerState == PlayerState.Playing)
                {
                    player.Queue.Enqueue(track);
                    continue;
                }

                //play track
                await player.PlayAsync(track);
            }
        }

        public async Task<Embed> LeaveAsync(SocketGuildUser user)
        {
            //TODO make it so just any user can't disconnect the bot
            IGuild guild = user.Guild;

            try
            {
                //Get the guild's player
                var player = _lavaNode.GetPlayer(guild);

                //if it's playing, stop playing
                if (player.PlayerState == PlayerState.Playing)
                {
                    await player.StopAsync();
                }

                var channelName = player.VoiceChannel.Name;
                await _lavaNode.LeaveAsync(user.VoiceChannel);
                return await _embedService.CreateBasicEmbedAsync("Music", $"Disconnected from {channelName}.");
            }
            catch (InvalidOperationException e)
            {
                return await _embedService.CreateBasicEmbedAsync("Leaving Music Channel", e.Message);
            }
        }

        public async Task<Embed> ListAsync(SocketGuildUser user)
        {
            //Max tracks to display in the queue
            var maxTracks = 5;

            try
            {
                var descriptionBuilder = new StringBuilder();

                var player = _lavaNode.GetPlayer(user.Guild);
                if (player == null)
                {
                    return await _embedService.CreateBasicEmbedAsync("Music Queue",
                        "Could not aquire music player.\nAre you using the music service right now?");
                }
                    
                
                //if the player is playing
                if (player.PlayerState == PlayerState.Playing)
                {
                    //if the queue count is less than one and the track is not equal to null give a now playing message
                    if (player.Queue.Count < 1 && player.Track != null)
                    {
                        return await _embedService.CreateBasicEmbedAsync($"Now Playing: {player.Track.Title}",
                            "There are no other items in the queue.");
                    }
                        

                    var trackNum = 2;
                    foreach (var track in player.Queue)
                    {
                        //if there are two tracks
                        if (trackNum == 2)
                        {
                            descriptionBuilder.Append($"Up Next: [{track.Title}]({track.Url})\n");
                            trackNum++;
                        }

                        //If the track number is between 0 and 4 then print it in the queue
                        else if (trackNum <= maxTracks)
                        {
                            descriptionBuilder.Append($"#{trackNum}: [{track.Title}]({track.Url})\n");
                            trackNum++;
                        }
                        else
                        {
                            descriptionBuilder.Append($"And {player.Queue.Count - maxTracks} others.");
                            break;
                        }
                    }


                    return await _embedService.CreateBasicEmbedAsync("Music Playlist",
                        $"Now Playing: [{player.Track?.Title}]({player.Track?.Url})\n{descriptionBuilder}");
                }
            }
            catch (Exception ex)
            {
                return await _embedService.CreateBasicEmbedAsync("Music, List", ex.Message);
            }

            return null;
        }

        public async Task<Embed> SkipTrackAsync(SocketGuildUser user)
        {
            try
            {
                //Get the guild's player
                var player = _lavaNode.GetPlayer(user.Guild);

                //if the player doesn't exist, say that we could not acquire the player
                if (player == null)
                {
                    return await _embedService.CreateBasicEmbedAsync("Music, List",
                        "Could not acquire player.\nAre you using the bot right now?");
                }
                    
                if (player.Queue.Count == 0)
                {
                    //Leave if the queue count is zero
                    await LeaveAsync(user).ConfigureAwait(false);
                    return await _embedService.CreateBasicEmbedAsync("Music Skipping",
                        "There are no songs to skip to! stopping player.");
                }

                try
                {
                    var currentTrack = player.Track;
                    await player.SkipAsync();
                    return await _embedService.CreateBasicEmbedAsync("Music Skip",
                        $"Successfully skipped {currentTrack.Title}\nNow Playing{player.Track.Title}");
                }
                catch (Exception ex)
                {
                    return await _embedService.CreateBasicEmbedAsync("Music Skipping Exception:", ex.ToString());
                }
            }
            catch (Exception ex)
            {
                return await _embedService.CreateBasicEmbedAsync("Music Skip", ex.ToString());
            }
        }
        
        /// <summary>
        /// Adjust the volume of the player 
        /// </summary>
        /// <param name="user">The user who requested the command</param>
        /// <param name="volume">The volume to set the player to</param>
        /// <returns>An embed containing information about the operation</returns>
        public async Task<Embed> VolumeAsync(SocketGuildUser user, int volume)
        {
            //check if the volume is in bounds
            if (volume >= 150 || volume <= 0)
            {
                return await _embedService.CreateBasicEmbedAsync("Music Volume", "Volume must be between 1 and 149.");
            }
                
            try
            {
                //get the player 
                var player = _lavaNode.GetPlayer(user.Guild);

                //update the volume
                await player.UpdateVolumeAsync((ushort) volume);
                
                return await _embedService.CreateBasicEmbedAsync("🔊 Music Volume", $"Volume has been set to {volume}.");
            }
            catch (InvalidOperationException ex)
            {
                return await _embedService.CreateBasicEmbedAsync("Music Volume", $"{ex.Message}",
                    "Please contact Stage in the support server if this is a recurring issue.");
            }
        }

        public async Task<Embed> Pause(SocketGuildUser user)
        {
            try
            {
                var player = _lavaNode.GetPlayer(user.Guild);
                if (player.PlayerState == PlayerState.Paused)
                {
                    await player.ResumeAsync();
                    return await _embedService.CreateBasicEmbedAsync("▶️ Music",
                        $"**Resumed:** Now Playing {player.Track.Title}");
                }

                await player.PauseAsync();
                return await _embedService.CreateBasicEmbedAsync("⏸️ Music", $"**Paused:** {player.Track.Title}");
            }
            catch (InvalidOperationException e)
            {
                return await _embedService.CreateBasicEmbedAsync("Music Play/Pause", e.Message);
            }
        }

        public async Task<Embed> SeekAsync(SocketGuildUser user, int seconds)
        {
            var player = _lavaNode.GetPlayer(user.Guild);

            var seekPoint = TimeSpan.FromSeconds(seconds);

            await player.SeekAsync(seekPoint);

            return await _embedService.CreateBasicEmbedAsync("Music Seek", $"Skipped to {seekPoint.TotalMinutes}");
        }
        
        /// <summary>
        /// Loops the current track
        /// </summary>
        /// <param name="user">SocketGuildUser who issued this command</param>
        /// <returns></returns>
        public async Task<Embed> LoopTrack(SocketGuildUser user)
        {
            return await _embedService.CreateBasicEmbedAsync("Music Loop",
                    $"This feature has been removed and will be re-added at a later date");
            }
        
        /// <summary>
        /// Get the art of the track that is currently playing 
        /// </summary>
        /// <param name="user">The user who executed the command</param>
        /// <returns>An embed containing the art</returns>
        public async Task<Embed> GetTrackArt(SocketGuildUser user)
        {
            //get the guild
            IGuild guild = user.Guild;
            
            //if the guild doesn't have a player
            if (!_lavaNode.HasPlayer(guild))
            {
                return await _embedService.CreateBasicEmbedAsync("Music Artwork", "The guild does not have a player.");
            }
            
            //get the player for the current guild
            var player = _lavaNode.GetPlayer(user.Guild);
            
            //if the bot is not playing anything say no songs are playing 
            if (player.PlayerState != PlayerState.Playing)
            {
                return await _embedService.CreateBasicEmbedAsync("Music Artwork", "No songs playing.");
            }
            
            try
            {
                //get the track
                var track = player.Track;
                var artwork = await track.FetchArtworkAsync(); //get the artwork for the track
                
                //create embed builder
                var builder = new EmbedBuilder
                {
                    ImageUrl = artwork,
                    Author = new EmbedAuthorBuilder {Name = $"{track.Title} - {track.Author}", IconUrl = artwork}
                };
                
                //add the current timestamp to the builder
                builder.WithCurrentTimestamp();
                
                return builder.Build();
            }
            catch
            {
                return await _embedService.CreateBasicEmbedAsync("Music Artwork",
                    "Exception occured while trying to retrieve artwork");
            }
        }
        
        /// <summary>
        /// Get the song that is currently playing and print an embed based on it
        /// </summary>
        /// <param name="user">The user who requested the operation</param>
        /// <returns>An embed based on what happened in the operation</returns>
        public async Task<Embed> Playing(SocketGuildUser user)
        {
            //get the guild
            IGuild guild = user.Guild;
            
            //check if the guild has a player
            if (!_lavaNode.HasPlayer(guild))
            {
                return await _embedService.CreateBasicEmbedAsync("Playing",
                    "The guild does not currently have a player");
            }
            
            //get the player
            var player = _lavaNode.GetPlayer(user.Guild);
            
            //if the guild isn't playing anything return an embed that lets them know that 
            if (player.PlayerState != PlayerState.Playing)
            {
                return await _embedService.CreateBasicEmbedAsync("Music Artwork", "No songs playing");
            }
            
            try
            {
                //get the track
                var track = player.Track;
                var artwork = await track.FetchArtworkAsync(); //get the artwork for the track
                //get runtime in seconds
                var progress = track.Position.Divide(track.Duration);
                
                //TODO calculate ticks based on resolution of the image
                var ticks = 54;
                var position = (int) (progress * ticks);

                //Create an output string using the string builder
                var output = new StringBuilder();
                
                //iterate through each index in the ticks
                for (var index = 0; index < ticks; index++)
                {
                    output.Append(index == position ? ":blue_circle:" : "-");
                }
                
                //create embed builder
                var builder = new EmbedBuilder
                {
                    ImageUrl = artwork,
                    Author = new EmbedAuthorBuilder {Name = $"{track.Title} - {track.Author}", IconUrl = artwork}
                };
                
                builder.WithCurrentTimestamp();
                builder.Description = output.ToString();
                return builder.Build();
            }
            catch
            {
                return await _embedService.CreateBasicEmbedAsync("Music Artwork",
                    "Exception occured while trying to retrieve artwork");
            }
        }
        
        /// <summary>
        /// Method that tries to grab the lyrics for the song that is currently playing
        /// </summary>
        /// <param name="user">User who called the lyrics method</param>
        /// <returns>An embed with either the lyrics or any errors that occured while retrieving them</returns>
        public async Task<Embed> Lyrics(SocketGuildUser user)
        {
            //if the player is not in that guild, let em know
            if (!_lavaNode.HasPlayer(user.Guild))
            {
                return await _embedService.CreateBasicEmbedAsync("Lyrics", "No player in guild");
            }
            
            //Get the player and the track
            var player = _lavaNode.GetPlayer(user.Guild);
            var track = player.Track;

            //If there is no song playing, tell em
            if (player.PlayerState != PlayerState.Playing)
            {
                return await _embedService.CreateBasicEmbedAsync("Lyrics", "No song is playing.");
            }
                

            try
            {
                //TODO Learn regex, this is horrifying
                //Remove parentheses bs from the title
                var withoutParentheses = track.Title.Split("(")[0];
                var withoutFeat = withoutParentheses.Split("feat")[0];
                var withoutft = withoutFeat.Split("ft")[0];
                var withoutPipe = withoutft.Split("|")[0];

                //Create a dummy track for the purpose of searching
                var dummyTrack = new LavaTrack(track.Hash, track.Id, withoutPipe, " ", track.Url, TimeSpan.Zero, 0,
                    false, false);

                //Get the lyrics from the host
                var lyrics = await dummyTrack.FetchLyricsFromOVHAsync();

                //Create embed builder for lyrics
                var embedBuilder = new EmbedBuilder();

                //if the lyrics are longer than discord supports return an embed saying that 
                if (lyrics.Length >= 6000)
                {
                    return await _embedService.CreateBasicEmbedAsync("Lyrics", "Lyrics are too long to post, blame discord.");
                }
                    

                //Calculate iterations as the lyrics length / 1000
                var iterations = (int) Math.Ceiling(lyrics.Length / 1024.0);
                
                //Iterate through the iterations and split the embed up
                for (var index = 0; index < iterations; index++)
                {
                    //Add the section
                    embedBuilder.AddField($"Section {index + 1}",
                        index == iterations - 1
                            ? lyrics.Substring(index * 1024)
                            : lyrics.Substring(index * 1024, 1024));
                }


                //add the author to the embed
                embedBuilder.Author = new EmbedAuthorBuilder
                {
                    IconUrl = await track.FetchArtworkAsync(),
                    Name = $"Lyrics for {track.Title}"
                };

                //add the current timestamp
                embedBuilder.WithCurrentTimestamp();

                //Build the embed
                return embedBuilder.Build();
            }
            catch (Exception exception)
            {
                return await _embedService.CreateBasicEmbedAsync("lyrics",
                    "Failed to retrieve lyrics by either an error, or the source doesn't have any\n" +
                    exception.Message);
            }
        }
        
        /// <summary>
        /// Catches the event when a track ends
        /// </summary>
        /// <param name="args">things that happen on track ending</param>
        /// <returns>A task after the event ends</returns>
        public async Task OnTrackFinished(TrackEndedEventArgs args)
        {
            var reason = args.Reason;
            var player = args.Player;
            var track = args.Track;

            if (!reason.ShouldPlayNext())
            { 
                return;
            }
            
            if (!player.Queue.TryDequeue(out
                var item) || !(item is {}
                nextTrack))
            {
                //Make sure the text channel exists
                if (player.TextChannel != null)
                {
                    //Say there are no more songs in the queue
                    await player.TextChannel?.SendMessageAsync("There are no more songs left in queue. Disconnecting");
                }
                    

                //Leave the voice channel without using leave async
                await _lavaNode.LeaveAsync(player.VoiceChannel);
                
                return;
            }

            await player.PlayAsync(nextTrack);

            var embed = new EmbedBuilder();
            embed.WithDescription($"**Finished Playing: `{track.Title}`\nNow Playing: `{nextTrack.Title}`**");
            embed.WithColor(Color.Red);
            await player.TextChannel.SendMessageAsync(embed: embed.Build());
            await player.TextChannel.SendMessageAsync(player.ToString());
        }
    }
}