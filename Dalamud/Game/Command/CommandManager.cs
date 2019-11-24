using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Dalamud.Game.Chat;
using Dalamud.Game.Internal.Libc;
using Serilog;

namespace Dalamud.Game.Command {
    public sealed class CommandManager {
        private readonly Dalamud dalamud;

        private readonly Dictionary<string, CommandInfo> commandMap = new Dictionary<string, CommandInfo>();

        public ReadOnlyDictionary<string, CommandInfo> Commands =>
            new ReadOnlyDictionary<string, CommandInfo>(this.commandMap);

        private readonly Regex commandRegexEn =
            new Regex(@"^The command (?<command>.+) does not exist\.$", RegexOptions.Compiled);

        private readonly Regex commandRegexJp = new Regex(@"^そのコマンドはありません。： (?<command>.+)$", RegexOptions.Compiled);

        private readonly Regex commandRegexDe =
            new Regex(@"^„(?<command>.+)“ existiert nicht als Textkommando\.$", RegexOptions.Compiled);

        private readonly Regex commandRegexFr =
            new Regex(@"^La commande texte “(?<command>.+)” n'existe pas\.$",
                      RegexOptions.Compiled);

        private readonly Regex CommandRegex;


        public CommandManager(Dalamud dalamud, ClientLanguage language) {
            this.dalamud = dalamud;

            switch (language) {
                case ClientLanguage.Japanese:
                    this.CommandRegex = this.commandRegexJp;
                    break;
                case ClientLanguage.English:
                    this.CommandRegex = this.commandRegexEn;
                    break;
                case ClientLanguage.German:
                    this.CommandRegex = this.commandRegexDe;
                    break;
                case ClientLanguage.French:
                    this.CommandRegex = this.commandRegexFr;
                    break;
            }

            dalamud.Framework.Gui.Chat.OnChatMessage += OnChatMessage;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref StdString sender,
                                   ref StdString message, ref bool isHandled) {
            if (type == XivChatType.GatheringSystemMessage && senderId == 0) {
                var cmdMatch = this.CommandRegex.Match(message.Value).Groups["command"];
                if (cmdMatch.Success) {
                    // Yes, it's a chat command.
                    var command = cmdMatch.Value;
                    if (ProcessCommand(command)) isHandled = true;
                }
            }
        }

        private bool ProcessCommand(string content) {
            string command;
            string argument;

            var speratorPosition = content.IndexOf(' ');
            if (speratorPosition == -1 || speratorPosition + 1 >= content.Length) {
                // If no space was found or ends with the space. Process them as a no argument
                command = content;
                argument = string.Empty;
            } else {
                // e.g.)
                // /testcommand arg1
                // => Total of 17 chars
                // => command: 0-12 (12 chars)
                // => argument: 13-17 (4 chars)
                // => content.IndexOf(' ') == 12
                command = content.Substring(0, speratorPosition);

                var argStart = speratorPosition + 1;
                argument = content.Substring(argStart, content.Length - argStart);
            }

            if (!this.commandMap.TryGetValue(command, out var handler)) // Commad was not found.
                return false;

            DispatchCommand(command, argument, handler);
            return true;
        }

        public void DispatchCommand(string command, string argument, CommandInfo info) {
            try {
                info.Handler(command, argument);
            } catch (Exception ex) {
                Log.Error(ex, "Error while dispatching command {CommandName} (Argument: {Argument})", command,
                          argument);
            }
        }

        public bool AddHandler(string command, CommandInfo info) {
            if (info == null) throw new ArgumentNullException(nameof(info), "Command handler is null.");

            try {
                this.commandMap.Add(command, info);
                return true;
            } catch (ArgumentException) {
                Log.Warning("Command {CommandName} is already registered.", command);
                return false;
            }
        }

        public bool RemoveHandler(string command) {
            return this.commandMap.Remove(command);
        }
    }
}
