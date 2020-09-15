using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Tsubasa.Models
{
    public class BotSettings
    {
        public string BotName { get; set; } = "Tsubasa";
        public string MusicModuleName { get; set; } = "Tsubasa Audio#1";

        public DiscordSettings DiscordSettings { get; set; } = new DiscordSettings();
    }
}