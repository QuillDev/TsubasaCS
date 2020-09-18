using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Tsubasa.Helper;
using Tsubasa.Helper.MusicSearch;
using Tsubasa.Models;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Rest;

namespace Tsubasa.Services
{
    //TODO This in places we can https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/start-multiple-async-tasks-and-process-them-as-they-complete
    //TODO Rework the rest of this file to the standard of PlayAsync()!
    public class MusicService
    {
        private readonly LavaNode _lavaNode;


        private readonly Lazy<ConcurrentDictionary<ulong, MusicSettings>> _lazySettings =
            new Lazy<ConcurrentDictionary<ulong, MusicSettings>>();

        public MusicService(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
        }

        private ConcurrentDictionary<ulong, MusicSettings> Options => _lazySettings.Value;

        public async Task<Embed> JoinAsync(SocketGuildUser user)
        {
            IGuild guild = user.Guild;

            if (user.VoiceChannel == null)
                return await EmbedHelper.CreateBasicEmbed("Music Join", "You must be in a voice channel");

            if (_lavaNode.HasPlayer(guild))
                return await EmbedHelper.CreateBasicEmbed("Music, Join",
                    "I can't join another voice channel until I'm disconnected.");

            //Add this to the dictionary
            Options.TryAdd(user.Guild.Id, new MusicSettings
            {
                Master = user
            });

            await _lavaNode.JoinAsync(user.VoiceChannel);
            
            return await EmbedHelper.CreateBasicEmbed("Music Join", $"Joined {user.VoiceChannel}");
        }

        public async Task<Embed> PlayAsync(SocketGuildUser user, string query = null)
        {
            //TODO This needs a MAJOR Refactor. plz fix
            //Get the guild off the user
            IGuild guild = user.Guild;

            //Check if the user is not in a voice channel
            if (user.VoiceChannel == null)
                return await EmbedHelper.CreateBasicEmbed("Music Play", "You must be in a channel first!");

            //If the guild does not have a player, join the server when callig play.
            if (!_lavaNode.HasPlayer(guild)) await JoinAsync(user);

            //get the LavaPlayer for the guild the person is in
            var player = _lavaNode.GetPlayer(guild);
            
            try
            {
                //TODO Add other sources (Twitch/etc..)
                
                //Search for the formatted song url
                var urls = await TsubasaSearch.GetSongUrlAsync(query);
                
                //If the length of the url list is zero there was an error
                if (urls.Count == 0)
                    throw new Exception($"Url list came back empty for query {query}");
                
                //Search lavaplayer for tracks on all of the queries entered
                var responses = urls.Select(url => _lavaNode.SearchAsync(url)).ToList();
                
                //await all responses
                await Task.WhenAll(responses);
                
                //create a master track list to push all tracks to
                var masterTrackList = new List<LavaTrack>();
                
                //Iterate through responses TODO foreach?
                foreach (var curResponse in from t in responses
                    select t.Result
                    into curResponse
                    let resultStatus = curResponse.LoadStatus
                    where resultStatus == LoadStatus.TrackLoaded || resultStatus == LoadStatus.PlaylistLoaded
                    select curResponse)
                {
                    //Add the current tracks to the master list
                    masterTrackList.AddRange(curResponse.Tracks);
                }
                
                //if the masterTrackList is empty there was a serious problem, how about logging it!
                if(masterTrackList.Count == 0)
                    throw new Exception($"No tracks were able to load for query {query}");
                
                //Load tracks by iterating the masterTrackList
                foreach(var track in masterTrackList)
                {
                    //if the player is playing, add the track and continue
                    if (player.PlayerState == PlayerState.Playing)
                    {
                        player.Queue.Enqueue(track); 
                        continue;
                    }
                    
                    //check if the player has joined the channel
                    
                    //if the player is not playing then we can play the track
                    await player.PlayAsync(track);
                }
                
                //if there was only one song in the master track list, put a cute message about us adding it
                if (masterTrackList.Count == 1)
                    return await EmbedHelper.CreateBasicEmbed("Track Loader",
                        $"Added track {masterTrackList[0].Title} to the queue");
                
                //Otherwise, return an embed that shows the main track, and the amount of other tracks
                return await EmbedHelper.CreateBasicEmbed("Track Loader",
                    $"Added track {masterTrackList[0].Title} to the queue and {masterTrackList.Count - 1} others!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return await EmbedHelper.CreateBasicEmbed("Music, Play Exception", $"An error occured when processing your query {query} it was likely invalid, or there was a networking issue on our side!");
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
                    await player.StopAsync();

                var channelname = player.VoiceChannel.Name;
                await _lavaNode.LeaveAsync(user.VoiceChannel);
                return await EmbedHelper.CreateBasicEmbed("Music", $"Disconnected from {channelname}.");
            }
            catch (InvalidOperationException e)
            {
                return await EmbedHelper.CreateBasicEmbed("Leaving Music Channel", e.Message);
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
                    return await EmbedHelper.CreateBasicEmbed("Music Queue",
                        "Could not aquire music player.\nAre you using the music service right now?");

                if (player.PlayerState == PlayerState.Playing)
                {
                    if (player.Queue.Count < 1 && player.Track != null)
                        return await EmbedHelper.CreateBasicEmbed($"Now Playing: {player.Track.Title}",
                            "There are no other items in the queue.");

                    var trackNum = 2;
                    foreach (var track in player.Queue)
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

                    return await EmbedHelper.CreateBasicEmbed("Music Playlist",
                        $"Now Playing: [{player.Track.Title}]({player.Track.Url})\n{descriptionBuilder}");
                }
            }
            catch (Exception ex)
            {
                return await EmbedHelper.CreateBasicEmbed("Music, List", ex.Message);
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
                    return await EmbedHelper.CreateBasicEmbed("Music, List",
                        "Could not acquire player.\nAre you using the bot right now?");
                if (player.Queue.Count == 0)
                {
                    await player.StopAsync();
                    return await EmbedHelper.CreateBasicEmbed("Music Skipping",
                        "There are no songs to skip to! stopping player.");
                }

                try
                {
                    var currentTrack = player.Track;
                    await player.SkipAsync();
                    return await EmbedHelper.CreateBasicEmbed("Music Skip",
                        $"Successfully skipped {currentTrack.Title}\nNow Playing{player.Track.Title}");
                }
                catch (Exception ex)
                {
                    return await EmbedHelper.CreateBasicEmbed("Music Skipping Exception:", ex.ToString());
                }
            }
            catch (Exception ex)
            {
                return await EmbedHelper.CreateBasicEmbed("Music Skip", ex.ToString());
            }
        }

        public async Task<Embed> VolumeAsync(SocketGuildUser user, int volume)
        {
            if (volume >= 150 || volume <= 0)
                return await EmbedHelper.CreateBasicEmbed("Music Volume", "Volume must be between 1 and 149.");
            try
            {
                //get the player 
                var player = _lavaNode.GetPlayer(user.Guild);

                //update the volume
                await player.UpdateVolumeAsync((ushort) volume);
                //Ear Rape for fun
                //await player.EqualizerAsync(new EqualizerBand(5, 1.0), new EqualizerBand(6, 1.0), new EqualizerBand(7, -.1), new EqualizerBand(8, 0), new EqualizerBand(9, 0), new EqualizerBand(10, 0), new EqualizerBand(11, 0),  new EqualizerBand(12, 0), new EqualizerBand(13, 0));
                return await EmbedHelper.CreateBasicEmbed("🔊 Music Volume", $"Volume has been set to {volume}.");
            }
            catch (InvalidOperationException ex)
            {
                return await EmbedHelper.CreateBasicEmbed("Music Volume", $"{ex.Message}",
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
                    return await EmbedHelper.CreateBasicEmbed("▶️ Music",
                        $"**Resumed:** Now Playing {player.Track.Title}");
                }

                await player.PauseAsync();
                return await EmbedHelper.CreateBasicEmbed("⏸️ Music", $"**Paused:** {player.Track.Title}");
            }
            catch (InvalidOperationException e)
            {
                return await EmbedHelper.CreateBasicEmbed("Music Play/Pause", e.Message);
            }
        }

        public async Task<Embed> SeekAsync(SocketGuildUser user, int seconds)
        {
            var player = _lavaNode.GetPlayer(user.Guild);

            var seekPoint = TimeSpan.FromSeconds(seconds);

            await player.SeekAsync(seekPoint);

            return await EmbedHelper.CreateBasicEmbed("Music Seek", $"Skipped to {seekPoint.TotalMinutes}");
        }

        public async Task<Embed> LoopTrack(SocketGuildUser user)
        {
            try
            {
                //Get the option
                Options.TryGetValue(user.Guild.Id, out var option);

                //add or update values in the thing i guess fuck
                Options.TryUpdate(user.Guild.Id, new MusicSettings
                {
                    RepeatTrack = !option.RepeatTrack
                }, option);

                return await EmbedHelper.CreateBasicEmbed("Music Loop", $"Looping set to {!option.RepeatTrack}");
            }
            catch (Exception exception)
            {
                return await EmbedHelper.CreateBasicEmbed("Music Loop",
                    $"The dev fucked up. idk what this error even is{exception.Message}");
            }
        }

        public async Task<Embed> GetTrackArt(SocketGuildUser user)
        {
            var player = _lavaNode.GetPlayer(user.Guild);

            if (player.PlayerState != PlayerState.Playing)
                return await EmbedHelper.CreateBasicEmbed("Music Artwork", "No songs playing");

            try
            {
                //get the track
                var track = player.Track;
                var artwork = await track.FetchArtworkAsync(); //get the artwork for the track

                //create embed builder
                var builder = new EmbedBuilder();
                builder.ImageUrl = artwork;
                builder.Author = new EmbedAuthorBuilder
                {
                    Name = $"{track.Title} - {track.Author}",
                    IconUrl = artwork
                };
                builder.WithCurrentTimestamp();
                return builder.Build();
            }
            catch
            {
                return await EmbedHelper.CreateBasicEmbed("Music Artwork",
                    "Exception occured while trying to retrieve artwork");
            }
        }

        public async Task<Embed> Playing(SocketGuildUser user)
        {
            var player = _lavaNode.GetPlayer(user.Guild);

            if (player.PlayerState != PlayerState.Playing)
                return await EmbedHelper.CreateBasicEmbed("Music Artwork", "No songs playing");

            try
            {
                //get the track
                var track = player.Track;
                var artwork = await track.FetchArtworkAsync(); //get the artwork for the track

                //get runtime in seconds
                var progress = track.Position.Divide(track.Duration);
                var ticks = 54;
                var position = (int) (progress * ticks);

                //Output the 
                var output = new StringBuilder();
                for (var index = 0; index < ticks; index++) output.Append(index == position ? ":blue_circle:" : "-");
                //create embed builder
                var builder = new EmbedBuilder();
                builder.ImageUrl = artwork;
                builder.Author = new EmbedAuthorBuilder
                {
                    Name = $"{track.Title} - {track.Author}",
                    IconUrl = artwork
                };
                builder.WithCurrentTimestamp();
                builder.Description = output.ToString();
                return builder.Build();
            }
            catch
            {
                return await EmbedHelper.CreateBasicEmbed("Music Artwork",
                    "Exception occured while trying to retrieve artwork");
            }
        }

        public async Task<Embed> Lyrics(SocketGuildUser user)
        {
            //if the player is not in that guild, let em know
            if (!_lavaNode.HasPlayer(user.Guild))
                return await EmbedHelper.CreateBasicEmbed("Lyrics", "No player in guild");

            //Get the player and the track
            var player = _lavaNode.GetPlayer(user.Guild);
            var track = player.Track;

            //If there is no song playing, tell em
            if (player.PlayerState != PlayerState.Playing)
                return await EmbedHelper.CreateBasicEmbed("Lyrics", "No song is playing.");

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
                    return await EmbedHelper.CreateBasicEmbed("Lyrics", "Lyrics are too long to post, blame discord.");
                
                //Calculate iterations as the lyrics length / 1000
                var iterations = (int) Math.Ceiling(lyrics.Length / 1000.0); //TODO change unit to 1024? need more testing
                
                //Iterate through the iterations and split the embed up
                for (var index = 0; index < iterations; index++)
                    //Add the section
                    embedBuilder.AddField($"Section {index + 1}",
                        index == iterations - 1
                            ? lyrics.Substring(index * 1024)
                            : lyrics.Substring(index * 1024, 1024));

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
                Console.WriteLine(exception.StackTrace);
                return await EmbedHelper.CreateBasicEmbed("lyrics",
                    "Failed to retrieve lyrics by either an error, or the source doesn't have any\n" +
                    exception.Message);
            }
        }

        public async Task OnTrackFinished(TrackEndedEventArgs args)
        {
            var reason = args.Reason;
            var player = args.Player;
            var track = args.Track;

            if (!reason.ShouldPlayNext())
                return;

            //Get the option
            Options.TryGetValue(player.VoiceChannel.Guild.Id, out var option);

            if (option != null && option.RepeatTrack)
            {
                //Play the track again
                await player.PlayAsync(track);
                return;
            }
            
            //TODO check if this broke OnTrackFinished
            if (!player.Queue.TryDequeue(out var item) || !(item is { } nextTrack))
            {
                //Make sure the text channel exists
                if (player.TextChannel != null)
                    //Say there are no more songs in the queue
                    await player.TextChannel?.SendMessageAsync("There are no more songs left in queue. Disconnecting");
                
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