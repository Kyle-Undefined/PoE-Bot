using System;
using System.Collections.Generic;

namespace PoE.Bot.Plugin.Twitch
{
    internal class Twitch
    {
        public string Name { get; private set; }
        public string UserId { get; private set; }
        public bool IsLive { get; set; }
        public ulong ChannelId { get; private set; }

        public Twitch(string name, string userId, bool isLive, ulong channelId)
        {
            this.Name = name;
            this.UserId = userId;
            this.IsLive = isLive;
            this.ChannelId = channelId;
        }
    }
}
