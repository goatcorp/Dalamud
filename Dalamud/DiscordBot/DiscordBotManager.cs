using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Chat;
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
            this.socketClient.MessageReceived += SocketClient_MessageReceived;
        }

        private async Task SocketClient_MessageReceived(SocketMessage arg) {
            if (arg.Embeds != null && arg.Embeds.Count == 1) {
                this.recentMessages.Add(arg);
                return;
            }

            var msgContent = arg.Content;

            if (!msgContent.StartsWith("!f"))
                return;

            if (arg.Author.Id != this.config.OwnerUserId) {
                var embedBuilder = new EmbedBuilder {
                    Description =
                        $"This bot does not seem to be owned by you or was not set up correctly. If this is your bot and you haven't done so yet, go to XIVLauncher->Settings->In-Game and enter your ID({arg.Author.Id}) into the owner ID field.",
                    Color = new Color(0xc20000),
                    Footer = new EmbedFooterBuilder {
                        Text = "XIVLauncher"
                    }
                };

                await arg.Channel.SendMessageAsync(embed: embedBuilder.Build());
                return;
            }

            msgContent = msgContent.Substring(2);
            var parts = msgContent.Split();

            switch (parts[0]) {
                case "setdefault": {
                    var selectedType = GetChatTypeBySlug(parts[1]);

                    EmbedBuilder embedBuilder = null;
                    if (selectedType == XivChatType.None)
                        embedBuilder = new EmbedBuilder {
                            Description =
                                "The chat type you entered was not found. Use !ftypes for a list of possible values.",
                            Color = new Color(0xc20000),
                            Footer = new EmbedFooterBuilder {
                                Text = "XIVLauncher"
                            }
                        };

                    await arg.Channel.SendMessageAsync(embed: embedBuilder.Build());
                }
                    break;

                case "types": {
                    var embedText = string.Empty;

                    foreach (var chatType in Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>()) {
                        var details = chatType.GetDetails();

                        if (details?.Slug == null)
                            continue;

                        embedText += $"{details.FancyName}    -   {details.Slug}\n";
                    }

                    var embedBuilder = new EmbedBuilder {
                        Description =
                            "These are the possible chat type values you can use, when set up in the XIVLauncher settings:\n\n" +
                            embedText,
                        Color = new Color(0x949494),
                        Footer = new EmbedFooterBuilder {
                            Text = "XIVLauncher"
                        }
                    };

                    await arg.Channel.SendMessageAsync(embed: embedBuilder.Build());
                }
                    break;
                default: {
                    var selectedType = GetChatTypeBySlug(parts[0]);
                }
                    break;
            }
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

        public async Task ProcessFate(int id) {
            if (this.config.FateNotificationChannel == null)
                return;

            var channel = await GetChannel(this.config.FateNotificationChannel);

            dynamic fateInfo = XivApi.GetFate(id).GetAwaiter().GetResult();

            this.dalamud.Framework.Gui.Chat.Print("Watched Fate spawned: " + (string) fateInfo.Name);

            var embedBuilder = new EmbedBuilder {
                Author = new EmbedAuthorBuilder {
                    IconUrl = "https://xivapi.com" + (string) fateInfo.Icon,
                    Name = "Fate spawned: " + (string) fateInfo.Name
                },
                Color = new Color(0xa73ed1),
                Timestamp = DateTimeOffset.Now
            };

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public async Task ProcessCfPop(JObject contentFinderCondition) {
            var contentName = contentFinderCondition["Name"];

            if (this.config.CfNotificationChannel == null)
                return;

            var channel = await GetChannel(this.config.CfNotificationChannel);

            var contentImage = contentFinderCondition["Image"];

            var embedBuilder = new EmbedBuilder {
                Title = "Duty is ready: " + contentName,
                Timestamp = DateTimeOffset.Now,
                Color = new Color(0x297c00),
                ImageUrl = "https://xivapi.com" + contentImage
            };

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public async Task ProcessRetainerSale(int itemId, int amount, bool isHq) {
            if (this.config.RetainerNotificationChannel == null)
                return;

            var channel = await GetChannel(this.config.RetainerNotificationChannel);

            dynamic item = XivApi.GetItem(itemId).GetAwaiter().GetResult();

            var embedBuilder = new EmbedBuilder {
                Title = (isHq ? "<:hq:593406013651156994> " : "") + item.Name,
                Url = "https://www.garlandtools.org/db/#item/" + itemId,
                Description = "Sold " + amount,
                Timestamp = DateTimeOffset.Now,
                Color = new Color(0xd89b0d),
                ThumbnailUrl = "https://xivapi.com" + item.Icon
            };

            await channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        public async Task ProcessChatMessage(XivChatType type, string message, string sender) {
            // Special case for outgoing tells, these should be sent under Incoming tells
            var wasOutgoingTell = false;
            if (type == XivChatType.TellOutgoing) {
                type = XivChatType.TellIncoming;
                sender = this.dalamud.ClientState.LocalPlayer.Name;
                wasOutgoingTell = true;
            }

            var chatTypeConfigs =
                this.config.ChatTypeConfigurations.Where(typeConfig => typeConfig.ChatType == type);

            if (!chatTypeConfigs.Any())
                return;

            var channels = chatTypeConfigs.Select(c => GetChannel(c.Channel).GetAwaiter().GetResult());

            var senderSplit = sender.Split(new[] {this.worldIcon}, StringSplitOptions.None);


            var world = string.Empty;

            if (this.dalamud.ClientState.Actors.Length > 0)
                world = this.dalamud.ClientState.LocalPlayer.CurrentWorld.Name;

            if (senderSplit.Length == 2) {
                world = senderSplit[1];
                sender = senderSplit[0];
            }

            sender = SeString.Parse(sender).Output;
            message = SeString.Parse(message).Output;

            sender = RemoveAllNonLanguageCharacters(sender);

            var avatarUrl = "";
            var lodestoneId = 0;

            if (!this.config.DisableEmbeds) {
                try
                {
                    dynamic charCandidates = await XivApi.GetCharacterSearch(sender, world);

                    if (charCandidates.Results.Count > 0)
                    {
                        avatarUrl = charCandidates.Results[0].Avatar;
                        lodestoneId = charCandidates.Results[0].ID;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not get XIVAPI character search result.");
                }
            }

            Thread.Sleep(this.config.ChatDelayMs);

            var name = wasOutgoingTell
                           ? "You"
                           : sender + (string.IsNullOrEmpty(world) || string.IsNullOrEmpty(sender)
                                           ? ""
                                           : $" on {world}");

            for (var chatTypeIndex = 0; chatTypeIndex < chatTypeConfigs.Count(); chatTypeIndex++) {
                if (!this.config.DisableEmbeds) {
                    var embedBuilder = new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = avatarUrl,
                            Name = name,
                            Url = lodestoneId != 0 ? "https://eu.finalfantasyxiv.com/lodestone/character/" + lodestoneId : null
                        },
                        Description = message,
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
                    var simpleMessage = $"{name}: {message}";

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

                    await channels.ElementAt(chatTypeIndex).SendMessageAsync($"{name}: {message}");
                }
            }
        }

        private async Task<IMessageChannel> GetChannel(ChannelConfiguration channelConfig) {
            if (channelConfig.Type == ChannelType.Guild)
                return this.socketClient.GetGuild(channelConfig.GuildId).GetTextChannel(channelConfig.ChannelId);
            return await this.socketClient.GetUser(channelConfig.ChannelId).GetOrCreateDMChannelAsync();
        }

        private string RemoveAllNonLanguageCharacters(string input) {
            return Regex.Replace(input, @"[^\p{L} ']", "");
        }

        public void Dispose() {
            this.socketClient.LogoutAsync().GetAwaiter().GetResult();
        }
    }
}
