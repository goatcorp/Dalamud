using ImGuiNET;

namespace Dalamud.Interface.Components
{
    /// <summary>
    /// HelpMarker component to add a help icon with text on hover.
    /// </summary>
    public class HelpMarkerComponent : IComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpMarkerComponent"/> class.
        /// </summary>
        /// <param name="helpText">The text to display on hover.</param>
        public HelpMarkerComponent(string helpText)
        {
            this.HelpText = helpText;
        }

        /// <summary>
        /// Gets component name.
        /// </summary>
        public string Name { get; } = "HelpMarker Component";

        /// <summary>
        /// Gets or sets a value indicating whether the help text should display on same line as previous element.
        /// </summary>
        public bool SameLine { get; set; } = true;

        /// <summary>
        /// Gets or sets the help text.
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        /// Gets or sets the help marker icon.
        /// </summary>
        public FontAwesomeIcon HelpIcon { get; set; } = FontAwesomeIcon.InfoCircle;

        /// <summary>
        /// Gets or sets the help text size modifier.
        /// </summary>
        public float HelpTextModifier { get; set; } = 35.0f;

        /// <summary>
        /// Draw HelpMarker component.
        /// </summary>
        public void Draw()
        {
            if (this.SameLine) ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextDisabled(this.HelpIcon.ToIconString());
            ImGui.PopFont();
            if (!ImGui.IsItemHovered()) return;
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * this.HelpTextModifier);
            ImGui.TextUnformatted(this.HelpText);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }
    }
}
