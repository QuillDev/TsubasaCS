using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Tsubasa.Services
{
    internal class CommandHandlerService
    {
        private readonly DiscordShardedClient _client;
        private readonly IServiceProvider _services;
        private CommandService _commands;

        public CommandHandlerService(CommandService commands, IServiceProvider services, DiscordShardedClient client)
        {
            _commands = commands;
            _services = services;
            _client = client;
        }

        public async Task ConfigureAsync()
        {
            //Configure the command service
            _commands = new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false
            });

            //add modules and have them use our services
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task HandleCommandAsync(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage msg)) return;

            //position where prefix ends and commands begin
            var argPos = 0;

            //Checks if we have the prefix or if we mentioned the bot
            if (!msg.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

            //create the command context to prepare it for execution
            var context = new ShardedCommandContext(_client, msg);

            //execute the command and return whether there was an error
            await _commands.ExecuteAsync(context, argPos, _services);
        }
    }
}