using System.Numerics;

using Dalamud.Game.Text;
using Dalamud.Plugin.Internal.Types;

using ImGuiNET;

namespace Dalamud.Interface.ImGuiNotification.Internal.IconSource;

/// <summary>Represents the use of <see cref="SeIconChar"/> as the icon of a notification.</summary>
internal class SeIconCharIconSource : INotificationIconSource.IInternal
{
    /// <summary>Initializes a new instance of the <see cref="SeIconCharIconSource"/> class.</summary>
    /// <param name="c">The character.</param>
    public SeIconCharIconSource(SeIconChar c) => this.IconChar = c;

    /// <summary>Gets the icon character.</summary>
    public SeIconChar IconChar { get; }

    /// <inheritdoc/>
    public INotificationIconSource Clone() => this;

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public INotificationMaterializedIcon Materialize() => new MaterializedIcon(this.IconChar);

    private sealed class MaterializedIcon : INotificationMaterializedIcon
    {
        private readonly string iconString;

        public MaterializedIcon(SeIconChar c) => this.iconString = c.ToIconString();

        public void Dispose()
        {
        }

        public void DrawIcon(Vector2 minCoord, Vector2 maxCoord, Vector4 color, LocalPlugin? initiatorPlugin)
        {
            using (Service<NotificationManager>.Get().IconAxisFontHandle.Push())
            {
                var size = ImGui.CalcTextSize(this.iconString);
                var pos = ((minCoord + maxCoord) - size) / 2;
                ImGui.SetCursorPos(pos);
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TextUnformatted(this.iconString);
                ImGui.PopStyleColor();
            }
        }
    }
}
