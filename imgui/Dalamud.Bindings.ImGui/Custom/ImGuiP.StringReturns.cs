using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bindings.ImGui;

public unsafe partial class ImGuiP
{
    public static Span<byte> ImTextCharToUtf8(Span<byte> buf, char c) => ImTextCharToUtf8(buf, (uint)c);

    public static Span<byte> ImTextCharToUtf8(Span<byte> buf, int c) => ImTextCharToUtf8(buf, (uint)c);

    public static Span<byte> ImTextCharToUtf8(Span<byte> buf, uint c)
    {
        if (!new Rune(c).TryEncodeToUtf8(buf, out var len))
            throw new ArgumentException("Buffer is too small.", nameof(buf));
        return buf[..len];
    }

    public static ReadOnlySpan<byte> GetNameU8(this ImGuiWindowSettingsPtr self) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiPNative.GetName(self.Handle));

    public static ReadOnlySpan<byte> GetTabNameU8(this ImGuiTabBarPtr self, ImGuiTabItemPtr tab) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiPNative.GetTabName(self.Handle, tab.Handle));

    public static ReadOnlySpan<byte> GetNavInputNameU8(this ImGuiNavInput n) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiPNative.GetNavInputName(n));

    public static ReadOnlySpan<byte> TableGetColumnNameU8(this ImGuiTablePtr table, int columnN) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiPNative.TableGetColumnName(table.Handle, columnN));

    public static ReadOnlySpan<byte> GetNameU8(this in ImGuiWindowSettings self)
    {
        fixed (ImGuiWindowSettings* selfPtr = &self)
            return GetNameU8(selfPtr);
    }

    public static ReadOnlySpan<byte> GetTabNameU8(ImGuiTabBarPtr self, in ImGuiTabItem tab)
    {
        fixed (ImGuiTabItem* tabPtr = &tab)
            return GetTabNameU8(self, tabPtr);
    }

    public static ReadOnlySpan<byte> GetTabNameU8(this in ImGuiTabBar self, ImGuiTabItemPtr tab)
    {
        fixed (ImGuiTabBar* selfPtr = &self)
            return GetTabNameU8(selfPtr, tab);
    }

    public static ReadOnlySpan<byte> GetTabNameU8(this in ImGuiTabBar self, in ImGuiTabItem tab)
    {
        fixed (ImGuiTabBar* selfPtr = &self)
        fixed (ImGuiTabItem* tabPtr = &tab)
            return GetTabNameU8(selfPtr, tabPtr);
    }

    public static ReadOnlySpan<byte> TableGetColumnNameU8(this in ImGuiTable table, int columnN)
    {
        fixed (ImGuiTable* tablePtr = &table)
            return TableGetColumnNameU8(tablePtr, columnN);
    }

    public static string GetName(this ImGuiWindowSettingsPtr self) => Encoding.UTF8.GetString(GetNameU8(self));

    public static string GetTabName(this ImGuiTabBarPtr self, ImGuiTabItemPtr tab) =>
        Encoding.UTF8.GetString(GetTabNameU8(self, tab));

    public static string GetTabName(this ImGuiTabBarPtr self, in ImGuiTabItem tab) =>
        Encoding.UTF8.GetString(GetTabNameU8(self, tab));

    public static string GetNavInputName(this ImGuiNavInput n) => Encoding.UTF8.GetString(GetNavInputNameU8(n));

    public static string TableGetColumnName(this ImGuiTablePtr table, int columnN) =>
        Encoding.UTF8.GetString(TableGetColumnNameU8(table, columnN));

    public static string GetName(this in ImGuiWindowSettings self) => Encoding.UTF8.GetString(GetNameU8(self));

    public static string GetTabName(this in ImGuiTabBar self, ImGuiTabItemPtr tab) =>
        Encoding.UTF8.GetString(GetTabNameU8(self, tab));

    public static string GetTabName(this in ImGuiTabBar self, in ImGuiTabItem tab) =>
        Encoding.UTF8.GetString(GetTabNameU8(self, tab));

    public static string TableGetColumnName(this in ImGuiTable table, int columnN) =>
        Encoding.UTF8.GetString(TableGetColumnNameU8(table, columnN));
}
