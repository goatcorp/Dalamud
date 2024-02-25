using System.Numerics;

using Dalamud.Game.Text;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Plugin.Internal.Types;

using ImGuiNET;

namespace Dalamud.Interface.ImGuiNotification.IconSource;

/// <summary>Represents the use of <see cref="SeIconChar"/> as the icon of a notification.</summary>
public readonly struct SeIconCharIconSource : INotificationIconSource.IInternal
{
    /// <summary>The icon character.</summary>
    public readonly SeIconChar Char;

    /// <summary>Initializes a new instance of the <see cref="SeIconCharIconSource"/> struct.</summary>
    /// <param name="c">The character.</param>
    public SeIconCharIconSource(SeIconChar c) => this.Char = c;

    /// <inheritdoc/>
    public INotificationIconSource Clone() => this;

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
    }

    /// <inheritdoc/>
    INotificationMaterializedIcon INotificationIconSource.IInternal.Materialize() => new MaterializedIcon(this.Char);

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
