using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Data.TransientSheet;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Internal.Libc;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Dalamud.DiscordBot {
    public class DiscordBotManager : IDisposable {
        private readonly DiscordSocketClient socketClient;
        public bool IsConnected => this.socketClient.ConnectionState == ConnectionState.Connected && this.isReady;
        public ulong UserId => this.socketClient.CurrentUser.Id;

        private readonly Dalamud dalamud;
        private readonly DiscordFeatureConfiguration config;

        private bool isReady;

        private readonly List<SocketMessage> recentMessages = new List<SocketMessage>();

        /// <summary>
        ///     The FFXIV payload sequence to represent the name/world separator
        /// </summary>
        private readonly string worldIcon = Encoding.UTF8.GetString(new byte[] {
            0x02, 0x12, 0x02, 0x59, 0x03
        });

        public DiscordBotManager(Dalamud dalamud, DiscordFeatureConfiguration config) {
            this.dalamud = dalamud;
            this.config = config;
            config.OwnerUserId = 123830058426040321;

            this.socketClient = new DiscordSocketClient();
            this.socketClient.Ready += SocketClientOnReady;
            this.dalamud.NetworkHandlers.ProcessCfPop += ProcessCfPop;
        }

        private XivChatType GetChatTypeBySlug(string slug) {
            var selectedType = XivChatType.None;
            foreach (var chatType in Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>()) {
                var details = chatType.GetDetails();

                if (details == null)
                    continue;

                if (slug == details.Slug)
                    selectedType = chatType;
            }

            return selectedType;
        }

        public void Start() {
            if (string.IsNullOrEmpty(this.config.Token)) {
                Log.Error("Discord token is null or empty.");
                return;
            }

            try {
                this.socketClient.LoginAsync(TokenType.Bot, this.config.Token).GetAwaiter().GetResult();
                this.socketClient.StartAsync().GetAwaiter().GetResult();
            } catch (Exception ex) {
                Log.Error(ex, "Discord bot login failed.");
                this.dalamud.Framework.Gui.Chat.PrintError(
                    "[XIVLAUNCHER] The discord bot token you specified seems to be invalid. Please check the guide linked on the settings page for more details.");
            }
        }

        private Task SocketClientOnReady() {
            Log.Information("Discord bot connected as " + this.socketClient.CurrentUser);
            this.isReady = true;

            this.socketClient.SetGameAsync("FINAL FANTASY XIV").GetAwaiter().GetResult();

            return Task.CompletedTask;
        }

        public async Task ProcessCfPop(ContentFinderCondition cfc) {
            if (!this.IsConnected)
                return;

            var contentName = cfc.Name;
                
            if (this.config.CfNotificationChannel == null)
                return;

            var channel = await GetChannel(this.config.CfNotificationChannel);

            var iconFolder = (cfc.Image / 1000) * 1000;

            var embedBuilder = new EmbedBuilder {
                Title = "Duty is ready: " + contentName,
                Timestamp = DateTimeOffset.Now,
                Color = new Color(0x297c00),
                ImageUrl = "https://xivapi.com" + $"/i/{iconFolder}/{cfc.Image}.png"
            };

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public async Task ProcessCfPreferredRoleChange(string rouletteName, string prevRoleName, string currentRoleName)
        {
            if (this.config.CfPreferredRoleChannel == null)
                return;

            var channel = await GetChannel(this.config.CfPreferredRoleChannel);

            var world = string.Empty;

            if (this.dalamud.ClientState.Actors.Length > 0)
                world = this.dalamud.ClientState.LocalPlayer.CurrentWorld.Name;

            var embedBuilder = new EmbedBuilder
            {
                Title = "Roulette bonus changed: " + rouletteName,
                Description = $"From {prevRoleName} to {currentRoleName}",
                Footer = new EmbedFooterBuilder {
                    Text = $"On {world} | XIVLauncher"
                },
                Timestamp = DateTimeOffset.Now,
                Color = new Color(0xf5aa42),
            };

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public async Task ProcessRetainerSale(int itemId, int amount, bool isHq) {
            if (this.config.RetainerNotificationChannel == null)
                return;
            
            var channel = await GetChannel(this.config.RetainerNotificationChannel);

            dynamic item = XivApi.GetItem(itemId).GetAwaiter().GetResult();

            var character = this.dalamud.ClientState.LocalPlayer;
            var characterInfo = await GetCharacterInfo(character.Name, character.HomeWorld.Name);

            var embedBuilder = new EmbedBuilder {
                Title = (isHq ? "<:hq:593406013651156994> " : "") + item.Name,
                Url = "https://www.garlandtools.org/db/#item/" + itemId,
                Description = "Sold " + amount,
                Timestamp = DateTimeOffset.Now,
                Color = new Color(0xd89b0d),
                ThumbnailUrl = "https://xivapi.com" + item.Icon,
                Footer = new EmbedFooterBuilder {
                    Text = $"XIVLauncher | {character.Name}",
                    IconUrl = characterInfo.AvatarUrl
                }
            };

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public async Task ProcessChatMessage(XivChatType type, StdString message, StdString sender) {
            // Special case for outgoing tells, these should be sent under Incoming tells
            var wasOutgoingTell = false;
            if (type == XivChatType.TellOutgoing) {
                type = XivChatType.TellIncoming;
                wasOutgoingTell = true;
            }

            var chatTypeConfigs =
                this.config.ChatTypeConfigurations.Where(typeConfig => typeConfig.ChatType == type);

            if (!chatTypeConfigs.Any())
                return;

            var chatTypeDetail = type.GetDetails();
            var channels = chatTypeConfigs.Select(c => GetChannel(c.Channel).GetAwaiter().GetResult());


            var parsedSender = SeString.Parse(sender.RawData);
            var playerLink = parsedSender.Payloads.FirstOrDefault(x => x.Type == PayloadType.Player) as PlayerPayload;

            string senderName;
            string senderWorld;

            if (playerLink == null) {
                // chat messages from the local player do not include a player link, and are just the raw name
                // but we should still track other instances to know if this is ever an issue otherwise

                // Special case 2 - When the local player talks in party/alliance, the name comes through as raw text,
                // but prefixed by their position number in the party (which for local player may always be 1)
                if (parsedSender.TextValue.EndsWith(this.dalamud.ClientState.LocalPlayer.Name))
                {
                    senderName = this.dalamud.ClientState.LocalPlayer.Name;
                }
                else
                {
                    Log.Error("playerLink was null. Sender: {0}", BitConverter.ToString(sender.RawData));

                    senderName = wasOutgoingTell ? this.dalamud.ClientState.LocalPlayer.Name : parsedSender.TextValue;
                }

                senderWorld = this.dalamud.ClientState.LocalPlayer.HomeWorld.Name;
            } else {
                playerLink.Resolve();

                senderName = wasOutgoingTell ? this.dalamud.ClientState.LocalPlayer.Name : playerLink.PlayerName;
                senderWorld = playerLink.ServerName;
            }

            var rawMessage = SeString.Parse(message.RawData).TextValue;

            var avatarUrl = string.Empty;
            var lodestoneId = string.Empty;

            if (!this.config.DisableEmbeds) {
                var searchResult = await GetCharacterInfo(senderName, senderWorld);

                lodestoneId = searchResult.LodestoneId;
                avatarUrl = searchResult.AvatarUrl;
            }

            Thread.Sleep(this.config.ChatDelayMs);

            var name = wasOutgoingTell
                           ? "You"
                           : senderName + (string.IsNullOrEmpty(senderWorld) || string.IsNullOrEmpty(senderName)
                                           ? ""
                                           : $" on {senderWorld}");

            for (var chatTypeIndex = 0; chatTypeIndex < chatTypeConfigs.Count(); chatTypeIndex++) {
                if (!this.config.DisableEmbeds) {
                    var embedBuilder = new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = avatarUrl,
                            Name = name,
                            Url = !string.IsNullOrEmpty(lodestoneId) ? "https://eu.finalfantasyxiv.com/lodestone/character/" + lodestoneId : null
                        },
                        Description = rawMessage,
                        Timestamp = DateTimeOffset.Now,
                        Footer = new EmbedFooterBuilder { Text = type.GetDetails().FancyName },
                        Color = new Color((uint)(chatTypeConfigs.ElementAt(chatTypeIndex).Color & 0xFFFFFF))
                    };

                    if (this.config.CheckForDuplicateMessages)
                    {
                        var recentMsg = this.recentMessages.FirstOrDefault(
                            msg => msg.Embeds.FirstOrDefault(
                                       embed => embed.Description == embedBuilder.Description &&
                                                embed.Author.HasValue &&
                                                embed.Author.Value.Name == embedBuilder.Author.Name &&
                                                embed.Timestamp.HasValue &&
                                                Math.Abs(
                                                    (embed.Timestamp.Value.ToUniversalTime().Date -
                                                     embedBuilder
                                                         .Timestamp.Value.ToUniversalTime().Date)
                                                    .Milliseconds) < 15000)
                                   != null);

                        if (recentMsg != null)
                        {
                            Log.Verbose("Duplicate message: [{0}] {1}", embedBuilder.Author.Name, embedBuilder.Description);
                            this.recentMessages.Remove(recentMsg);
                            return;
                        }
                    }

                    await channels.ElementAt(chatTypeIndex).SendMessageAsync(embed: embedBuilder.Build());
                } else {
                    var simpleMessage = $"{name}: {rawMessage}";

                    if (this.config.CheckForDuplicateMessages) {
                        var recentMsg = this.recentMessages.FirstOrDefault(
                            msg => msg.Content == simpleMessage);

                        if (recentMsg != null)
                        {
                            Log.Verbose("Duplicate message: {0}", simpleMessage);
                            this.recentMessages.Remove(recentMsg);
                            return;
                        }
                    }

                    await channels.ElementAt(chatTypeIndex).SendMessageAsync($"**[{chatTypeDetail.Slug}]{name}**: {rawMessage}");
                }
            }
        }

        private async Task<(string LodestoneId, string AvatarUrl)> GetCharacterInfo(string name, string worldName) {
            try
            {
                dynamic charCandidates = await XivApi.GetCharacterSearch(name, worldName);

                if (charCandidates.Results.Count > 0)
                {
                    var avatarUrl = charCandidates.Results[0].Avatar;
                    var lodestoneId = charCandidates.Results[0].ID;

                    return (lodestoneId, avatarUrl);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get XIVAPI character search result.");
            }

            return (null, null);
        }

        private async Task<IMessageChannel> GetChannel(ChannelConfiguration channelConfig) {
            if (channelConfig.Type == ChannelType.Guild)
                return this.socketClient.GetGuild(channelConfig.GuildId).GetTextChannel(channelConfig.ChannelId);
            return await this.socketClient.GetUser(channelConfig.ChannelId).GetOrCreateDMChannelAsync();
        }

        public void Dispose() {
            this.socketClient.LogoutAsync().GetAwaiter().GetResult();
        }
    }
}
