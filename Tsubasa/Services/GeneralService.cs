using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Sends the PFP of the user into the chat
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<Embed> SendProfilePictureAsync(SocketGuildUser user)
        {
            //send their image into the chat
            return await _embed.CreateImageEmbedAsync($"Profile Pic for {user.Nickname}", user.GetAvatarUrl());
        }

        public async Task<Embed> SendProfilePictureAsync(SocketUserMessage message)
        {
            //if there are any mentioned users print an embed with them
            if (message.MentionedUsers.Any())
            {
                return await SendProfilePictureAsync((SocketGuildUser) message.MentionedUsers.ElementAt(0));
            }
            
            //if that didnt work print an error message
            return await _embed.CreateErrorEmbed("Tsubasa - PFP", "Couldn't get the PFP for the mentioned user.");
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

        /// <summary>
        /// Send the invite URL of the bot into the server via an embed
        /// </summary>
        /// <param name="client"></param>
        /// <returns>An embed with the invite url</returns>
        public async Task<Embed> SendInviteAsync(DiscordShardedClient client)
        {
            
            //Generate the embed asynchronously using task.run
            var result = Task.Run(() =>
            {
                
                //TODO see if i want to add an extra method to embed helper class for this kinda stuff
                const string url = "https://discord.com/oauth2/authorize?client_id=753764233484828703&permissions=2147483639&scope=bot";
                var inviteEmbed = new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder
                    {
                        Url = url,
                        IconUrl = client.CurrentUser.GetAvatarUrl(),
                        Name = " > Invite Tsubasa to your server! <",
                    })
                    .WithUrl(url)
                    .WithColor(Color.Purple)
                    .Build();

                return inviteEmbed;
            });

            return await result;
        }
        
        /// <summary>
        /// Responds to the message with an embed that sends a url to by github
        /// </summary>
        /// <returns>An embed with tsubasas github</returns>
        public async Task<Embed> SendSourceAsync()
        {
            return await _embed.CreateBasicEmbedAsync("Tsubasa - Source Code",
                "The source code for Tsubasa and my other projects can be found here!\nhttps://github.com/QuillDev/");
        }
    }
}