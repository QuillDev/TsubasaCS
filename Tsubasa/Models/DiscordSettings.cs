﻿namespace Tsubasa.Models
{
    public class DiscordSettings
    {
        //TODO Dont hardcode
        public string BotToken { get; set; } = "TOKEN-GOES-HERE";
        public int[] ShardIds { get; set; } = {0, 1};
    }
}