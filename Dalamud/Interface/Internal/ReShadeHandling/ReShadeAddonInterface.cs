using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Hooking;

using JetBrains.Annotations;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal.ReShadeHandling;

/// <summary>Managed receiver for Dalamud's process-lifetime ReShade addon registration.</summary>
internal sealed unsafe partial class ReShadeAddonInterface : IDisposable
{
    private const int ReShadeApiVersion = 1;

    private static readonly Lock RegistrationLock = new();

    // ReShade does not synchronize callback-vector mutation with dispatch. Keep native callbacks rooted
    // until process exit; only the managed receiver can be replaced.
    private static NativeRegistration? nativeRegistration;

    private readonly NativeRegistration registration;

    private ReShadeAddonInterface(NativeRegistration registration) => this.registration = registration;

    /// <summary>Delegate for <see cref="ReShadeAddonInterface.AddonEvent.Present"/>.</summary>
    /// <param name="commandQueue">Current command queue. Type: <c>api::command_queue</c>.</param>
    /// <param name="swapChain">Current swap chain. Type: <c>api::swapchain</c>.</param>
    /// <param name="sourceRect">Optional; source rectangle. May contain up to 1 element.</param>
    /// <param name="destRect">Optional; target rectangle. May contain up to 1 element.</param>
    /// <param name="dirtyRects">Dirty rectangles.</param>
    public delegate void PresentDelegate(
        ref ApiObject commandQueue,
        ref ApiObject swapChain,
        ReadOnlySpan<RECT> sourceRect,
        ReadOnlySpan<RECT> destRect,
        ReadOnlySpan<RECT> dirtyRects);

    /// <summary>Delegate for <see cref="ReShadeAddonInterface.AddonEvent.ReShadeOverlay"/>.</summary>
    /// <param name="effectRuntime">Reference to the ReShade runtime.</param>
    public delegate void ReShadeOverlayDelegate(ref ApiObject effectRuntime);

    /// <summary>Delegate for <see cref="ReShadeAddonInterface.AddonEvent.InitSwapChain"/>.</summary>
    /// <param name="swapChain">Reference to the ReShade SwapChain wrapper.</param>
    public delegate void ReShadeInitSwapChain(ref ApiObject swapChain);

    /// <summary>Delegate for <see cref="ReShadeAddonInterface.AddonEvent.DestroySwapChain"/>.</summary>
    /// <param name="swapChain">Reference to the ReShade SwapChain wrapper.</param>
    public delegate void ReShadeDestroySwapChain(ref ApiObject swapChain);

    /// <summary>Native delegate for <see cref="ReShadeAddonInterface.AddonEvent.Present"/>.</summary>
    /// <param name="commandQueue">Current command queue. Type: <c>api::command_queue</c>.</param>
    /// <param name="swapChain">Current swap chain. Type: <c>api::swapchain</c>.</param>
    /// <param name="pSourceRect">Optional; source rectangle.</param>
    /// <param name="pDestRect">Optional; target rectangle.</param>
    /// <param name="dirtyRectCount">Number of dirty rectangles.</param>
    /// <param name="pDirtyRects">Optional; dirty rectangles.</param>
    private delegate void UnsafePresentDelegate(
        ref ApiObject commandQueue,
        ref ApiObject swapChain,
        RECT* pSourceRect,
        RECT* pDestRect,
        uint dirtyRectCount,
        void* pDirtyRects);

    private delegate BOOL GetModuleHandleExWDelegate(uint dwFlags, ushort* lpModuleName, HMODULE* phModule);

    /// <summary>Called on <see cref="ReShadeAddonInterface.AddonEvent.Present"/>.</summary>
    public event PresentDelegate? Present;

    /// <summary>Called on <see cref="ReShadeAddonInterface.AddonEvent.ReShadeOverlay"/>.</summary>
    public event ReShadeOverlayDelegate? ReShadeOverlay;

    /// <summary>Called on <see cref="ReShadeAddonInterface.AddonEvent.InitSwapChain"/>.</summary>
    public event ReShadeInitSwapChain? InitSwapChain;

    /// <summary>Called on <see cref="ReShadeAddonInterface.AddonEvent.DestroySwapChain"/>.</summary>
    public event ReShadeDestroySwapChain? DestroySwapChain;

    /// <summary>Attaches to Dalamud's process-lifetime ReShade addon registration.</summary>
    /// <param name="r">The attached receiver, or <c>null</c> on failure.</param>
    /// <returns><c>true</c> if a receiver was attached.</returns>
    public static bool TryRegisterAddon([NotNullWhen(true)] out ReShadeAddonInterface? r)
    {
        using (RegistrationLock.EnterScope())
        {
            r = null;
            try
            {
                if (Exports.ReShadeRegisterAddon is null)
                    return false;

                if (nativeRegistration is null)
                {
                    // Publish the root first: partial registration must retain callbacks and prevent retries.
                    nativeRegistration = new();
                    nativeRegistration.Initialize();
                }

                return nativeRegistration.IsInitialized && nativeRegistration.TryAttach(out r);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // InterfaceManager drains admitted handlers separately. Detach without mutating native callback vectors.
        this.registration.Detach(this);
        this.Present = null;
        this.ReShadeOverlay = null;
        this.InitSwapChain = null;
        this.DestroySwapChain = null;
    }

    private void OnPresent(
        ref ApiObject commandQueue,
        ref ApiObject swapChain,
        RECT* pSourceRect,
        RECT* pDestRect,
        uint dirtyRectCount,
        void* pDirtyRects)
    {
        this.Present?.Invoke(
            ref commandQueue,
            ref swapChain,
            pSourceRect is null ? default : new(pSourceRect, 1),
            pDestRect is null ? default : new(pDestRect, 1),
            new(pDirtyRects, (int)dirtyRectCount));
    }

    private void OnReShadeOverlay(ref ApiObject runtime) => this.ReShadeOverlay?.Invoke(ref runtime);

    private void OnInitSwapChain(ref ApiObject swapChain) => this.InitSwapChain?.Invoke(ref swapChain);

    private void OnDestroySwapChain(ref ApiObject swapChain) => this.DestroySwapChain?.Invoke(ref swapChain);

    /// <summary>ReShade API object exposing its underlying native object.</summary>
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

    /// <summary>Roots a managed delegate alongside its native callback address.</summary>
    /// <typeparam name="T">The callback delegate type.</typeparam>
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

    /// <summary>Retains native callbacks across managed receiver lifetimes.</summary>
    private sealed class NativeRegistration
    {
        private readonly DelegateStorage<UnsafePresentDelegate> presentDelegate;
        private readonly DelegateStorage<ReShadeOverlayDelegate> reShadeOverlayDelegate;
        private readonly DelegateStorage<ReShadeInitSwapChain> initSwapChainDelegate;
        private readonly DelegateStorage<ReShadeDestroySwapChain> destroySwapChainDelegate;

        // Retain even the disposed import hook: a native caller may already have fetched its managed detour.
        private Hook<GetModuleHandleExWDelegate>? addonModuleResolverHook;
        private GetModuleHandleExWDelegate? getModuleHandleExWOriginal;
        private HMODULE hDalamudModule;

        private ReShadeAddonInterface? target;

        public NativeRegistration()
        {
            this.presentDelegate = new(
                (ref ApiObject queue, ref ApiObject swapChain, RECT* source, RECT* dest, uint count, void* dirty) =>
                    Volatile.Read(ref this.target)?.OnPresent(ref queue, ref swapChain, source, dest, count, dirty));
            this.reShadeOverlayDelegate = new((ref ApiObject rt) => Volatile.Read(ref this.target)?.OnReShadeOverlay(ref rt));
            this.initSwapChainDelegate = new((ref ApiObject rt) => Volatile.Read(ref this.target)?.OnInitSwapChain(ref rt));
            this.destroySwapChainDelegate = new((ref ApiObject rt) => Volatile.Read(ref this.target)?.OnDestroySwapChain(ref rt));
        }

        /// <summary>Gets a value indicating whether native registration completed successfully.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Attaches a receiver only when no receiver is active.</summary>
        /// <param name="addon">The attached receiver, or <c>null</c> if one is already active.</param>
        /// <returns><c>true</c> if a receiver was attached.</returns>
        public bool TryAttach([NotNullWhen(true)] out ReShadeAddonInterface? addon)
        {
            addon = new(this);
            if (Interlocked.CompareExchange(ref this.target, addon, null) is null)
                return true;

            addon = null;
            return false;
        }

        /// <summary>Detaches only the specified receiver, preserving any replacement attachment.</summary>
        /// <param name="addon">The receiver being disposed.</param>
        public void Detach(ReShadeAddonInterface addon) => Interlocked.CompareExchange(ref this.target, null, addon);

        /// <summary>Registers the addon and callbacks, retaining any published callbacks on failure.</summary>
        public void Initialize()
        {
            this.hDalamudModule = (HMODULE)Marshal.GetHINSTANCE(typeof(ReShadeAddonInterface).Assembly.ManifestModule);

            // https://github.com/crosire/reshade/commit/eaaa2a2c5adf5749ad17b358305da3f2d0f6baf4
            // The resolver is needed only while attributing callback registrations to this module.
            this.addonModuleResolverHook = Hook<GetModuleHandleExWDelegate>.FromImport(
                ReShadeModule!, "kernel32.dll", nameof(GetModuleHandleExW), 0, this.GetModuleHandleExWDetour);
            try
            {
                this.getModuleHandleExWOriginal = this.addonModuleResolverHook.Original;
                this.addonModuleResolverHook.Enable();
                if (!Exports.ReShadeRegisterAddon(this.hDalamudModule, ReShadeApiVersion))
                    throw new InvalidOperationException("ReShadeRegisterAddon failure.");

                Exports.ReShadeRegisterEvent(AddonEvent.Present, this.presentDelegate);
                Exports.ReShadeRegisterEvent(AddonEvent.ReShadeOverlay, this.reShadeOverlayDelegate);
                Exports.ReShadeRegisterEvent(AddonEvent.InitSwapChain, this.initSwapChainDelegate);
                Exports.ReShadeRegisterEvent(AddonEvent.DestroySwapChain, this.destroySwapChainDelegate);
            }
            finally
            {
                // Native dispatch may already reference a callback, even if registration failed partway through.
                this.addonModuleResolverHook.Dispose();
            }

            this.IsInitialized = true;
        }

        // Attribute managed callback thunks to the addon module while ReShade registers its events.
        private BOOL GetModuleHandleExWDetour(uint dwFlags, ushort* lpModuleName, HMODULE* phModule)
        {
            if ((dwFlags & GET.GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS) != 0 &&
                (dwFlags & GET.GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT) != 0 &&
                (lpModuleName == this.initSwapChainDelegate ||
                 lpModuleName == this.destroySwapChainDelegate ||
                 lpModuleName == this.presentDelegate ||
                 lpModuleName == this.reShadeOverlayDelegate))
            {
                *phModule = this.hDalamudModule;
                return BOOL.TRUE;
            }

            return this.getModuleHandleExWOriginal!(dwFlags, lpModuleName, phModule);
        }
    }
}
