using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Tsubasa.Services;
using Tsubasa.Services.AnimeServices;

namespace Tsubasa.Modules
{
    public class NSFW : ModuleBase<ShardedCommandContext>
    {
        private readonly HentaiService _hentai;

        public NSFW(HentaiService hentai)
        {
            _hentai = hentai;
        }

        [Command("hentai")]
        public async Task GetHentai([Remainder] string query)
        {
            await ReplyAsync(embed: await _hentai.GetHentaiAsync( (ITextChannel) Context.Channel, query));
        }



    }
    
    

}