using System.Threading.Tasks;
using Discord.Commands;
using Tsubasa.Services.Anime;

namespace Tsubasa.Modules
{
    public class AnimeModule : ModuleBase<ShardedCommandContext>
    {
        private readonly MyAnimeListService _animeListService;

        public AnimeModule(MyAnimeListService animeListService)
        {
            _animeListService = animeListService;
        }
        
        [Command("anime schedule"), Alias("anime")]
        public async Task GetAiringAnime()
        {
            await _animeListService.GetScheduleAsync(Context.Channel);
        }
        
    }
}