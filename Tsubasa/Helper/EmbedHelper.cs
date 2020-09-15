using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Tsubasa.Helper
{
    public class EmbedHelper
    {
        public enum EmbedMessageType
        {
            Success = 0,
            Info = 10,
            Error = 20,
            Exception = 30,
            Basic = 1
        }

        public static Embed CreateEmbed(string title, string body, EmbedMessageType type, SocketUser target)
        {
            
            //Create the embed builder
            var embed = new EmbedBuilder();
            
            //get the users avatar url
            var thumbnailUrl = target.GetAvatarUrl();
            
            //Build the author ofhte embed
            var author = new EmbedAuthorBuilder()
            {
                Name = target.Username,
                IconUrl = thumbnailUrl
            };
            
            //Add properties to the embed
            embed.WithAuthor(author);
            embed.WithTitle(title);
            embed.WithDescription(body);
            embed.WithColor(GetEmbedColor(type));
            embed.WithCurrentTimestamp();
            
            return embed.Build();
        }

        public static async Task<Embed> CreateBasicEmbed(string title = null, string description = null, string footer = null)
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