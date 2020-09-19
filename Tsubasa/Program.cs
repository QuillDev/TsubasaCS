﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Nett;
using SpotifyAPI.Web;
using Tsubasa.Helper;
using Tsubasa.Models;
using Tsubasa.Services;
using Tsubasa.Services.Music_Services;
using Victoria;

namespace Tsubasa
{
    internal class Program
    {
        //TODO Documentation for all the methods I made today @9/14/2020
        private const string ConfigPath = "config.toml";
        private static BotSettings _config;
        private static SpotifyClient _spotifyClient;

        //Declare member vars for service and config
        private IServiceProvider _services;

        private static void Main()
        {
            //Load config
            _config = GetConfiguration();
            
            //set the title of our console
            Console.Title = "Tsubasa - Developer"; 
            
            //move the program to an async context
            new Program().StartAsync().GetAwaiter().GetResult();
        }

        //TODO setup logging for all files that should really have it
        private async Task StartAsync()
        {
            //Create the spotify client
            _spotifyClient = await AuthenticateSpotifyAsync();

            //Build services
            _services = BuildServices();

            //get the bot from our services
            var bot = _services.GetRequiredService<DiscordShardedClient>();
            bot.Log += Logger.Log;

            //Configure EventHandlerService and CommandHandlerService
            _services.GetRequiredService<DiscordEventHandlerService>().Configure();
            await _services.GetRequiredService<CommandHandlerService>().ConfigureAsync();

            //log the bot in
            await bot.LoginAsync(TokenType.Bot, _config.DiscordSettings.BotToken);
            await bot.StartAsync();

            //Make sure this task doesn't end until it crashes, or we want it to
            await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
        }


        private IServiceProvider BuildServices()
        {
            //TODO Add any new services we add here
            return new ServiceCollection()
                .AddSingleton(new DiscordShardedClient(_config.DiscordSettings.ShardIds, new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 100,
                    TotalShards = _config.DiscordSettings.ShardIds.Length
                }))
                //Add services
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<DiscordEventHandlerService>()
                .AddSingleton<MusicService>()
                .AddSingleton<TsubasaSearch>()
                .AddSingleton(_spotifyClient)
                .AddSingleton<SpotifyService>()
                .AddSingleton<TsubasaSearch>()
                .AddSingleton<EmbedService>()
                .AddSingleton<YoutubeScraperService>()
                .AddSingleton<WebRequestService>()
                //Misc
                .AddLavaNode(x =>
                    x.SelfDeaf = false
                )
                .BuildServiceProvider();
        }

        private async Task<SpotifyClient> AuthenticateSpotifyAsync()
        {
            //create a default config
            var config = SpotifyClientConfig.CreateDefault();

            //create a request for credentials
            var request =
                new ClientCredentialsRequest(_config.SpotifyId, _config.SpotifySecret);
            var response = await new OAuthClient(config).RequestToken(request);

            var spotify = new SpotifyClient(config.WithToken(response.AccessToken));

            return spotify;
        }

        private static BotSettings GetConfiguration()
        {
            try
            {
                return Toml.ReadFile<BotSettings>(Path.Combine(Directory.GetCurrentDirectory(), ConfigPath));
            }
            catch
            {
                var initializeConfig = new BotSettings();
                Toml.WriteFile(initializeConfig, Path.Combine(ConfigPath));
                return Toml.ReadFile<BotSettings>(Path.Combine(Directory.GetCurrentDirectory(), ConfigPath));
            }
        }
    }
}