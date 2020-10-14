using System;
using System.IO;
using System.Threading.Tasks;
using Discord.WebSocket;
using Tsubasa.Services.LeagueOfLegends;
using Tsubasa.Services.Music_Services;

namespace Tsubasa.Services.LeagueOfLegendsService
{
    public class LolCommandService
    {
        private readonly LolApiService _lol;
        private readonly EmbedService _embed;
        
        public LolCommandService(LolApiService lol, EmbedService embed)
        {
            _lol = lol;
            _embed = embed;
        }
        
        /// <summary>
        /// Send data about the given user's lol rank
        /// </summary>
        /// <param name="channel">the channel the message was from</param>
        /// <param name="query">the summoner name to check for</param>
        /// <returns></returns>
        public async Task SendLolRankAsync(ISocketMessageChannel channel, string query)
        {

            var imgData = await _lol.GetLolRankDataAsync(query);
            
            //if image data is null or empty
            if (string.IsNullOrEmpty(imgData))
            {
                await channel.SendMessageAsync(embed: await _embed.CreateErrorEmbed("Tsubasa - LoL",
                    $"Couldn't get data for the summoner name {query}, check it's validity or try again."));
                return;
            }
            
            var data = Convert.FromBase64String(imgData);


            //generate a file with a random file name at the tmp directory 
            var path = Path.Combine("./tmp/", Path.ChangeExtension(Path.GetRandomFileName(), ".jpg"));
            
            await File.WriteAllBytesAsync(path, data);
            
            //send the image
            await channel.SendFileAsync(path);
            
            //delete the file after we're done with it
            File.Delete(path);
        }
    }
}