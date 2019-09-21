namespace Dalamud.Game.Command {
    public sealed class CommandInfo {
        public delegate void HandlerDelegate(string command, string arguments);

        public HandlerDelegate Handler { get; }

        public string HelpMessage { get; set; } = string.Empty;

        public bool ShowInHelp { get; set; } = true;
        
        public CommandInfo(HandlerDelegate handler) {
            Handler = handler;
        }
    }
}
