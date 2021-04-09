using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Components
{
    /// <summary>
    /// IconButton component to use an icon as a button.
    /// </summary>
    public class IconButtonComponent : IComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IconButtonComponent"/> class.
        /// </summary>
        /// <param name="buttonId">The id for the button.</param>
        /// <param name="buttonIcon">The icon for the button.</param>
        public IconButtonComponent(int buttonId, FontAwesomeIcon buttonIcon)
        {
            this.ButtonId = buttonId;
            this.ButtonIcon = buttonIcon;
        }

        /// <summary>
        /// Delegate for the <see cref="IconButtonComponent.IsButtonClickedDelegate"/> event that occurs when the button is clicked.
        /// </summary>
        /// <param name="buttonId">The id of the button that was clicked.</param>
        public delegate void IsButtonClickedDelegate(int buttonId);

        /// <summary>
        /// Event that occurs when the button is clicked.
        /// </summary>
        public event IsButtonClickedDelegate OnButtonClicked;

        /// <summary>
        /// Gets component name.
        /// </summary>
        public string Name { get; } = "IconButton Component";

        /// <summary>
        /// Gets or sets the id for the button.
        /// </summary>
        public int ButtonId { get; set; }

        /// <summary>
        /// Gets or sets the icon to use for the button.
        /// </summary>
        public FontAwesomeIcon ButtonIcon { get; set; }

        /// <summary>
        /// Gets or sets the button color.
        /// </summary>
        public Vector4 ButtonColor { get; set; } = Vector4.Zero;

        /// <summary>
        /// Gets or sets the active button color.
        /// </summary>
        public Vector4 ButtonColorActive { get; set; } = Vector4.Zero;

        /// <summary>
        /// Gets or sets the hovered button color.
        /// </summary>
        public Vector4 ButtonColorHovered { get; set; } = Vector4.Zero;

        /// <summary>
        /// Draw IconButton component.
        /// </summary>
        public void Draw()
        {
            ImGui.PushStyleColor(ImGuiCol.Button, this.ButtonColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, this.ButtonColorActive);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, this.ButtonColorHovered);
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{this.ButtonIcon.ToIconString()}{this.ButtonId}"))
            {
                this.OnButtonClicked?.Invoke(this.ButtonId);
            }

            ImGui.PopFont();
            ImGui.PopStyleColor(3);
        }
    }
}
