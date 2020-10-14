using System;
using System.Threading.Tasks;
using Discord;
using Tsubasa.Services.Music_Services;

namespace Tsubasa.Services.AnimeServices
{
    public class HentaiService
    {
        private readonly DanbooruSearchService _danbooru;
        private readonly EmbedService _embedService;

        public HentaiService(DanbooruSearchService danbooru, EmbedService embedService)
        {
            _danbooru = danbooru;
            _embedService = embedService;
        }

        public async Task<Embed> GetHentaiAsync(ITextChannel channel, string query)
        {
            
            //check if the channel is nsfw
            if (!channel.IsNsfw)
            {
                return await _embedService.CreateErrorEmbed($"Hentai - {query}",
                    "Cannot use this command in a non NSFW channel for more info on how to set a channel as NSFW refer to the following url\nhttps://support.discord.com/hc/en-us/articles/115000084051-NSFW-Channels-and-Content");
            }
            
            //get the url using the random hentai gen from Danboroo
            var url = await _danbooru.GetRandomHentaiAsync(query);

            //if the string is null or empty create the error embed
            if (string.IsNullOrEmpty(url))
            {
                return await _embedService.CreateErrorEmbed($"Hentai - {query}", $"Could not find hentai for image {query}");
            }

            return await _embedService.CreateImageEmbedAsync($"Hentai - {query}", url);
        }
    }
}