﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Tsubasa.Modules;

namespace Tsubasa.Services
{
    internal class CommandHandlerService
    {
        private CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly DiscordShardedClient _client;

        public CommandHandlerService(CommandService commands, IServiceProvider services, DiscordShardedClient client)
        {
            _commands = commands;
            _services = services;
            _client = client;
        }

        public async Task ConfigureAsync()
        {
            //Configure the command service
            _commands = new CommandService( new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false
            });
            
            //add modules and have them use our services
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task HandleCommandAsync(SocketMessage arg)
        {
            Console.WriteLine("Got Here!");
            if (!(arg is SocketUserMessage msg)) return;
            
            //position where prefix ends and commands begin
            var argPos = 0;
            
            //Checks if we have the prefix or if we mentioned the bot
            if (!msg.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;
            
            Console.WriteLine("Got Past check!");
            //create the command context to prepare it for execution
            var context = new ShardedCommandContext(_client, msg);
            
            //execute the command and return whether there was an error
            var result = await _commands.ExecuteAsync(context, argPos, _services);
        }
    }
}