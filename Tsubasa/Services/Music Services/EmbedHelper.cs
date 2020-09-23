using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Tsubasa.Services.Music_Services
{
    //TODO Make this a service?
    public class EmbedService
    {

        private readonly DiscordShardedClient _client;

        public EmbedService(DiscordShardedClient client)
        {
            _client = client;
        }
        
        private enum EmbedMessageType
        {
            Success = 0,
            Info = 10,
            Error = 20,
            Exception = 30,
            Basic = 1
        }

        public Embed CreateErrorEmbed(string title, string body)
        {
            //Create the embed builder
            var embed = new EmbedBuilder();

            //get the users avatar url
            var thumbnailUrl = _client.CurrentUser.GetAvatarUrl();

            //Build the author of the embed
            var author = new EmbedAuthorBuilder
            {
                Name = _client.CurrentUser.Username,
                IconUrl = thumbnailUrl
            };

            //Add properties to the embed
            embed.WithAuthor(author);
            embed.WithTitle(title);
            embed.WithDescription(body);
            embed.WithColor(GetEmbedColor(EmbedMessageType.Error));
            embed.WithCurrentTimestamp();

            return embed.Build();
        }
        
        public async Task<Embed> CreateBasicEmbedAsync(string title, string description, string footer)
        {
            
            //Create the embed using the given data
            var embed = await Task.Run(() => new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithFooter(footer)
                .WithColor(GetEmbedColor(EmbedMessageType.Basic))
                .WithCurrentTimestamp()
                .Build()
            );

            return embed;
        }
        //overload for main method
        public async Task<Embed> CreateBasicEmbedAsync(string title, string description)
        {
            return await CreateBasicEmbedAsync(title, description, null);
        }
        
        /// <summary>
        /// Get embed color depending on the enum used
        /// </summary>
        /// <param name="type">The EmbedMessageType to use</param>
        /// <returns>return the color corresponding to the message type</returns>
        private static Color GetEmbedColor(EmbedMessageType type)
        {
            //determine embed color based on the embed type
            switch (type)
            {
                case EmbedMessageType.Info:
                    return Color.DarkBlue;
                case EmbedMessageType.Success:
                    return Color.Green;
                case EmbedMessageType.Error:
                    return Color.Red;
                case EmbedMessageType.Exception:
                    return Color.Red;
                default:
                    return Color.Purple;
            }
        }
    }
}