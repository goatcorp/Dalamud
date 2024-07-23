using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Hooking;

using JetBrains.Annotations;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.ReShadeHandling;

/// <summary>ReShade interface.</summary>
internal sealed unsafe partial class ReShadeAddonInterface : IDisposable
{
    private const int ReShadeApiVersion = 12;

    private readonly HMODULE hDalamudModule;

    private readonly Hook<GetModuleHandleExWDelegate> addonModuleResolverHook;

    private readonly DelegateStorage<ReShadeOverlayDelegate> reShadeOverlayDelegate;
    private readonly DelegateStorage<ReShadeInitSwapChain> initSwapChainDelegate;
    private readonly DelegateStorage<ReShadeDestroySwapChain> destroySwapChainDelegate;

    private bool requiresFinalize;

    private ReShadeAddonInterface()
    {
        this.hDalamudModule = (HMODULE)Marshal.GetHINSTANCE(typeof(ReShadeAddonInterface).Assembly.ManifestModule);
        if (!Exports.ReShadeRegisterAddon(this.hDalamudModule, ReShadeApiVersion))
            throw new InvalidOperationException("ReShadeRegisterAddon failure.");

        // https://github.com/crosire/reshade/commit/eaaa2a2c5adf5749ad17b358305da3f2d0f6baf4
        // TODO: when ReShade gets a proper release with this commit, make this hook optional
        this.addonModuleResolverHook = Hook<GetModuleHandleExWDelegate>.FromImport(
            ReShadeModule!,
            "kernel32.dll",
            nameof(GetModuleHandleExW),
            0,
            this.GetModuleHandleExWDetour);

        try
        {
            this.addonModuleResolverHook.Enable();
            Exports.ReShadeRegisterEvent(
                AddonEvent.ReShadeOverlay,
                this.reShadeOverlayDelegate = new((ref ApiObject rt) => this.ReShadeOverlay?.Invoke(ref rt)));
            Exports.ReShadeRegisterEvent(
                AddonEvent.InitSwapChain,
                this.initSwapChainDelegate = new((ref ApiObject rt) => this.InitSwapChain?.Invoke(ref rt)));
            Exports.ReShadeRegisterEvent(
                AddonEvent.DestroySwapChain,
                this.destroySwapChainDelegate = new((ref ApiObject rt) => this.DestroySwapChain?.Invoke(ref rt)));
        }
        catch
        {
            try
            {
                this.addonModuleResolverHook.Disable();
                this.addonModuleResolverHook.Dispose();
            }
            catch
            {
                // ignore
            }

            Exports.ReShadeUnregisterAddon(this.hDalamudModule);
            throw;
        }

        this.requiresFinalize = true;
    }

    /// <summary>Finalizes an instance of the <see cref="ReShadeAddonInterface"/> class.</summary>
    ~ReShadeAddonInterface() => this.ReleaseUnmanagedResources();

    /// <summary>Delegate for <see cref="ReShadeAddonInterface.AddonEvent.ReShadeOverlay"/>.</summary>
    /// <param name="effectRuntime">Reference to the ReShade runtime.</param>
    public delegate void ReShadeOverlayDelegate(ref ApiObject effectRuntime);

    /// <summary>Delegate for <see cref="ReShadeAddonInterface.AddonEvent.InitSwapChain"/>.</summary>
    /// <param name="swapChain">Reference to the ReShade SwapChain wrapper.</param>
    public delegate void ReShadeInitSwapChain(ref ApiObject swapChain);

    /// <summary>Delegate for <see cref="ReShadeAddonInterface.AddonEvent.DestroySwapChain"/>.</summary>
    /// <param name="swapChain">Reference to the ReShade SwapChain wrapper.</param>
    public delegate void ReShadeDestroySwapChain(ref ApiObject swapChain);

    private delegate BOOL GetModuleHandleExWDelegate(uint dwFlags, ushort* lpModuleName, HMODULE* phModule);

    /// <summary>Called on <see cref="ReShadeAddonInterface.AddonEvent.ReShadeOverlay"/>.</summary>
    public event ReShadeOverlayDelegate? ReShadeOverlay;

    /// <summary>Called on <see cref="ReShadeAddonInterface.AddonEvent.InitSwapChain"/>.</summary>
    public event ReShadeInitSwapChain? InitSwapChain;

    /// <summary>Called on <see cref="ReShadeAddonInterface.AddonEvent.DestroySwapChain"/>.</summary>
    public event ReShadeDestroySwapChain? DestroySwapChain;

    /// <summary>Registers Dalamud as a ReShade addon.</summary>
    /// <param name="r">Initialized interface.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryRegisterAddon([NotNullWhen(true)] out ReShadeAddonInterface? r)
    {
        try
        {
            r = Exports.ReShadeRegisterAddon is null ? null : new();
            return r is not null;
        }
        catch
        {
            r = null;
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        if (!this.requiresFinalize)
            return;
        this.requiresFinalize = false;
        Exports.ReShadeUnregisterAddon(this.hDalamudModule);
        this.addonModuleResolverHook.Disable();
        this.addonModuleResolverHook.Dispose();
    }

    private BOOL GetModuleHandleExWDetour(uint dwFlags, ushort* lpModuleName, HMODULE* phModule)
    {
        if ((dwFlags & GET.GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS) == 0)
            return this.addonModuleResolverHook.Original(dwFlags, lpModuleName, phModule);
        if ((dwFlags & GET.GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT) == 0)
            return this.addonModuleResolverHook.Original(dwFlags, lpModuleName, phModule);
        if (lpModuleName == this.initSwapChainDelegate ||
            lpModuleName == this.destroySwapChainDelegate ||
            lpModuleName == this.reShadeOverlayDelegate)
        {
            *phModule = this.hDalamudModule;
            return BOOL.TRUE;
        }

        return this.addonModuleResolverHook.Original(dwFlags, lpModuleName, phModule);
    }

    /// <summary>ReShade effect runtime object.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ApiObject
    {
        /// <summary>The vtable.</summary>
        public VTable* Vtbl;

        /// <summary>Gets this object as a typed pointer.</summary>
        /// <returns>Address of this instance.</returns>
        /// <remarks>This call is invalid if this object is not already fixed.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ApiObject* AsPointer() => (ApiObject*)Unsafe.AsPointer(ref this);

        /// <summary>Gets the native object.</summary>
        /// <returns>The native object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nint GetNative() => this.Vtbl->GetNative(this.AsPointer());

        /// <inheritdoc cref="GetNative"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* GetNative<T>() where T : unmanaged => (T*)this.GetNative();

        /// <summary>VTable of <see cref="ApiObject"/>.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct VTable
        {
            /// <inheritdoc cref="ApiObject.GetNative"/>
            public delegate* unmanaged<ApiObject*, nint> GetNative;
        }
    }

    private readonly struct DelegateStorage<T> where T : Delegate
    {
        [UsedImplicitly]
        public readonly T Delegate;

        public readonly void* Address;

        public DelegateStorage(T @delegate)
        {
            this.Delegate = @delegate;
            this.Address = (void*)Marshal.GetFunctionPointerForDelegate(@delegate);
        }

        public static implicit operator void*(DelegateStorage<T> sto) => sto.Address;
    }
}
