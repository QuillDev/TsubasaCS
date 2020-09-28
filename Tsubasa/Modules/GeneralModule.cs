using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Tsubasa.Services;

namespace Tsubasa.Modules
{
    public class General : ModuleBase<ShardedCommandContext>
    {
        private readonly GeneralService _service;
        public General(GeneralService service)
        {
            _service = service;
        }
        
        [Command("help")]
        public async Task HelpCommand()
        {
            await ReplyAsync(embed: await _service.SendHelpAsync());
        }

        [Command("guild")]
        public async Task GuildInfoCommand()
        {
            await ReplyAsync(embed: await _service.SendGuildInfoAsync((SocketGuildUser) Context.User));
        }
        
        //TODO Look at docs for a better way, this works tho
        [Command("pfp")]
        public async Task UserProfilePictureCommand([Remainder] string dummy)
        {
            await ReplyAsync(embed: await _service.SendProfilePictureAsync(Context.Message));
        }
    }
}