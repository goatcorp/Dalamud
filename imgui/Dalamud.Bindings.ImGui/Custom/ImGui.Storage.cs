using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Bindings.ImGui;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static unsafe partial class ImGui
{
    public static ref bool GetBoolRef(ImGuiStoragePtr self, uint key, bool defaultValue = false) =>
        ref *ImGuiNative.GetBoolRef(self.Handle, key, defaultValue ? (byte)1 : (byte)0);

    public static ref float GetFloatRef(ImGuiStoragePtr self, uint key, float defaultValue = 0.0f) =>
        ref *ImGuiNative.GetFloatRef(self.Handle, key, defaultValue);

    public static ref int GetIntRef(ImGuiStoragePtr self, uint key, int defaultValue = 0) =>
        ref *ImGuiNative.GetIntRef(self.Handle, key, defaultValue);

    public static ref void* GetVoidPtrRef(ImGuiStoragePtr self, uint key, void* defaultValue = null) =>
        ref *ImGuiNative.GetVoidPtrRef(self.Handle, key, defaultValue);

    public static ref T* GetPtrRef<T>(ImGuiStoragePtr self, uint key, T* defaultValue = null)
        where T : unmanaged =>
        ref *(T**)ImGuiNative.GetVoidPtrRef(self.Handle, key, defaultValue);

    public static ref T GetRef<T>(ImGuiStoragePtr self, uint key, T defaultValue = default)
        where T : unmanaged
    {
        if (sizeof(T) > sizeof(void*))
            throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);

        return ref *(T*)ImGuiNative.GetVoidPtrRef(self.Handle, key, *(void**)&defaultValue);
    }

    public static uint GetID(AutoUtf8Buffer strId)
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
}
