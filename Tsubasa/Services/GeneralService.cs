using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Tsubasa.Services.Music_Services;

namespace Tsubasa.Services
{
    public class GeneralService
    {
        private readonly EmbedService _embed;
        
        public GeneralService(EmbedService embed)
        {
            _embed = embed;
        }
        
        /// <summary>
        /// Gets the help command and sends it as a result
        /// </summary>
        /// <returns>An embed with help info</returns>
        public Task<Embed> SendHelpAsync()
        {
            return _embed.CreateBasicEmbedAsync("Tsubasa - Help",
                "Information about commands can be found here\nhttps://quilldev.github.io/Tsubasa/");
        }
        
        /// <summary>
        /// Get information about the guild as an embed
        /// </summary>
        /// <param name="user">The user who requested the information</param>
        /// <returns>the guild info in an embed</returns>
        public async Task<Embed> SendGuildInfoAsync(SocketGuildUser user)
        {
            //Execute the embed task on another thread
            var embed = await Task.Run(() =>
            {
                //get the guild from the user
                var guild = user.Guild;

                //Create the embed with guild-specific info
                var asyncEmbed = new EmbedBuilder
                    {
                        Title = "Tsubasa - Guild Info",
                        ThumbnailUrl = guild.IconUrl,
                        Timestamp = DateTimeOffset.Now,
                        Fields = new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Guild Users",
                                Value = guild.MemberCount
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Creation Date",
                                Value = guild.CreatedAt.ToString("d")
                            },
                            new EmbedFieldBuilder
                            {
                                IsInline = true,
                                Name = "Guild Description",
                                Value = guild.Description ?? "No Description"
                            }
                        }
                    }
                    .WithCurrentTimestamp()
                    .Build();

                return asyncEmbed;
            }).ConfigureAwait(false);
            return embed;
        }
    }
}