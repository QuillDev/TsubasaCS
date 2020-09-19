namespace Tsubasa.Models
{
    public class BotSettings
    {
        public string BotName { get; set; } = "Tsubasa";
        public string MusicModuleName { get; set; } = "Tsubasa Audio#1";
        public string SpotifyId{ get; set; } = "clientidgoeshere";
        public string SpotifySecret{ get; set; } = "secretgoeshere";

        public DiscordSettings DiscordSettings { get; set; } = new DiscordSettings();
    }
}