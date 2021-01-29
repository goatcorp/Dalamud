namespace Dalamud.Game.Internal.Gui {
    public class HoveredAction {
        
        /// <summary>
        /// The base action ID
        /// </summary>
        public uint BaseActionID { get; set; } = 0;
        
        /// <summary>
        /// Action ID accounting for automatic upgrades.
        /// </summary>
        public uint ActionID { get; set; } = 0;
        
        /// <summary>
        /// The type of action
        /// </summary>
        public HoverActionKind ActionKind { get; set; } = HoverActionKind.None;
    }
}
