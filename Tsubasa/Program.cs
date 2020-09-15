using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

using Nett;

using Tsubasa.Models;
using Tsubasa.Services;

using Victoria;

namespace Tsubasa
{
    class Program
    {
        private const string configPath = "config.toml";

        private IServiceProvider _services;
        private static BotSettings _config;

        static void Main()
        {
            //Load config
            _config = GetConfiguration();

            //set the title of our console
            Console.Title = "Tsubasa - Developer";

            try
            {
                new Program().StartAsync().GetAwaiter().GetResult();
            }
            //If an exception occurs print it and exit
            catch(Exception exception)
            {
                Console.WriteLine(exception);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("awaiting input for exit");
                Console.ReadLine();
            }
        }

        private async Task StartAsync()
        {
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
            await Task.Delay(Timeout.Infinite);
        }


        private IServiceProvider BuildServices()
        {
            //TODO Add any new services we add here
            return new ServiceCollection()
                .AddSingleton( new DiscordShardedClient(_config.DiscordSettings.ShardIds, new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 100,
                    TotalShards = _config.DiscordSettings.ShardIds.Length
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<DiscordEventHandlerService>()
                .AddLavaNode(x =>
                    x.SelfDeaf = false
                )
                .AddSingleton<MusicService>()
                .BuildServiceProvider();
        }

        private static BotSettings GetConfiguration()
        {
            try
            {
                return Toml.ReadFile<BotSettings>(Path.Combine(Directory.GetCurrentDirectory(), configPath));
            }
            catch
            {
                var initializeConfig = new BotSettings();
                Toml.WriteFile(initializeConfig, Path.Combine(configPath));
                return Toml.ReadFile<BotSettings>(Path.Combine(Directory.GetCurrentDirectory(), configPath));
            }
        }
    }
}
