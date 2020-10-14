using System;
using System.IO;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Tsubasa.Helper;
using Tsubasa.Services.Music_Services;

namespace Tsubasa.Services.Anime
{
    public class MyAnimeListService
    {
        private readonly WebRequestService _web;
        private readonly EmbedService _embed;

        public MyAnimeListService(WebRequestService web, EmbedService embed)
        {
            _web = web;
            _embed = embed;
        }
        
        /// <summary>
        /// Get anime scheduled for this week and print it in a readable format
        /// </summary>
        /// <returns>The anime scheduled today</returns>
        public async Task GetScheduleAsync(ISocketMessageChannel channel)
        {
            var raw = await _web.RequestRawAsync("http://localhost:2069/api/anime/seasonal");

            var scheduleData = JObject.Parse(raw);
            //Get information about the shows airing
            var imgData = scheduleData["img_data"]?.ToString();
            var names = scheduleData["names"]?.ToString();
            
            Console.WriteLine(names);
            //if the img data is null
            if (imgData == null)
            {
                await channel.SendMessageAsync(embed: await _embed.CreateErrorEmbed("Tsubasa - Airing Anime",
                    "Couldn't load data on the anime"));
                return;
            }
            
            //if the names are null or empty send an error
            if (string.IsNullOrEmpty(names))
            {
                await channel.SendMessageAsync(embed: await _embed.CreateErrorEmbed("Tsubasa - Airing Anime",
                    "Couldn't get anime name info."));
            }
            
            
            //convert image data to a byte array
            var data = Convert.FromBase64String(imgData);
            
            //generate a file with a random file name at the tmp directory 
            var path = Path.Combine("./tmp/", Path.ChangeExtension(Path.GetRandomFileName(), ".jpg"));
            
            await File.WriteAllBytesAsync(path, data);
            
            //send the image file 
            await channel.SendFileAsync(path);
            
            //send the embed with the shows in it
            await channel.SendMessageAsync(embed: await _embed.CreateBasicEmbedAsync("Tsubasa - Airing Anime", names));
        }
    }
}