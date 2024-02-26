using System.Numerics;

using Dalamud.Plugin.Internal.Types;

using ImGuiNET;

namespace Dalamud.Interface.ImGuiNotification.Internal.IconSource;

/// <summary>Represents the use of <see cref="FontAwesomeIcon"/> as the icon of a notification.</summary>
internal class FontAwesomeIconIconSource : INotificationIconSource.IInternal
{
    /// <summary>Initializes a new instance of the <see cref="FontAwesomeIconIconSource"/> class.</summary>
    /// <param name="iconChar">The character.</param>
    public FontAwesomeIconIconSource(FontAwesomeIcon iconChar) => this.IconChar = iconChar;

    /// <summary>Gets the icon character.</summary>
    public FontAwesomeIcon IconChar { get; }

    /// <inheritdoc/>
    public INotificationIconSource Clone() => this;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public INotificationMaterializedIcon Materialize() => new MaterializedIcon(this.IconChar);

    /// <summary>Draws the icon.</summary>
    /// <param name="iconString">The icon string.</param>
    /// <param name="minCoord">The coordinates of the top left of the icon area.</param>
    /// <param name="maxCoord">The coordinates of the bottom right of the icon area.</param>
    /// <param name="color">The foreground color.</param>
    internal static void DrawIconStatic(string iconString, Vector2 minCoord, Vector2 maxCoord, Vector4 color)
    {
        using (Service<NotificationManager>.Get().IconFontAwesomeFontHandle.Push())
        {
            var size = ImGui.CalcTextSize(iconString);
            var pos = ((minCoord + maxCoord) - size) / 2;
            ImGui.SetCursorPos(pos);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted(iconString);
            ImGui.PopStyleColor();
        }
    }

    private sealed class MaterializedIcon : INotificationMaterializedIcon
    {
        private readonly string iconString;

        public MaterializedIcon(FontAwesomeIcon c) => this.iconString = c.ToIconString();

        public void Dispose()
        {
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin) =>
            DrawIconStatic(this.iconString, minCoord, maxCoord, color);
    }
}
