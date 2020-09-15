using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Tsubasa.Helper;
using Tsubasa.Models;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace Tsubasa.Services
{
    public class MusicService
    {
        private readonly LavaNode _lavaNode;

        public MusicService(LavaNode lavaNode)
        {
            _lavaNode = lavaNode;
        }
        
        
        private readonly Lazy<ConcurrentDictionary<ulong, MusicSettings>> LazySettings = new Lazy<ConcurrentDictionary<ulong, MusicSettings>>();

        private ConcurrentDictionary<ulong, MusicSettings> Options => LazySettings.Value;
        
        public async Task<Embed> JoinAsync(SocketGuildUser user)
        {
	        Console.WriteLine("Ran join command");
            //Get the guild off the user
            IGuild guild = user.Guild;

            if (user.VoiceChannel == null)
                return await EmbedHelper.CreateBasicEmbed("Music Join", "You must be in a voice channel");
            
            if (_lavaNode.HasPlayer(guild))
                return await EmbedHelper.CreateBasicEmbed("Music, Join",
                    $"I can't join another voice channel until I'm disconnected.");
            
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
            //Get the guild off the user
            IGuild guild = user.Guild;
            
            if (user.VoiceChannel == null)
                return await EmbedHelper.CreateBasicEmbed("Music Play", "You must be in a channel first!");
            
            //If the guild does not have a player, join the server when callig play.
            if (!_lavaNode.HasPlayer(guild))
            {
	            await JoinAsync(user);
            }
            
            LavaPlayer player = _lavaNode.GetPlayer(guild);
            

            try
            {
                //TODO Add other sources (Twitch/Spotify/Soundcloud/etc..)
                //create a var to save the track to
                LavaTrack track;

                //search yt for the song
                var search = await _lavaNode.SearchAsync(query);

                if (search.LoadStatus == LoadStatus.NoMatches && query != null)
                    return await EmbedHelper.CreateBasicEmbed("Music", $"I wasn't able to find anything for {query}");
                if (search.LoadStatus == LoadStatus.LoadFailed && query != null)
                    return await EmbedHelper.CreateBasicEmbed("Music", $"Loading failed for query {query}");

                //set track  the first track
                track = search.Tracks.FirstOrDefault();
                
                if (player.PlayerState == PlayerState.Playing)
                {
	                player.Queue.Enqueue(track);
                    return await EmbedHelper.CreateBasicEmbed("Music", $"{track.Title} has been added to queue");
                }

                await player.PlayAsync(track);
                return await EmbedHelper.CreateBasicEmbed("Music", $"Now Playing: {track.Title}\nURL: {track.Url}");
            }
            catch (Exception e)
            {
                return await EmbedHelper.CreateBasicEmbed("Music, Play Exception", e.Message);
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
            try
            {
                var descriptionBuilder = new StringBuilder();

                var player = _lavaNode.GetPlayer(user.Guild);
                if (player == null)
                    return await EmbedHelper.CreateBasicEmbed("Music Queue", $"Could not aquire music player.\nAre you using the music service right now?");

                if (player.PlayerState == PlayerState.Playing)
                {

                    if (player.Queue.Count < 1 && player.Track != null)
                    {
                        return await EmbedHelper.CreateBasicEmbed($"Now Playing: {player.Track.Title}", "There are no other items in the queue.");
                    }
                    else
                    {
                        var trackNum = 2;
                        foreach (LavaTrack track in player.Queue)
                        {
                            if (trackNum == 2) { descriptionBuilder.Append($"Up Next: [{track.Title}]({track.Url})\n"); trackNum++; }
                            else { descriptionBuilder.Append($"#{trackNum}: [{track.Title}]({track.Url})\n"); trackNum++; }
                        }
                        return await EmbedHelper.CreateBasicEmbed("Music Playlist", $"Now Playing: [{player.Track.Title}]({player.Track.Url})\n{descriptionBuilder.ToString()}");
                    }
                }
                else
                {
                    return await EmbedHelper.CreateBasicEmbed("Music Queue", "Player doesn't seem to be playing anything right now. If this is an error, Please contact Stage in the Kaguya support server.");
                }
            }
            catch (Exception ex)
            {
                return await EmbedHelper.CreateBasicEmbed("Music, List", ex.Message);
            }

        }
        
	    public async Task<Embed> SkipTrackAsync(SocketGuildUser user)
		{
			try
			{
				var player = _lavaNode.GetPlayer(user.Guild);
				if (player == null)
					return await EmbedHelper.CreateBasicEmbed("Music, List", $"Could not acquire player.\nAre you using the bot right now?");
				if (player.Queue.Count == 0)
					return await EmbedHelper.CreateBasicEmbed("Music Skipping", "There are no songs to skip!");
				try
				{
						var currentTrack = player.Track;
						await player.SkipAsync();
						return await EmbedHelper.CreateBasicEmbed("Music Skip", $"Successfully skipped {currentTrack.Title}\nNow Playing{player.Track.Title}");
				}
				catch (Exception ex){
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
			{
				return await EmbedHelper.CreateBasicEmbed($"Music Volume", $"Volume must be between 1 and 149.");
			}
			try
			{
				//get the player 
				var player = _lavaNode.GetPlayer(user.Guild);
				
				//update the volume
				await player.UpdateVolumeAsync((ushort) volume);
				return await EmbedHelper.CreateBasicEmbed($"🔊 Music Volume", $"Volume has been set to {volume}.");
			}
			catch (InvalidOperationException ex)
			{
				return await EmbedHelper.CreateBasicEmbed("Music Volume", $"{ex.Message}", "Please contact Stage in the support server if this is a recurring issue.");
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
					return await EmbedHelper.CreateBasicEmbed("▶️ Music", $"**Resumed:** Now Playing {player.Track.Title}");
				}
				else
				{
					await player.PauseAsync();
					return await EmbedHelper.CreateBasicEmbed("⏸️ Music", $"**Paused:** {player.Track.Title}");
				}
			}
			catch (InvalidOperationException e)
			{
				return await EmbedHelper.CreateBasicEmbed("Music Play/Pause", e.Message);
			}
		}

		public async Task<Embed> SeekAsync(SocketGuildUser user, int seconds)
		{
			LavaPlayer player = _lavaNode.GetPlayer(user.Guild);

			TimeSpan seekPoint = TimeSpan.FromSeconds(seconds);
			
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
				Options.TryUpdate(user.Guild.Id, new MusicSettings{
					RepeatTrack = !option.RepeatTrack
				}, option);

				return await EmbedHelper.CreateBasicEmbed("Music Loop", $"Looping set to {!option.RepeatTrack}");
			}
			catch (Exception exception)
			{
				return await EmbedHelper.CreateBasicEmbed("Music Loop", $"The dev fucked up. idk what this error even is");
			}

			
		}
		public async Task OnTrackFinished(TrackEndedEventArgs args)
		{
			TrackEndReason reason = args.Reason;
			LavaPlayer player = args.Player;
			LavaTrack track = args.Track;
			
			if (!reason.ShouldPlayNext())
				return;
			
			//Get the option
			Options.TryGetValue(player.VoiceChannel.Guild.Id, out var option);

			if (option.RepeatTrack)
			{
				//Play the track again
				await player.PlayAsync(track);
				return;
			}

			if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTrack))
			{
				await player.TextChannel?.SendMessageAsync($"There are no more songs left in queue.");
				return;
			}
			
			await player.PlayAsync(nextTrack);

			EmbedBuilder embed = new EmbedBuilder();
			embed.WithDescription($"**Finished Playing: `{track.Title}`\nNow Playing: `{nextTrack.Title}`**");
			embed.WithColor(Color.Red);
			await player.TextChannel.SendMessageAsync(embed: embed.Build());
			await player.TextChannel.SendMessageAsync(player.ToString());
		}
		
    }
}