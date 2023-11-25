using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using Win32 = TerraFX.Interop.Windows.Windows;

namespace Dalamud.ImGuiScene.Helpers;

/// <summary>
/// Wraps a <see cref="HANDLE"/>, that should be closed using <see cref="Win32.CloseHandle"/>.<br />
/// Wrapping anything else will result in an undefined behavior.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal readonly struct Win32Handle : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Win32Handle"/> struct.
    /// </summary>
    /// <param name="handle">The handle.</param>
    public Win32Handle(HANDLE handle) => this.Handle = handle;

    /// <summary>
    /// Gets the handle.
    /// </summary>
    public HANDLE Handle { get; }

    public static implicit operator HANDLE(Win32Handle t) => t.Handle;

    /// <summary>
    /// Creates a new instance of <see cref="Win32Handle"/>, by wrapping <see cref="Win32.CreateEventW"/>.
    /// </summary>
    /// <param name="securityAttributes">The optional security attributes.</param>
    /// <param name="manualReset">Whether the event is manual reset.</param>
    /// <param name="initialState">The initial state on whether the event is signaled.</param>
    /// <param name="name">The optional name of the pipe.</param>
    /// <returns>The new instance of <see cref="Win32Handle"/> containig a valid Win32 event handle.</returns>
    public static unsafe Win32Handle CreateEvent(
        in SECURITY_ATTRIBUTES securityAttributes = default,
        bool manualReset = true,
        bool initialState = false,
        string? name = null)
    {
        fixed (SECURITY_ATTRIBUTES* psa = &securityAttributes)
        fixed (char* pName = name)
        {
            var h = Win32.CreateEventW(psa, manualReset, initialState, (ushort*)pName);
            if (h == default)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new();
            return new(h);
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="Win32Handle"/>, by wrapping <see cref="ID3D12Device.CreateSharedHandle"/>.
    /// </summary>
    /// <param name="device">A pointer to an instance of <see cref="ID3D12Device"/>.</param>
    /// <param name="resource">The resource to share.</param>
    /// <param name="securityAttributes">The optional security attributes.</param>
    /// <param name="access">The access. Currently the only valid value is <see cref="Win32.GENERIC_ALL"/>.</param>
    /// <param name="name">The optional name of the shared object.</param>
    /// <typeparam name="TResource">The type of resource.</typeparam>
    /// <returns>The new instance of <see cref="Win32Handle"/> containig a valid shared DX12 resource handle.</returns>
    public static unsafe Win32Handle CreateSharedHandle<TResource>(
        ID3D12Device* device,
        TResource* resource,
        in SECURITY_ATTRIBUTES securityAttributes = default,
        uint access = Win32.GENERIC_ALL,
        string? name = null)
        where TResource : unmanaged, ID3D12DeviceChild.Interface
    {
        HANDLE handle;
        fixed (SECURITY_ATTRIBUTES* psa = &securityAttributes)
        fixed (char* pName = name)
            device->CreateSharedHandle((ID3D12DeviceChild*)resource, psa, access, (ushort*)pName, &handle).ThrowHr();
        return new(handle);
    }

    /// <inheritdoc/>
    public void Dispose() => Win32.CloseHandle(this.Handle);
}
