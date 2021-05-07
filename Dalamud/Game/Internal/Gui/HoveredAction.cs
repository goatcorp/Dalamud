namespace Dalamud.Game.Internal.Gui
{
    public class HoveredAction
    {
        /// <summary>
        /// Gets or sets the base action ID.
        /// </summary>
        public uint BaseActionID { get; set; } = 0;

        /// <summary>
        /// Gets or sets the action ID accounting for automatic upgrades.
        /// </summary>
        public uint ActionID { get; set; } = 0;

        /// <summary>
        /// Gets or sets the type of action.
        /// </summary>
        public HoverActionKind ActionKind { get; set; } = HoverActionKind.None;
    }
}
