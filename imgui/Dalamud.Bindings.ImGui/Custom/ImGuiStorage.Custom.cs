namespace Dalamud.Bindings.ImGui;

public unsafe partial struct ImGuiStorage
{
    public readonly ref bool GetBoolRef(uint key, bool defaultValue = false)
    {
        fixed (ImGuiStorage* thisPtr = &this)
            return ref ImGui.GetBoolRef(thisPtr, key, defaultValue);
    }

    public readonly ref float GetFloatRef(uint key, float defaultValue = 0.0f)
    {
        fixed (ImGuiStorage* thisPtr = &this)
            return ref ImGui.GetFloatRef(thisPtr, key, defaultValue);
    }

    public readonly ref int GetIntRef(uint key, int defaultValue = 0)
    {
        fixed (ImGuiStorage* thisPtr = &this)
            return ref ImGui.GetIntRef(thisPtr, key, defaultValue);
    }

    public readonly ref void* GetVoidPtrRef(uint key, void* defaultValue = null)
    {
        fixed (ImGuiStorage* thisPtr = &this)
            return ref ImGui.GetVoidPtrRef(thisPtr, key, defaultValue);
    }

    public readonly ref T* GetPtrRef<T>(uint key, T* defaultValue = null) where T : unmanaged
    {
        fixed (ImGuiStorage* thisPtr = &this)
            return ref ImGui.GetPtrRef(thisPtr, key, defaultValue);
    }

    public readonly ref T GetRef<T>(uint key, T defaultValue = default) where T : unmanaged
    {
        fixed (ImGuiStorage* thisPtr = &this)
            return ref ImGui.GetRef(thisPtr, key, defaultValue);
    }
}

public unsafe partial struct ImGuiStoragePtr
{
    public readonly ref bool GetBoolRef(uint key, bool defaultValue = false) =>
        ref ImGui.GetBoolRef(this, key, defaultValue);

    public readonly ref float GetFloatRef(uint key, float defaultValue = 0.0f) =>
        ref ImGui.GetFloatRef(this, key, defaultValue);

    public readonly ref int GetIntRef(uint key, int defaultValue = 0) => ref ImGui.GetIntRef(this, key, defaultValue);

    public readonly ref void* GetVoidPtrRef(uint key, void* defaultValue = null) =>
        ref ImGui.GetVoidPtrRef(this, key, defaultValue);

    public readonly ref T* GetPtrRef<T>(uint key, T* defaultValue = null) where T : unmanaged =>
        ref ImGui.GetPtrRef(this, key, defaultValue);

    public readonly ref T GetRef<T>(uint key, T defaultValue = default) where T : unmanaged =>
        ref ImGui.GetRef(this, key, defaultValue);
}
