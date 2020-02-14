namespace Dalamud.Game.Command {
    /// <summary>
    /// This class describes a registered command.
    /// </summary>
    public sealed class CommandInfo {
        /// <summary>
        /// The function to be executed when the command is dispatched.
        /// </summary>
        /// <param name="command">The command itself.</param>
        /// <param name="arguments">The arguments supplied to the command, ready for parsing.</param>
        public delegate void HandlerDelegate(string command, string arguments);

        /// <summary>
        /// A <see cref="HandlerDelegate"/> which will be called when the command is dispatched.
        /// </summary>
        public HandlerDelegate Handler { get; }

        /// <summary>
        /// The help message for this command.
        /// </summary>
        public string HelpMessage { get; set; } = string.Empty;

        /// <summary>
        /// If this command should be shown in the help output.
        /// </summary>
        public bool ShowInHelp { get; set; } = true;
        
        /// <summary>
        /// Create a new CommandInfo with the provided handler.
        /// </summary>
        /// <param name="handler"></param>
        public CommandInfo(HandlerDelegate handler) {
            Handler = handler;
        }
    }
}
