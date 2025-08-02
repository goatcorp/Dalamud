using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static void AddCallback(
        ImDrawListPtr self, delegate*<ImDrawList*, ImDrawCmd*, void> callback, void* callbackData = null) =>
        ImGuiNative.AddCallback(self, callback, callbackData);

    public static void AddCallback(
        ImDrawListPtr self, delegate*<ImDrawListPtr, ImDrawCmdPtr, void> callback, void* callbackData = null) =>
        AddCallback(self, (delegate*<ImDrawList*, ImDrawCmd*, void>)callback, callbackData);

    public static void AddCallback(
        ImDrawListPtr self, delegate*<ref ImDrawList, ref ImDrawCmd, void> callback, void* callbackData = null) =>
        AddCallback(self, (delegate*<ImDrawList*, ImDrawCmd*, void>)callback, callbackData);

    public static void AddCallback(ImDrawListPtr self, ImDrawCallbackEnum presetCallback)
    {
        if (!Enum.IsDefined(presetCallback))
            throw new ArgumentOutOfRangeException(nameof(presetCallback), presetCallback, null);
        AddCallback(self, (delegate*<ImDrawList*, ImDrawCmd*, void>)(nint)presetCallback);
    }

    public static ImGuiPayloadPtr AcceptDragDropPayload(
        ImU8String type, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
    {
        fixed (byte* typePtr = &type.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.AcceptDragDropPayload(typePtr, flags);
            type.Dispose();
            return r;
        }
    }

    public static ImFontPtr AddFontFromFileTTF(
        ImFontAtlasPtr self, ImU8String filename, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (byte* filenamePtr = &filename.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.AddFontFromFileTTF(self, filenamePtr, sizePixels, fontCfg, glyphRanges);
            filename.Dispose();
            return r;
        }
    }

    public static ImFontPtr AddFontFromMemoryCompressedBase85TTF(
        ImFontAtlasPtr self, ImU8String compressedFontDatabase85, float sizePixels,
        ImFontConfigPtr fontCfg = default, ushort* glyphRanges = null)
    {
        fixed (byte* compressedFontDatabase85Ptr = &compressedFontDatabase85.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.AddFontFromMemoryCompressedBase85TTF(
                self,
                compressedFontDatabase85Ptr,
                sizePixels,
                fontCfg,
                glyphRanges);
            compressedFontDatabase85.Dispose();
            return r;
        }
    }

    public static ImFontPtr AddFontFromMemoryCompressedTTF(
        ImFontAtlasPtr self, ReadOnlySpan<byte> compressedFontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (byte* compressedFontPtr = compressedFontData)
            return ImGuiNative.AddFontFromMemoryCompressedTTF(
                self,
                compressedFontPtr,
                compressedFontData.Length,
                sizePixels,
                fontCfg,
                glyphRanges);
    }

    public static ImFontPtr AddFontFromMemoryTTF(
        ImFontAtlasPtr self, ReadOnlySpan<byte> fontData, float sizePixels, ImFontConfigPtr fontCfg = default,
        ushort* glyphRanges = null)
    {
        fixed (byte* fontDataPtr = fontData)
            return ImGuiNative.AddFontFromMemoryTTF(
                self,
                fontDataPtr,
                fontData.Length,
                sizePixels,
                fontCfg,
                glyphRanges);
    }

    public static void AddInputCharacter(ImGuiIOPtr self, char c) => ImGuiNative.AddInputCharacter(self, c);
    public static void AddInputCharacter(ImGuiIOPtr self, Rune c) => ImGuiNative.AddInputCharacter(self, (uint)c.Value);
    public static void AddInputCharacters(ImGuiIOPtr self, ImU8String str)
    {
        fixed (byte* strPtr = &str.GetPinnableNullTerminatedReference())
            ImGuiNative.AddInputCharactersUTF8(self.Handle, strPtr);
        str.Dispose();
    }

    public static ref bool GetBoolRef(ImGuiStoragePtr self, uint key, bool defaultValue = false) =>
        ref *ImGuiNative.GetBoolRef(self.Handle, key, defaultValue ? (byte)1 : (byte)0);

    public static ref float GetFloatRef(ImGuiStoragePtr self, uint key, float defaultValue = 0.0f) =>
        ref *ImGuiNative.GetFloatRef(self.Handle, key, defaultValue);

    public static ref int GetIntRef(ImGuiStoragePtr self, uint key, int defaultValue = 0) =>
        ref *ImGuiNative.GetIntRef(self.Handle, key, defaultValue);

    public static ref void* GetVoidPtrRef(ImGuiStoragePtr self, uint key, void* defaultValue = null) =>
        ref *ImGuiNative.GetVoidPtrRef(self.Handle, key, defaultValue);

    public static ref T* GetPtrRef<T>(ImGuiStoragePtr self, uint key, T* defaultValue = null) where T : unmanaged =>
        ref *(T**)ImGuiNative.GetVoidPtrRef(self.Handle, key, defaultValue);

    public static ref T GetRef<T>(ImGuiStoragePtr self, uint key, T defaultValue = default) where T : unmanaged
    {
        if (sizeof(T) > sizeof(void*)) throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);
        return ref *(T*)ImGuiNative.GetVoidPtrRef(self.Handle, key, *(void**)&defaultValue);
    }

    public static uint GetID(ImU8String strId)
    {
        fixed (byte* strIdPtr = strId.Span)
        {
            var r = ImGuiNative.GetID(strIdPtr, strIdPtr + strId.Length);
            strId.Dispose();
            return r;
        }
    }

    public static uint GetID(nint ptrId) => ImGuiNative.GetID((void*)ptrId);
    public static uint GetID(nuint ptrId) => ImGuiNative.GetID((void*)ptrId);
    public static uint GetID(void* ptrId) => ImGuiNative.GetID(ptrId);

    public static void PushID(ImU8String strId)
    {
        fixed (byte* strIdPtr = strId.Span)
        {
            ImGuiNative.PushID(strIdPtr, strIdPtr + strId.Length);
            strId.Dispose();
        }
    }

    public static void PushID(nint ptrId) => ImGuiNative.PushID((void*)ptrId);
    public static void PushID(nuint ptrId) => ImGuiNative.PushID((void*)ptrId);

    public static void PushID(void* ptrId) =>
        ImGuiNative.PushID(ptrId);
}
