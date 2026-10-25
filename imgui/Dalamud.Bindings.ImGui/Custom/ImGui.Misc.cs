using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    /// <summary>Adds a callback that the renderer calls when it reaches this point of the draw list.</summary>
    /// <param name="self">The draw list.</param>
    /// <param name="callback">The callback.</param>
    /// <param name="callbackData">The data the callback gets in <see cref="ImDrawCmd.UserCallbackData"/>.</param>
    /// <remarks>Dalamud's renderer sets a straight alpha blend state before the callback. Its own pixel shader outputs
    /// premultiplied color, so a callback that draws sets its own pixel shader, and its own blend state if it needs
    /// another. The renderer binds its blend state and text resources again after the callback.</remarks>
    public static void AddCallback(
        ImDrawListPtr self, delegate*<ImDrawListPtr, ImDrawCmdPtr, void> callback, void* callbackData = null) =>
        ImGuiNative.AddCallback(self, (delegate*<ImDrawList*, ImDrawCmd*, void>)callback, callbackData, 0);

    /// <inheritdoc cref="AddCallback(ImDrawListPtr, delegate*{ImDrawListPtr, ImDrawCmdPtr, void}, void*)"/>
    public static void AddCallback(
        ImDrawListPtr self, delegate*<ref ImDrawList, ref ImDrawCmd, void> callback, void* callbackData = null) =>
        ImGuiNative.AddCallback(self, (delegate*<ImDrawList*, ImDrawCmd*, void>)callback, callbackData, 0);

    public static void AddCallback(ImDrawListPtr self, ImDrawCallbackEnum presetCallback)
    {
        if (!Enum.IsDefined(presetCallback))
            throw new ArgumentOutOfRangeException(nameof(presetCallback), presetCallback, null);
        ImGuiNative.AddCallback(self, (delegate*<ImDrawList*, ImDrawCmd*, void>)(nint)presetCallback, null, 0);
    }

    public static ImGuiPayloadPtr AcceptDragDropPayload(
        ImU8String type, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
    {
        fixed (byte* typePtr = &type.GetPinnableNullTerminatedReference())
        {
            var r = ImGuiNative.AcceptDragDropPayload(typePtr, flags);
            type.Recycle();
            return r;
        }
    }

    public static void AddInputCharacter(ImGuiIOPtr self, char c) => ImGuiNative.AddInputCharacter(self, c);
    public static void AddInputCharacter(ImGuiIOPtr self, Rune c) => ImGuiNative.AddInputCharacter(self, (uint)c.Value);

    public static void AddInputCharacters(ImGuiIOPtr self, ImU8String str)
    {
        fixed (byte* strPtr = &str.GetPinnableNullTerminatedReference())
            ImGuiNative.AddInputCharactersUTF8(self.Handle, strPtr);
        str.Recycle();
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
        fixed (byte* strIdPtr = strId)
        {
            var r = ImGuiNative.GetID(strIdPtr, strIdPtr + strId.Length);
            strId.Recycle();
            return r;
        }
    }

    public static uint GetID(nint ptrId) => ImGuiNative.GetID((void*)ptrId);
    public static uint GetID(nuint ptrId) => ImGuiNative.GetID((void*)ptrId);
    public static uint GetID(void* ptrId) => ImGuiNative.GetID(ptrId);

    public static void PushID(ImU8String strId)
    {
        fixed (byte* strIdPtr = strId)
        {
            ImGuiNative.PushID(strIdPtr, strIdPtr + strId.Length);
            strId.Recycle();
        }
    }

    public static void PushID(nint ptrId) => ImGuiNative.PushID((void*)ptrId);
    public static void PushID(nuint ptrId) => ImGuiNative.PushID((void*)ptrId);

    public static void PushID(void* ptrId) =>
        ImGuiNative.PushID(ptrId);
}
