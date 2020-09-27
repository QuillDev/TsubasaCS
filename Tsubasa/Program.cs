using System;
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
using Tsubasa.Services.AnimeServices;
using Tsubasa.Services.Music_Services;
using Victoria;

namespace Tsubasa
{
    internal class Program
    {
        private const string ConfigPath = "config.toml";
        private static BotSettings _config;
        private SpotifyClient _spotifyClient;

        private static void Main()
        {
            //Load config
            _config = GetConfiguration();
            
            //set the title of our console
            Console.Title = "Tsubasa - Developer"; 
            
            //move the program to an async context
            new Program().StartAsync().GetAwaiter().GetResult();
        }
        
        private async Task StartAsync()
        {
            //Declare member vars for service and config

            //Create the spotify client
            _spotifyClient = await AuthenticateSpotifyAsync().ConfigureAwait(false);

            //Build services
            var services = BuildServices();

            //get the bot from our services
            var bot = services.GetRequiredService<DiscordShardedClient>();
            bot.Log += Logger.Log;

            //Configure EventHandlerService and CommandHandlerService
            services.GetRequiredService<DiscordEventHandlerService>().Configure();
            await services.GetRequiredService<CommandHandlerService>().ConfigureAsync();

            //log the bot in
            await bot.LoginAsync(TokenType.Bot, _config.DiscordSettings.BotToken);
            await bot.StartAsync();

            //Make sure this task doesn't end until it crashes, or we want it to
            await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
        }


        private IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                //Add general discord related services
                .AddSingleton(new DiscordShardedClient(_config.DiscordSettings.ShardIds, new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 100,
                    TotalShards = _config.DiscordSettings.ShardIds.Length
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<DiscordEventHandlerService>()
                
                //Add Music related services
                .AddSingleton<MusicService>()
                .AddSingleton<TsubasaSearch>()
                .AddSingleton(_spotifyClient)
                .AddSingleton<SpotifyService>()
                .AddSingleton<TsubasaSearch>()
                .AddSingleton<YoutubeScraperService>()
                .AddLavaNode(x =>
                    x.SelfDeaf = false
                )
                
                //Add anime related services
                .AddSingleton<DanbooruSearchService>()
                .AddSingleton<HentaiService>()
                
                //add general command services
                .AddSingleton<GeneralService>()
                //add general utility services
                .AddSingleton<WebRequestService>()
                .AddSingleton<EmbedService>()

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