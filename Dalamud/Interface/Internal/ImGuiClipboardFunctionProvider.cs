using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal;

/// <summary>
/// Configures the ImGui clipboard behaviour to work nicely with XIV.
/// </summary>
/// <remarks>
/// <para>
/// XIV uses '\r' for line endings and will truncate all text after a '\n' character.
/// This means that copy/pasting multi-line text from ImGui to XIV will only copy the first line.
/// </para>
/// <para>
/// ImGui uses '\n' for line endings and will ignore '\r' entirely.
/// This means that copy/pasting multi-line text from XIV to ImGui will copy all the text
/// without line breaks.
/// </para>
/// <para>
/// To fix this we normalize all clipboard line endings entering/exiting ImGui to '\r\n' which
/// works for both ImGui and XIV.
/// </para>
/// </remarks>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ImGuiClipboardFunctionProvider : IServiceType, IDisposable
{
    private readonly nint clipboardUserDataOriginal;
    private readonly delegate* unmanaged<nint, byte*, void> setTextOriginal;
    private readonly delegate* unmanaged<nint, byte*> getTextOriginal;
    private GCHandle clipboardUserData;

    [ServiceManager.ServiceConstructor]
    private ImGuiClipboardFunctionProvider(InterfaceManager.InterfaceManagerWithScene imws)
    {
        // Effectively waiting for ImGui to become available.
        _ = imws;
        Debug.Assert(ImGuiHelpers.IsImGuiInitialized, "IMWS initialized but IsImGuiInitialized is false?");

        var io = ImGui.GetIO();
        this.setTextOriginal = (delegate* unmanaged<nint, byte*, void>)io.SetClipboardTextFn;
        this.getTextOriginal = (delegate* unmanaged<nint, byte*>)io.GetClipboardTextFn;
        this.clipboardUserDataOriginal = io.ClipboardUserData;
        io.SetClipboardTextFn = (nint)(delegate* unmanaged<nint, byte*, void>)(&StaticSetClipboardTextImpl);
        io.GetClipboardTextFn = (nint)(delegate* unmanaged<nint, byte*>)&StaticGetClipboardTextImpl;
        io.ClipboardUserData = GCHandle.ToIntPtr(this.clipboardUserData = GCHandle.Alloc(this));
        return;

        [UnmanagedCallersOnly]
        static void StaticSetClipboardTextImpl(nint userData, byte* text) =>
            ((ImGuiClipboardFunctionProvider)GCHandle.FromIntPtr(userData).Target)!.SetClipboardTextImpl(text);

        [UnmanagedCallersOnly]
        static byte* StaticGetClipboardTextImpl(nint userData) =>
            ((ImGuiClipboardFunctionProvider)GCHandle.FromIntPtr(userData).Target)!.GetClipboardTextImpl();
    }

    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute", Justification = "If it's null, it's crashworthy")]
    private static ImVectorWrapper<byte> ImGuiCurrentContextClipboardHandlerData =>
        new((ImVector*)(ImGui.GetCurrentContext() + 0x5520));

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.clipboardUserData.IsAllocated)
            return;

        var io = ImGui.GetIO();
        io.SetClipboardTextFn = (nint)this.setTextOriginal;
        io.GetClipboardTextFn = (nint)this.getTextOriginal;
        io.ClipboardUserData = this.clipboardUserDataOriginal;

        this.clipboardUserData.Free();
    }

    private void SetClipboardTextImpl(byte* text)
    {
        var buffer = ImGuiCurrentContextClipboardHandlerData;
        buffer.SetFromZeroTerminatedSequence(text);
        buffer.Utf8Normalize();
        buffer.AddZeroTerminatorIfMissing();
        this.setTextOriginal(this.clipboardUserDataOriginal, buffer.Data);
    }

    private byte* GetClipboardTextImpl()
    {
        _ = this.getTextOriginal(this.clipboardUserDataOriginal);

        var buffer = ImGuiCurrentContextClipboardHandlerData;
        buffer.TrimZeroTerminator();
        buffer.Utf8Normalize();
        buffer.AddZeroTerminatorIfMissing();
        return buffer.Data;
    }
}
