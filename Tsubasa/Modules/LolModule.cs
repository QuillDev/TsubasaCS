using System.Threading.Tasks;
using Discord.Commands;
using Tsubasa.Services.LeagueOfLegendsService;

namespace Tsubasa.Modules
{
    public class LolModule : ModuleBase<ShardedCommandContext>
    {
        private readonly LolCommandService _lolCommandService;

        public LolModule(LolCommandService lolCommandService)
        {
            _lolCommandService = lolCommandService;
        }
        
        [Command("lol rank")]
        public async Task SendLolRankedImage([Remainder] string query)
        {
            await _lolCommandService.SendLolRankAsync(Context.Channel, query);
        }
    }
}