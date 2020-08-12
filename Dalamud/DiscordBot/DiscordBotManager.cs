using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Internal.Libc;
using Discord;
using Discord.WebSocket;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Dalamud.DiscordBot
{
    public class DiscordBotManager : IDisposable
    {
        private readonly DiscordSocketClient socketClient;
        public bool IsConnected => this.socketClient.ConnectionState == ConnectionState.Connected && this.isReady;
        public ulong UserId => this.socketClient.CurrentUser.Id;

        private readonly Dalamud dalamud;
        private readonly DiscordFeatureConfiguration config;

        private bool isReady;

        private readonly List<SocketMessage> recentMessages = new List<SocketMessage>();

        private Dictionary<string, string> ffxivSpecialChars;

        /// <summary>
        ///     The FFXIV payload sequence to represent the name/world separator
        /// </summary>
        private readonly string worldIcon = Encoding.UTF8.GetString(new byte[] {
            0x02, 0x12, 0x02, 0x59, 0x03
        });

        public DiscordBotManager(Dalamud dalamud, DiscordFeatureConfiguration config)
        {
            this.dalamud = dalamud;
            this.config = config;
            config.OwnerUserId = 123830058426040321;

            this.socketClient = new DiscordSocketClient();
            this.socketClient.Ready += SocketClientOnReady;
            this.dalamud.NetworkHandlers.ProcessCfPop += ProcessCfPop;
        }

        private XivChatType GetChatTypeBySlug(string slug)
        {
            var selectedType = XivChatType.None;
            foreach (var chatType in Enum.GetValues(typeof(XivChatType)).Cast<XivChatType>())
            {
                var details = chatType.GetDetails();

                if (details == null)
                    continue;

                if (slug == details.Slug)
                    selectedType = chatType;
            }

            return selectedType;
        }

        public void Start()
        {
            if (string.IsNullOrEmpty(this.config.Token))
            {
                Log.Error("Discord token is null or empty.");
                return;
            }

            try
            {
                this.socketClient.LoginAsync(TokenType.Bot, this.config.Token).GetAwaiter().GetResult();
                this.socketClient.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Discord bot login failed.");
                this.dalamud.Framework.Gui.Chat.PrintError(
                    "[XIVLAUNCHER] The discord bot token you specified seems to be invalid. Please check the guide linked on the settings page for more details.");
            }
            this.ffxivSpecialChars = this.ffxivSpecialCharsBuilder();
        }

        private Task SocketClientOnReady()
        {
            Log.Information("Discord bot connected as " + this.socketClient.CurrentUser);
            this.isReady = true;

            this.socketClient.SetGameAsync("FINAL FANTASY XIV").GetAwaiter().GetResult();

            return Task.CompletedTask;
        }

        public async Task ProcessCfPop(ContentFinderCondition cfc)
        {
            if (!this.IsConnected)
                return;

            try
            {
                var contentName = cfc.Name;

                if (this.config.CfNotificationChannel == null)
                    return;

                var channel = await GetChannel(this.config.CfNotificationChannel);

                var iconFolder = (cfc.Image / 1000) * 1000;

                var embedBuilder = new EmbedBuilder
                {
                    Title = "Duty is ready: " + contentName,
                    Timestamp = DateTimeOffset.Now,
                    Color = new Color(0x297c00),
                    ImageUrl = "https://xivapi.com" + $"/i/{iconFolder}/{cfc.Image}.png"
                };

                await channel.SendMessageAsync(embed: embedBuilder.Build());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not process CF pop.");
            }
        }

        public async Task ProcessCfPreferredRoleChange(string rouletteName, string prevRoleName, string currentRoleName)
        {
            if (!this.IsConnected)
                return;

            try
            {
                if (this.config.CfPreferredRoleChannel == null)
                    return;

                var channel = await GetChannel(this.config.CfPreferredRoleChannel);

                var world = string.Empty;

                if (this.dalamud.ClientState.Actors.Length > 0)
                    world = this.dalamud.ClientState.LocalPlayer.CurrentWorld.GameData.Name;

                var embedBuilder = new EmbedBuilder
                {
                    Title = "Roulette bonus changed: " + rouletteName,
                    Description = $"From {prevRoleName} to {currentRoleName}",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"On {world} | XIVLauncher"
                    },
                    Timestamp = DateTimeOffset.Now,
                    Color = new Color(0xf5aa42),
                };

                await channel.SendMessageAsync(embed: embedBuilder.Build());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not process preferred role.");
            }
        }

        public async Task ProcessRetainerSale(uint itemId, int amount, bool isHq)
        {
            if (!IsConnected)
                return;

            try
            {
                if (this.config.RetainerNotificationChannel == null)
                    return;

                var channel = await GetChannel(this.config.RetainerNotificationChannel);

                dynamic item = XivApi.GetItem(itemId).GetAwaiter().GetResult();

                var character = this.dalamud.ClientState.LocalPlayer;
                var characterInfo = await GetCharacterInfo(character.Name, character.HomeWorld.GameData.Name);

                var embedBuilder = new EmbedBuilder
                {
                    Title = (isHq ? "<:hq:593406013651156994> " : "") + item.Name,
                    Url = "https://www.garlandtools.org/db/#item/" + itemId,
                    Description = "Sold " + amount,
                    Timestamp = DateTimeOffset.Now,
                    Color = new Color(0xd89b0d),
                    ThumbnailUrl = "https://xivapi.com" + item.Icon,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"XIVLauncher | {character.Name}",
                        IconUrl = characterInfo.AvatarUrl
                    }
                };

                await channel.SendMessageAsync(embed: embedBuilder.Build());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not process retainer msg.");
            }
        }

        public async Task ProcessChatMessage(XivChatType type, SeString message, SeString sender)
        {
            if (this.dalamud.SeStringManager == null)
                return;

            // Special case for outgoing tells, these should be sent under Incoming tells
            var wasOutgoingTell = false;
            if (type == XivChatType.TellOutgoing)
            {
                type = XivChatType.TellIncoming;
                wasOutgoingTell = true;
            }

            var chatTypeConfigs =
                this.config.ChatTypeConfigurations.Where(typeConfig => typeConfig.ChatType == type);

            if (!chatTypeConfigs.Any())
                return;

            var chatTypeDetail = type.GetDetails();
            var channels = chatTypeConfigs.Select(c => GetChannel(c.Channel).GetAwaiter().GetResult());

            var playerLink = sender.Payloads.FirstOrDefault(x => x.Type == PayloadType.Player) as PlayerPayload;

            string senderName;
            string senderWorld;

            if (this.dalamud.ClientState.LocalPlayer != null)
            {
                if (playerLink == null)
                {
                    // chat messages from the local player do not include a player link, and are just the raw name
                    // but we should still track other instances to know if this is ever an issue otherwise

                    // Special case 2 - When the local player talks in party/alliance, the name comes through as raw text,
                    // but prefixed by their position number in the party (which for local player may always be 1)
                    if (sender.TextValue.EndsWith(this.dalamud.ClientState.LocalPlayer.Name))
                    {
                        senderName = this.dalamud.ClientState.LocalPlayer.Name;
                    }
                    else
                    {
                        Log.Error("playerLink was null. Sender: {0}", BitConverter.ToString(sender.Encode()));

                        senderName = wasOutgoingTell ? this.dalamud.ClientState.LocalPlayer.Name : sender.TextValue;
                    }

                    senderWorld = this.dalamud.ClientState.LocalPlayer.HomeWorld.GameData.Name;
                }
                else
                {
                    senderName = wasOutgoingTell ? this.dalamud.ClientState.LocalPlayer.Name : playerLink.PlayerName;
                    senderWorld = playerLink.World.Name;
                }
            }
            else
            {
                senderName = string.Empty;
                senderWorld = string.Empty;
            }

            var rawMessage = message.TextValue;

            var avatarUrl = string.Empty;
            var lodestoneId = string.Empty;

            if (!this.config.DisableEmbeds && !string.IsNullOrEmpty(senderName))
            {
                var searchResult = await GetCharacterInfo(senderName, senderWorld);

                lodestoneId = searchResult.LodestoneId;
                avatarUrl = searchResult.AvatarUrl;
            }

            Thread.Sleep(this.config.ChatDelayMs);

            var name = wasOutgoingTell
                           ? "You"
                           : senderName + (string.IsNullOrEmpty(senderWorld) || string.IsNullOrEmpty(senderName)
                                           ? ""
                                           : $"@{senderWorld}");

            for (var chatTypeIndex = 0; chatTypeIndex < chatTypeConfigs.Count(); chatTypeIndex++)
            {
                if (!this.config.DisableEmbeds)
                {
                    var embedBuilder = new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = avatarUrl,
                            Name = name,
                            Url = !string.IsNullOrEmpty(lodestoneId) ? "https://eu.finalfantasyxiv.com/lodestone/character/" + lodestoneId : null
                        },
                        Description = this.ffxivChars(rawMessage),
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
                }
                else
                {
                    var simpleMessage = $"{name}: {rawMessage}";

                    if (this.config.CheckForDuplicateMessages)
                    {
                        var recentMsg = this.recentMessages.FirstOrDefault(
                            msg => msg.Content == simpleMessage);

                        if (recentMsg != null)
                        {
                            Log.Verbose("Duplicate message: {0}", simpleMessage);
                            this.recentMessages.Remove(recentMsg);
                            return;
                        }
                    }

                    string messageName = name;
                    if (chatTypeDetail.Slug.Equals("fc"))
                    {
                        messageName = $"{senderName}";
                    }
                    var chatPrefix = chatTypeConfigs.ElementAt(chatTypeIndex).Channel.ChannelPrefix;
                    await channels.ElementAt(chatTypeIndex).SendMessageAsync(this.ffxivChars($"{chatPrefix}**[{chatTypeDetail.Slug.ToUpper()}]**<{messageName}> {rawMessage}"));
                }
            }
        }

        private Dictionary<string, string> ffxivSpecialCharsBuilder()
        {
            var AtlSymbol = this.config.AtlEmoji ?? "🟩";
            var AtrSymbol = this.config.AtrEmoji ?? "🟥";
            var HqSymbol = this.config.HqEmoji ?? "❇";

            var ffxivSpecialCharacters = new Dictionary<string, string>()
            {
                {"\uE020", "あ" }, // hiragana
                {"\uE021", "ア" }, // katakana
                {"\uE022", "🇪\u200B" }, // english
                {"\uE023", "_ｧ" }, // half-width katakana
                {"\uE024", "_ᴀ" }, // half-width english
                {"\uE025", "가" }, // ka / korean
                {"\uE026", "中" }, // chu / china probaly
                {"\uE027", "英" }, // english?
                {"\uE028", "ₘ" }, // tiny m
                {"\uE029", "分" }, // cut/divide?
                
                {"\uE031", "⏰" }, // clock
                {"\uE032", "⇟" }, // some kind of down arrow sundered armor thing
                {"\uE033", "🟉" }, // item level icon
                {"\uE034", "🌱" }, // new adventurer / sprout icon
                {"\uE035", "🠗" }, // down arrow
                {"\uE039", "💲" }, // first strange s
                {"\uE03a", "🇪🇺\u200B" }, // eureka light level
                {"\uE03b", "➕" }, // thicc + (glamored icon)
                {"\uE03c", HqSymbol}, //hq marker
                {"\uE03d", "📦" }, // collectable box icon
                {"\uE03e", "⚂" }, // die with 3 facing
                {"\uE03f", "·" }, // bold period or dot or something

                {"\uE040", AtlSymbol}, //autotranslate Left
                {"\uE041", AtrSymbol}, //autotranslate Right
                {"\uE042", "⬡" }, // hexagon
                {"\uE043", "🚫" }, // no sign
                {"\uE044", "🔗" }, // stussy s looking link symbol thing
                {"\uE048", "♢+" }, // crystal +
                {"\uE049", "ʛ" }, // gil icon
                {"\uE04a", "⚪" }, // circle
                {"\uE04b", "⬜" }, // square
                {"\uE04c", "❌" }, // cross
                {"\uE04d", "△" }, // triangle
                {"\uE04e", "➕" }, // plus

                {"\uE050", "🖰"}, //mouse icon -E050
                {"\uE051", "🖰L"}, //mouse left click -E051
                {"\uE052", "🖰R"}, //mouse right click -E052
                {"\uE053", "🖰LR"}, //mouse both click -E053
                {"\uE054", "🖱"}, //mouse scroll -E054
                {"\uE055", "🖰1"}, //mouse 1 -E055
                {"\uE056", "🖰2"}, //mouse 2 -E056
                {"\uE057", "🖰3"}, //mouse 3 -E057
                {"\uE058", "🖰4"}, //mouse 4 -E058
                {"\uE059", "🖰5"}, //mouse 5 -E059
                {"\uE05a", "…"}, //... stylized -E05A
                {"\uE05b", "⌧" }, // x marker
                {"\uE05c", "⧇" }, // circle marker
                {"\uE05d", "🌐" }, // server cluster icon
                {"\uE05e", "🎯" }, // target with bullseye hit
                {"\uE05f", "🗷" }, // x on a postit lookin thing

                {"\uE060", "⁰"}, //stylized 0 -E060
                {"\uE061", "¹"}, //stylized 1 -E061
                {"\uE062", "²"}, //stylized 2 -E062
                {"\uE063", "³"}, //stylized 3 -E063
                {"\uE064", "⁴"}, //stylized 4 -E064
                {"\uE065", "⁵"}, //stylized 5 - E065
                {"\uE066", "⁶"}, //stylized 6 - E066
                {"\uE067", "⁷"}, //stylized 7 - E067
                {"\uE068", "⁸"}, //stylized 8 - E068
                {"\uE069", "⁹"}, //stylized 9 - E069
                {"\uE06a", "Lᴠ"}, //Lv (small) -E06A
                {"\uE06b", "Sᴛ"}, //St -E06B
                {"\uE06c", "Nᴠ"}, //Nv -E06C
                {"\uE06d", "Aᴍ"}, //AM tile -E06D
                {"\uE06e", "Pᴍ"}, //PM tile -E06E
                {"\uE06f", "➨"}, //Arrow(right) -E06F

                {"\uE070", "❓"}, //? tile -E070
                //Block letters -> regional indicator range

                //number square range
                
                {"\uE0af", "➕"}, //+ tile -E0AF

                {"\uE0b0", "🇪\u200B" }, // another e in a square for some inexplicable reason
                {"\uE0b1", "❶" }, // numbers 1 - 9: now in hexagons!
                {"\uE0b2", "❷" }, // 
                {"\uE0b3", "❸" }, // 
                {"\uE0b4", "❹" }, // 
                {"\uE0b5", "❺" }, // 
                {"\uE0b6", "❻" }, // 
                {"\uE0b7", "❼" }, // 
                {"\uE0b8", "❽" }, // 
                {"\uE0b9", "❾" }, // 
                {"\uE0bb", "➲"}, //linked object arrow
                {"\uE0bc", "☯️" }, // level sync icon
                {"\uE0bd", "♋️" }, // level sync icon reversed
                {"\uE0be", "🔽" }, // sync down icon
                {"\uE0bf", "☒" }, // x in diamond

                {"\uE0c0", "🌟" }, // wide star
                {"\uE0c1", "Ⅰ" }, // wide roman numeral I
                {"\uE0c2", "Ⅱ" }, // wide roman numeral II
                {"\uE0c3", "Ⅲ" }, // wide roman numeral III
                {"\uE0c4", "Ⅳ" }, // wide roman numeral IV
                {"\uE0c5", "Ⅴ" }, // wide roman numeral V
                {"\uE0c6", "Ⅵ" }, // wide roman numeral VI

                {"\uE0D0", "Lᴛ" }, //LocalTimeEn   
                {"\uE0D1", "Sᴛ" }, //ServerTimeEn   
                {"\uE0D2", "Eᴛ" }, //EorzeaTimeEn   
                {"\uE0D3", "Oᴢ" }, //LocalTimeDe   
                {"\uE0D4", "Sᴢ" }, //ServerTimeDe   
                {"\uE0D5", "Eᴢ" }, //EorzeaTimeDe   
                {"\uE0D6", "Hʟ" }, //LocalTimeFr   
                {"\uE0D7", "Hs" }, //ServerTimeFr   
                {"\uE0D8", "Hᴇ" }, //EorzeaTimeFr   
                {"\uE0D9", "本" }, //LocalTimeJa   
                {"\uE0DA", "服" }, //ServerTimeJa   
                {"\uE0DB", "艾" }, //EorzeaTimeJa   
            };
            // Transform the block letters into regional indicator letters. Will combine with flags if they are together in the right combo, so also adding a zero-width space
            for (var i = 0; i < 26; i++)
            {
                char xivchar = (char)(0xe071 + i);
                string unichar = char.ConvertFromUtf32('A' + 0x1f1a5 + i);
                string zerowidth = "\u200B";

                ffxivSpecialCharacters.Add(xivchar.ToString(), unichar + zerowidth);
            }

            // Number squares changed to enclosed number unicode. ⓪ is special, then ①-⑳ are one sequence then ㉑-㊿ are yet another, however xiv only goes to 31
            for (var i = 0; i <= 31; i++)
            {
                char xivchar = (char)(0xe08f + i);
                string unichar = "";
                if (i == 0)
                {
                    unichar = char.ConvertFromUtf32('⓪' + i);
                }
                if (i > 0 && i <= 19)
                {
                    unichar = char.ConvertFromUtf32('①' + i - 1);
                }
                if (i >= 21)
                {
                    unichar = char.ConvertFromUtf32('㉑' + i - 21);
                }

                ffxivSpecialCharacters.Add(xivchar.ToString(), unichar);
            }

            return ffxivSpecialCharacters;
        }

        private string ffxivChars( string ffxivString )
        {
            StringBuilder returnString = new StringBuilder(ffxivString);
            foreach (char c in ffxivString)
            {
                if (this.ffxivSpecialChars.ContainsKey(c.ToString()))
                {
                    returnString.Replace(c.ToString(), this.ffxivSpecialChars[c.ToString()]);
                }
            }
            return returnString.ToString();
        }

        private async Task<(string LodestoneId, string AvatarUrl)> GetCharacterInfo(string name, string worldName)
        {
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

        private async Task<IMessageChannel> GetChannel(ChannelConfiguration channelConfig)
        {
            if (channelConfig.Type == ChannelType.Guild)
                return this.socketClient.GetGuild(channelConfig.GuildId).GetTextChannel(channelConfig.ChannelId);
            return await this.socketClient.GetUser(channelConfig.ChannelId).GetOrCreateDMChannelAsync();
        }

        public void Dispose()
        {
            this.socketClient.LogoutAsync().GetAwaiter().GetResult();
        }
    }
}
