using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public unsafe partial class ImGui
{
    public static ReadOnlySpan<byte> GetVersionU8() =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetVersion());

    public static ReadOnlySpan<byte> TableGetColumnNameU8(int columnN = -1) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.TableGetColumnName(columnN));

    public static ReadOnlySpan<byte> GetStyleColorNameU8(this ImGuiCol idx) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetStyleColorName(idx));

    public static ReadOnlySpan<byte> GetKeyNameU8(this ImGuiKey key) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetKeyName(key));

    public static ReadOnlySpan<byte> GetClipboardTextU8() =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetClipboardText());

    public static ReadOnlySpan<byte> SaveIniSettingsToMemoryU8()
    {
        nuint len;
        var ptr = ImGuiNative.SaveIniSettingsToMemory(&len);
        return new(ptr, (int)len);
    }

    public static ref byte begin(this ImGuiTextBufferPtr self) => ref *ImGuiNative.begin(self.Handle);

    public static ref byte begin(this in ImGuiTextBuffer self)
    {
        fixed (ImGuiTextBuffer* selfPtr = &self)
            return ref *ImGuiNative.begin(selfPtr);
    }

    public static ref byte end(this ImGuiTextBufferPtr self) => ref *ImGuiNative.end(self.Handle);

    public static ref byte end(this in ImGuiTextBuffer self)
    {
        fixed (ImGuiTextBuffer* selfPtr = &self)
            return ref *ImGuiNative.end(selfPtr);
    }

    public static ReadOnlySpan<byte> c_str(this ImGuiTextBufferPtr self) => self.Handle->c_str();

    public static ReadOnlySpan<byte> c_str(this scoped in ImGuiTextBuffer self)
    {
        fixed (ImGuiTextBuffer* selfPtr = &self)
            return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.c_str(selfPtr));
    }

    public static ReadOnlySpan<byte> GetDebugNameU8(this ImFontPtr self) =>
        MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetDebugName(self.Handle));

    public static ReadOnlySpan<byte> GetDebugNameU8(this scoped in ImFont self)
    {
        fixed (ImFont* selfPtr = &self)
            return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ImGuiNative.GetDebugName(selfPtr));
    }

    public static string GetVersion() => Encoding.UTF8.GetString(GetVersionU8());
    public static string TableGetColumnName(int columnN = -1) => Encoding.UTF8.GetString(TableGetColumnNameU8(columnN));
    public static string GetStyleColorName(this ImGuiCol idx) => Encoding.UTF8.GetString(GetStyleColorNameU8(idx));
    public static string GetKeyName(this ImGuiKey key) => Encoding.UTF8.GetString(GetKeyNameU8(key));
    public static string GetClipboardText() => Encoding.UTF8.GetString(GetClipboardTextU8());
    public static string SaveIniSettingsToMemory() => Encoding.UTF8.GetString(SaveIniSettingsToMemoryU8());
    public static string GetDebugName(this ImFontPtr self) => Encoding.UTF8.GetString(GetDebugNameU8(self));
    public static string GetDebugName(this scoped in ImFont self) => Encoding.UTF8.GetString(GetDebugNameU8(self));
}
