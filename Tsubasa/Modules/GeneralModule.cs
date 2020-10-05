using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Tsubasa.Services;

namespace Tsubasa.Modules
{
    public class General : ModuleBase<ShardedCommandContext>
    {
        private readonly GeneralService _generalService;
        public General(GeneralService generalService)
        {
            _generalService = generalService;
        }
        
        /// <summary>
        /// Help command that prints out the help website
        /// </summary>
        /// <returns>Replies to the message with the help website</returns>
        [Command("help")]
        public async Task HelpCommand()
        {
            await ReplyAsync(embed: await _generalService.SendHelpAsync());
        }
        
        /// <summary>
        /// Prints out information about the guild the user called the command from
        /// </summary>
        /// <returns>Replies to the message with an embed and info about the guild</returns>
        [Command("guild")]
        public async Task GuildInfoCommand()
        {
            await ReplyAsync(embed: await _generalService.SendGuildInfoAsync((SocketGuildUser) Context.User));
        }
        
        /// <summary>
        /// Command that prints the PFP of the user if no users are mentioned
        /// </summary>
        /// <returns>Replies to the message with the users profile picture</returns>
        [Command("pfp"), Priority(1)]
        public async Task UserProfilePictureCommand()
        {
            await ReplyAsync(embed: await _generalService.SendProfilePictureAsync((SocketGuildUser) Context.User));
        }
        
        /// <summary>
        /// Command that prints the PFP of the user if a user is mentioned
        /// </summary>
        /// <param name="content">The content of the message, goes unused but is needed for it to trigger</param>
        /// <returns>Replies to the message with the mentioned user's profile picture.</returns>
        [Command("pfp"), Priority(0)]
        public async Task UserProfilePictureCommand([Remainder] string content)
        {
            await ReplyAsync(embed: await _generalService.SendProfilePictureAsync(Context.Message));
        }
        
        /// <summary>
        /// Command that prints out the invite url to the bot so you can invite it
        /// </summary>
        /// <returns>Replies with teh bots invite url</returns>
        [Command("invite")]
        public async Task SendInviteUrlCommand()
        {
            await ReplyAsync(embed: await _generalService.SendInviteAsync(Context.Client));
        }

        [Command("source"), Alias("src", "github")]
        public async Task SendBotSourceUrl()
        {
            await ReplyAsync(embed: await _generalService.SendSourceAsync());
        }
    }
}