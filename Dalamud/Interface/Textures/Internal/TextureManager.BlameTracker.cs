using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using TerraFX.Interop;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    private readonly List<BlameTag> blameTracker = new();

    /// <summary>A wrapper for underlying texture2D resources.</summary>
    public interface IBlameableDalamudTextureWrap : IDalamudTextureWrap
    {
        /// <summary>Gets the name of the underlying resource of this texture wrap.</summary>
        public string Name { get; }

        /// <summary>Gets the format of the texture.</summary>
        public DXGI_FORMAT Format { get; }

        /// <summary>Gets the list of owner plugins.</summary>
        public List<LocalPlugin> OwnerPlugins { get; }
    }

    /// <summary>Gets all the loaded textures from plugins.</summary>
    /// <returns>The enumerable that goes through all textures and relevant plugins.</returns>
    /// <remarks>Returned value must be used inside a lock.</remarks>
    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Caller locks the return value.")]
    public IReadOnlyList<IBlameableDalamudTextureWrap> AllBlamesForDebug => this.blameTracker;

    /// <summary>Puts a plugin on blame for a texture.</summary>
    /// <param name="textureWrap">The texture.</param>
    /// <param name="ownerPlugin">The plugin.</param>
    /// <returns>Same <paramref name="textureWrap"/>.</returns>
    public unsafe IDalamudTextureWrap Blame(IDalamudTextureWrap textureWrap, LocalPlugin? ownerPlugin)
    {
        if (!this.dalamudConfiguration.UseTexturePluginTracking)
            return textureWrap;

        try
        {
            if (textureWrap.ImGuiHandle == nint.Zero)
                return textureWrap;
        }
        catch (ObjectDisposedException)
        {
            return textureWrap;
        }

        using var wrapAux = new WrapAux(textureWrap, true);
        var blame = BlameTag.From(wrapAux.ResPtr, out var isNew);

        if (ownerPlugin is not null)
        {
            lock (blame.OwnerPlugins)
                blame.OwnerPlugins.Add(ownerPlugin);
        }

        if (isNew)
        {
            lock (this.blameTracker)
                this.blameTracker.Add(blame);
        }

        return textureWrap;
    }

    /// <summary>Sets the blame name for a texture.</summary>
    /// <param name="textureWrap">The texture.</param>
    /// <param name="name">The name.</param>
    /// <returns>Same <paramref name="textureWrap"/>.</returns>
    public unsafe IDalamudTextureWrap BlameSetName(IDalamudTextureWrap textureWrap, string name)
    {
        if (!this.dalamudConfiguration.UseTexturePluginTracking)
            return textureWrap;

        try
        {
            if (textureWrap.ImGuiHandle == nint.Zero)
                return textureWrap;
        }
        catch (ObjectDisposedException)
        {
            return textureWrap;
        }

        using var wrapAux = new WrapAux(textureWrap, true);
        var blame = BlameTag.From(wrapAux.ResPtr, out var isNew);
        blame.Name = name;

        if (isNew)
        {
            lock (this.blameTracker)
                this.blameTracker.Add(blame);
        }

        return textureWrap;
    }

    private void BlameTrackerUpdate(IFramework unused)
    {
        lock (this.blameTracker)
        {
            for (var i = 0; i < this.blameTracker.Count;)
            {
                var entry = this.blameTracker[i];
                if (entry.TestIsReleasedOrShouldRelease())
                {
                    this.blameTracker[i] = this.blameTracker[^1];
                    this.blameTracker.RemoveAt(this.blameTracker.Count - 1);
                }
                else
                {
                    ++i;
                }
            }
        }
    }

    /// <summary>A COM object that works by tagging itself to a DirectX resource. When the resource destructs, it will
    /// also release our instance of the tag, letting us know that it is no longer being used, and can be evicted from
    /// our tracker.</summary>
    [Guid("2c3809e4-4f22-4c50-abde-4f22e5120875")]
    private sealed unsafe class BlameTag : IUnknown.Interface, IRefCountable, IBlameableDalamudTextureWrap
    {
        private static readonly Guid MyGuid = typeof(BlameTag).GUID;

        private readonly nint[] comObject;
        private readonly IUnknown.Vtbl<IUnknown> vtbl;
        private readonly D3D11_TEXTURE2D_DESC desc;

        private ID3D11Texture2D* tex2D;
        private GCHandle gchThis;
        private GCHandle gchComObject;
        private GCHandle gchVtbl;
        private int refCount;

        private ComPtr<ID3D11ShaderResourceView> srvDebugPreview;
        private long srvDebugPreviewExpiryTick;

        private BlameTag(IUnknown* trackWhat)
        {
            try
            {
                fixed (Guid* piid = &IID.IID_ID3D11Texture2D)
                fixed (ID3D11Texture2D** ppTex2D = &this.tex2D)
                    trackWhat->QueryInterface(piid, (void**)ppTex2D).ThrowOnError();

                fixed (D3D11_TEXTURE2D_DESC* pDesc = &this.desc)
                    this.tex2D->GetDesc(pDesc);

                this.comObject = new nint[2];

                this.vtbl.QueryInterface = &QueryInterfaceStatic;
                this.vtbl.AddRef = &AddRefStatic;
                this.vtbl.Release = &ReleaseStatic;

                this.gchThis = GCHandle.Alloc(this);
                this.gchVtbl = GCHandle.Alloc(this.vtbl, GCHandleType.Pinned);
                this.gchComObject = GCHandle.Alloc(this.comObject, GCHandleType.Pinned);
                this.comObject[0] = this.gchVtbl.AddrOfPinnedObject();
                this.comObject[1] = GCHandle.ToIntPtr(this.gchThis);
                this.refCount = 1;
            }
            catch
            {
                this.refCount = 0;
                if (this.gchComObject.IsAllocated)
                    this.gchComObject.Free();
                if (this.gchVtbl.IsAllocated)
                    this.gchVtbl.Free();
                if (this.gchThis.IsAllocated)
                    this.gchThis.Free();
                this.tex2D->Release();
                throw;
            }

            try
            {
                fixed (Guid* pMyGuid = &MyGuid)
                    this.tex2D->SetPrivateDataInterface(pMyGuid, this).ThrowOnError();
            }
            finally
            {
                // We don't own this.
                this.tex2D->Release();

                // If the try block above failed, then we will dispose ourselves right away.
                // Otherwise, we are transferring our ownership to the device child tagging system.
                this.Release();
            }

            return;

            [UnmanagedCallersOnly]
            static int QueryInterfaceStatic(IUnknown* pThis, Guid* riid, void** ppvObject) =>
                ToManagedObject(pThis)?.QueryInterface(riid, ppvObject) ?? E.E_UNEXPECTED;

            [UnmanagedCallersOnly]
            static uint AddRefStatic(IUnknown* pThis) => (uint)(ToManagedObject(pThis)?.AddRef() ?? 0);

            [UnmanagedCallersOnly]
            static uint ReleaseStatic(IUnknown* pThis) => (uint)(ToManagedObject(pThis)?.Release() ?? 0);
        }

        /// <inheritdoc cref="INativeGuid.NativeGuid"/>
        public static Guid* NativeGuid => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in MyGuid));

        /// <inheritdoc/>
        public List<LocalPlugin> OwnerPlugins { get; } = new();

        /// <inheritdoc/>
        public string Name { get; set; } = "<unnamed>";

        /// <inheritdoc/>
        public DXGI_FORMAT Format => this.desc.Format;

        /// <inheritdoc/>
        public IntPtr ImGuiHandle
        {
            get
            {
                if (this.refCount == 0)
                    return Service<DalamudAssetManager>.Get().Empty4X4.ImGuiHandle;

                this.srvDebugPreviewExpiryTick = Environment.TickCount64 + 1000;
                if (!this.srvDebugPreview.IsEmpty())
                    return (nint)this.srvDebugPreview.Get();
                var srvDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC(
                    this.tex2D,
                    D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2D);

                using var device = default(ComPtr<ID3D11Device>);
                this.tex2D->GetDevice(device.GetAddressOf());

                using var srv = default(ComPtr<ID3D11ShaderResourceView>);
                if (device.Get()->CreateShaderResourceView((ID3D11Resource*)this.tex2D, &srvDesc, srv.GetAddressOf())
                    .FAILED)
                    return Service<DalamudAssetManager>.Get().Empty4X4.ImGuiHandle;

                srv.Swap(ref this.srvDebugPreview);
                return (nint)this.srvDebugPreview.Get();
            }
        }

        /// <inheritdoc/>
        public int Width => (int)this.desc.Width;

        /// <inheritdoc/>
        public int Height => (int)this.desc.Height;

        public static implicit operator IUnknown*(BlameTag bt) => (IUnknown*)bt.gchComObject.AddrOfPinnedObject();

        /// <summary>Gets or creates an instance of <see cref="BlameTag"/> for the given resource.</summary>
        /// <param name="trackWhat">The COM object to track.</param>
        /// <param name="isNew"><c>true</c> if the tracker is new.</param>
        /// <typeparam name="T">A COM object type.</typeparam>
        /// <returns>A new instance of <see cref="BlameTag"/>.</returns>
        public static BlameTag From<T>(T* trackWhat, out bool isNew) where T : unmanaged, IUnknown.Interface
        {
            using var deviceChild = default(ComPtr<ID3D11DeviceChild>);
            fixed (Guid* piid = &IID.IID_ID3D11DeviceChild)
                trackWhat->QueryInterface(piid, (void**)deviceChild.GetAddressOf()).ThrowOnError();

            fixed (Guid* pMyGuid = &MyGuid)
            {
                var dataSize = (uint)sizeof(nint);
                IUnknown* existingTag;
                if (deviceChild.Get()->GetPrivateData(pMyGuid, &dataSize, &existingTag).SUCCEEDED)
                {
                    if (ToManagedObject(existingTag) is { } existingTagInstance)
                    {
                        existingTagInstance.Release();
                        isNew = false;
                        return existingTagInstance;
                    }
                }
            }

            isNew = true;
            return new((IUnknown*)trackWhat);
        }

        /// <summary>Tests whether the tag and the underlying resource are released or should be released.</summary>
        /// <returns><c>true</c> if there are no more remaining references to this instance.</returns>
        public bool TestIsReleasedOrShouldRelease()
        {
            if (this.srvDebugPreviewExpiryTick <= Environment.TickCount64)
                this.srvDebugPreview.Reset();

            return this.refCount == 0;
        }

        /// <inheritdoc/>
        public HRESULT QueryInterface(Guid* riid, void** ppvObject)
        {
            if (ppvObject == null)
                return E.E_POINTER;

            if (*riid == IID.IID_IUnknown ||
                *riid == MyGuid)
            {
                try
                {
                    this.AddRef();
                }
                catch
                {
                    return E.E_FAIL;
                }

                *ppvObject = (IUnknown*)this;
                return S.S_OK;
            }

            *ppvObject = null;
            return E.E_NOINTERFACE;
        }

        /// <inheritdoc/>
        public int AddRef() => IRefCountable.AlterRefCount(1, ref this.refCount, out var newRefCount) switch
        {
            IRefCountable.RefCountResult.StillAlive => newRefCount,
            IRefCountable.RefCountResult.AlreadyDisposed => throw new ObjectDisposedException(nameof(BlameTag)),
            IRefCountable.RefCountResult.FinalRelease => throw new InvalidOperationException(),
            _ => throw new InvalidOperationException(),
        };

        /// <inheritdoc/>
        public int Release()
        {
            switch (IRefCountable.AlterRefCount(-1, ref this.refCount, out var newRefCount))
            {
                case IRefCountable.RefCountResult.StillAlive:
                    return newRefCount;

                case IRefCountable.RefCountResult.FinalRelease:
                    this.gchThis.Free();
                    this.gchComObject.Free();
                    this.gchVtbl.Free();
                    return newRefCount;

                case IRefCountable.RefCountResult.AlreadyDisposed:
                    throw new ObjectDisposedException(nameof(BlameTag));

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <inheritdoc/>
        uint IUnknown.Interface.AddRef()
        {
            try
            {
                return (uint)this.AddRef();
            }
            catch
            {
                return 0;
            }
        }

        /// <inheritdoc/>
        uint IUnknown.Interface.Release()
        {
            this.srvDebugPreviewExpiryTick = 0;
            try
            {
                return (uint)this.Release();
            }
            catch
            {
                return 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BlameTag? ToManagedObject(void* pThis) =>
            GCHandle.FromIntPtr(((nint*)pThis)[1]).Target as BlameTag;
    }
}
