using System.Threading.Tasks;
using Discord.Commands;

namespace Tsubasa.Modules
{
    public class General : ModuleBase<ShardedCommandContext>
    {
        [Command("help")]
        public async Task HelpCommand()
        {
            await ReplyAsync("Information about commands can be found here\nhttps://quilldev.github.io/Tsubasa/");
        }
    }
}