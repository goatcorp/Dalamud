using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>Useful functions for implementing data window widgets.</summary>
internal static class DataWindowWidgetExtensions
{
    /// <summary>Draws a text column, and make it copiable by clicking.</summary>
    /// <param name="widget">Owner widget.</param>
    /// <param name="s">String to display.</param>
    /// <param name="alignRight">Whether to align to right.</param>
    /// <param name="framepad">Whether to offset to frame padding.</param>
    public static void TextColumnCopiable(this IDataWindowWidget widget, string s, bool alignRight, bool framepad)
    {
        var offset = ImGui.GetCursorScreenPos() + new Vector2(0, framepad ? ImGui.GetStyle().FramePadding.Y : 0);
        if (framepad)
            ImGui.AlignTextToFramePadding();
        if (alignRight)
        {
            var width = ImGui.CalcTextSize(s).X;
            var xoff = ImGui.GetColumnWidth() - width;
            offset.X += xoff;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + xoff);
            ImGui.TextUnformatted(s);
        }
        else
        {
            ImGui.TextUnformatted(s);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetNextWindowPos(offset - ImGui.GetStyle().WindowPadding);
            var vp = ImGui.GetWindowViewport();
            var wrx = (vp.WorkPos.X + vp.WorkSize.X) - offset.X;
            ImGui.SetNextWindowSizeConstraints(Vector2.One, new(wrx, float.MaxValue));
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(wrx);
            ImGui.TextWrapped(s.Replace("%", "%%"));
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        if (ImGui.IsItemClicked())
        {
            ImGui.SetClipboardText(s);
            Service<NotificationManager>.Get().AddNotification(
                $"Copied {ImGui.TableGetColumnNameS()} to clipboard.",
                widget.DisplayName,
                NotificationType.Success);
        }
    }
}
