using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Chat;

namespace Dalamud.DiscordBot
{
    public enum ChannelType {
        Guild, 
        User
    }

    [Serializable]
    public class ChannelConfiguration {
        public ChannelType Type { get; set; }
            
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public string ChannelPrefix { get; set; }
    }

    [Serializable]
    public class ChatTypeConfiguration {
        public XivChatType ChatType { get; set; }

        public ChannelConfiguration Channel { get; set; }
        public int Color { get; set; }
    }

    [Serializable]
    public class DiscordFeatureConfiguration
    {
        public string Token { get; set; }

        public bool CheckForDuplicateMessages { get; set; }
        public int ChatDelayMs { get; set; }
        public string AtlEmoji { get; set; }
        public string AtrEmoji { get; set; }
        public string HqEmoji { get; set; }

        public bool DisableEmbeds { get; set; }

        public ulong OwnerUserId { get; set; }

        public List<ChatTypeConfiguration> ChatTypeConfigurations { get; set; }

        public ChannelConfiguration CfNotificationChannel { get; set; }
        public ChannelConfiguration CfPreferredRoleChannel { get; set; }
        public ChannelConfiguration RetainerNotificationChannel { get; set; }
    }
}
