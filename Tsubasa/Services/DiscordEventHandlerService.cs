using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Tsubasa.Services.Music_Services;
using Victoria;

namespace Tsubasa.Services
{
    internal class DiscordEventHandlerService
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandHandlerService _commands;
        private readonly LavaNode _lavaNode;
        private readonly MusicService _music;

        //Counter for amount of loaded shards
        private int _loadedShards;


        public DiscordEventHandlerService(DiscordShardedClient client, CommandHandlerService commands, LavaNode node,
            MusicService music)
        {
            _client = client;
            _commands = commands;
            _lavaNode = node;
            _music = music;
        }

        internal void Configure()
        {
            _client.ShardReady += DiscordClientReadyAsync;
            _client.ShardDisconnected += DiscordClientDisconnected;
            _client.MessageReceived += DiscordMessageRecieved;
        }

        private async Task DiscordClientReadyAsync(DiscordSocketClient socketClient)
        {
            //Increment loaded shards
            _loadedShards++;

            //If all shards are loaded
            if (_loadedShards == _client.Shards.Count)
            {
                await Task.Delay(500).ConfigureAwait(false);

                //If the lavanode isn't connected, connect to it!
                if (!_lavaNode.IsConnected)
                    //Connect to the lavanode
                    await _lavaNode.ConnectAsync();

                //Add lava node to logger and add on track ended event to music on track ended
                _lavaNode.OnLog += Logger.Log;
                _lavaNode.OnTrackEnded += _music.OnTrackFinished;

                //set loaded shards to 0
                _loadedShards = 0;
            }
        }

        private Task DiscordClientDisconnected(Exception exception, DiscordSocketClient shard)
        {
            //Log that the shard was diconnected
            Logger.Log(new LogMessage(LogSeverity.Warning, $"Shard {shard.ShardId} Disconnected", exception.Message));
            return Task.CompletedTask;
        }

        private async Task DiscordMessageRecieved(SocketMessage message)
        {
            //ignore bots
            if (message.Author.IsBot) return;

            //Check if the message had a command in it using the command handler.
            await _commands.HandleCommandAsync(message);
        }
    }
}