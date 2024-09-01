using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Memory;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Textures.Internal;

/// <summary>Service responsible for loading and disposing ImGui texture wraps.</summary>
internal sealed partial class TextureManager
{
    /// <inheritdoc/>
    public async Task CopyToClipboardAsync(
        IDalamudTextureWrap wrap,
        string? preferredFileNameWithoutExtension = null,
        bool leaveWrapOpen = false,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this.disposeCts.IsCancellationRequested, this);

        using var wrapAux = new WrapAux(wrap, leaveWrapOpen);
        bool hasAlphaChannel;
        switch (wrapAux.Desc.Format)
        {
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                hasAlphaChannel = false;
                break;
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
            case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                hasAlphaChannel = true;
                break;
            default:
                await this.CopyToClipboardAsync(
                    await this.CreateFromExistingTextureAsync(
                        wrap,
                        new() { Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },
                        cancellationToken: cancellationToken),
                    preferredFileNameWithoutExtension,
                    false,
                    cancellationToken);
                return;
        }

        // https://stackoverflow.com/questions/15689541/win32-clipboard-and-alpha-channel-images
        // https://learn.microsoft.com/en-us/windows/win32/shell/clipboard
        using var pdo = default(ComPtr<IDataObject>);
        unsafe
        {
            fixed (Guid* piid = &IID.IID_IDataObject)
                SHCreateDataObject(null, 1, null, null, piid, (void**)pdo.GetAddressOf()).ThrowOnError();
        }

        var ms = new MemoryStream();
        {
            ms.SetLength(ms.Position = 0);
            await this.SaveToStreamAsync(
                wrap,
                GUID.GUID_ContainerFormatPng,
                ms,
                new Dictionary<string, object> { ["InterlaceOption"] = true },
                true,
                true,
                cancellationToken);

            unsafe
            {
                using var ims = default(ComPtr<IStream>);
                fixed (byte* p = ms.GetBuffer())
                    ims.Attach(SHCreateMemStream(p, (uint)ms.Length));
                if (ims.IsEmpty())
                    throw new OutOfMemoryException();

                AddToDataObject(
                    pdo,
                    ClipboardFormats.Png,
                    new()
                    {
                        tymed = (uint)TYMED.TYMED_ISTREAM,
                        pstm = ims.Get(),
                    });
                AddToDataObject(
                    pdo,
                    ClipboardFormats.FileContents,
                    new()
                    {
                        tymed = (uint)TYMED.TYMED_ISTREAM,
                        pstm = ims.Get(),
                    });
                ims.Get()->AddRef();
                ims.Detach();
            }

            if (preferredFileNameWithoutExtension is not null)
            {
                unsafe
                {
                    preferredFileNameWithoutExtension += ".png";
                    if (preferredFileNameWithoutExtension.Length >= 260)
                        preferredFileNameWithoutExtension = preferredFileNameWithoutExtension[..^4] + ".png";
                    var namea = (CodePagesEncodingProvider.Instance.GetEncoding(0) ?? Encoding.UTF8)
                        .GetBytes(preferredFileNameWithoutExtension);
                    if (namea.Length > 260)
                    {
                        namea.AsSpan()[^4..].CopyTo(namea.AsSpan(256, 4));
                        Array.Resize(ref namea, 260);
                    }

                    var fgda = new FILEGROUPDESCRIPTORA
                    {
                        cItems = 1,
                        fgd = new()
                        {
                            e0 = new()
                            {
                                dwFlags = unchecked((uint)FD_FLAGS.FD_FILESIZE | (uint)FD_FLAGS.FD_UNICODE),
                                nFileSizeHigh = (uint)(ms.Length >> 32),
                                nFileSizeLow = (uint)ms.Length,
                            },
                        },
                    };
                    namea.AsSpan().CopyTo(new(fgda.fgd.e0.cFileName, 260));

                    AddToDataObject(
                        pdo,
                        ClipboardFormats.FileDescriptorA,
                        new()
                        {
                            tymed = (uint)TYMED.TYMED_HGLOBAL,
                            hGlobal = CreateHGlobalFromMemory<FILEGROUPDESCRIPTORA>(new(ref fgda)),
                        });

                    var fgdw = new FILEGROUPDESCRIPTORW
                    {
                        cItems = 1,
                        fgd = new()
                        {
                            e0 = new()
                            {
                                dwFlags = unchecked((uint)FD_FLAGS.FD_FILESIZE | (uint)FD_FLAGS.FD_UNICODE),
                                nFileSizeHigh = (uint)(ms.Length >> 32),
                                nFileSizeLow = (uint)ms.Length,
                            },
                        },
                    };
                    preferredFileNameWithoutExtension.AsSpan().CopyTo(new(fgdw.fgd.e0.cFileName, 260));

                    AddToDataObject(
                        pdo,
                        ClipboardFormats.FileDescriptorW,
                        new()
                        {
                            tymed = (uint)TYMED.TYMED_HGLOBAL,
                            hGlobal = CreateHGlobalFromMemory<FILEGROUPDESCRIPTORW>(new(ref fgdw)),
                        });
                }
            }
        }

        {
            ms.SetLength(ms.Position = 0);
            await this.SaveToStreamAsync(
                wrap,
                GUID.GUID_ContainerFormatBmp,
                ms,
                new Dictionary<string, object> { ["EnableV5Header32bppBGRA"] = false },
                true,
                true,
                cancellationToken);
            AddToDataObject(
                pdo,
                CF.CF_DIB,
                new()
                {
                    tymed = (uint)TYMED.TYMED_HGLOBAL,
                    hGlobal = CreateHGlobalFromMemory<byte>(
                        ms.GetBuffer().AsSpan(0, (int)ms.Length)[Unsafe.SizeOf<BITMAPFILEHEADER>()..]),
                });
        }

        if (hasAlphaChannel)
        {
            ms.SetLength(ms.Position = 0);
            await this.SaveToStreamAsync(
                wrap,
                GUID.GUID_ContainerFormatBmp,
                ms,
                new Dictionary<string, object> { ["EnableV5Header32bppBGRA"] = true },
                true,
                true,
                cancellationToken);
            AddToDataObject(
                pdo,
                CF.CF_DIBV5,
                new()
                {
                    tymed = (uint)TYMED.TYMED_HGLOBAL,
                    hGlobal = CreateHGlobalFromMemory<byte>(
                        ms.GetBuffer().AsSpan(0, (int)ms.Length)[Unsafe.SizeOf<BITMAPFILEHEADER>()..]),
                });
        }

        var omts = await Service<StaThreadService>.GetAsync();
        await omts.Run(() => StaThreadService.OleSetClipboard(pdo), cancellationToken);

        return;

        static unsafe void AddToDataObject(ComPtr<IDataObject> pdo, uint clipboardFormat, STGMEDIUM stg)
        {
            var fec = new FORMATETC
            {
                cfFormat = (ushort)clipboardFormat,
                ptd = null,
                dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
                lindex = 0,
                tymed = stg.tymed,
            };
            pdo.Get()->SetData(&fec, &stg, true).ThrowOnError();
        }

        static unsafe HGLOBAL CreateHGlobalFromMemory<T>(ReadOnlySpan<T> data) where T : unmanaged
        {
            var h = GlobalAlloc(GMEM.GMEM_MOVEABLE, (nuint)(data.Length * sizeof(T)));
            if (h == 0)
                throw new OutOfMemoryException("Failed to allocate.");

            var p = GlobalLock(h);
            data.CopyTo(new(p, data.Length));
            GlobalUnlock(h);
            return h;
        }
    }

    /// <inheritdoc/>
    public bool HasClipboardImage()
    {
        var acf = Service<StaThreadService>.Get().AvailableClipboardFormats;
        return acf.Contains(CF.CF_DIBV5)
               || acf.Contains(CF.CF_DIB)
               || acf.Contains(ClipboardFormats.Png)
               || acf.Contains(ClipboardFormats.FileContents);
    }

    /// <inheritdoc/>
    public async Task<IDalamudTextureWrap> CreateFromClipboardAsync(
        string? debugName = null,
        CancellationToken cancellationToken = default)
    {
        var omts = await Service<StaThreadService>.GetAsync();
        var (stgm, clipboardFormat) = await omts.Run(GetSupportedClipboardData, cancellationToken);

        try
        {
            return this.BlameSetName(
                await this.DynamicPriorityTextureLoader.LoadAsync(
                    null,
                    ct =>
                        clipboardFormat is CF.CF_DIB or CF.CF_DIBV5
                            ? CreateTextureFromStorageMediumDib(this, stgm, ct)
                            : CreateTextureFromStorageMedium(this, stgm, ct),
                    cancellationToken),
                debugName ?? $"{nameof(this.CreateFromClipboardAsync)}({(TYMED)stgm.tymed})");
        }
        finally
        {
            StaThreadService.ReleaseStgMedium(ref stgm);
        }

        // Converts a CF_DIB/V5 format to a full BMP format, for WIC consumption.
        static unsafe Task<IDalamudTextureWrap> CreateTextureFromStorageMediumDib(
            TextureManager textureManager,
            scoped in STGMEDIUM stgm,
            CancellationToken ct)
        {
            var ms = new MemoryStream();
            switch ((TYMED)stgm.tymed)
            {
                case TYMED.TYMED_HGLOBAL when stgm.hGlobal != default:
                {
                    var pMem = GlobalLock(stgm.hGlobal);
                    if (pMem is null)
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    try
                    {
                        var size = (int)GlobalSize(stgm.hGlobal);
                        ms.SetLength(sizeof(BITMAPFILEHEADER) + size);
                        new ReadOnlySpan<byte>(pMem, size).CopyTo(ms.GetBuffer().AsSpan(sizeof(BITMAPFILEHEADER)));
                    }
                    finally
                    {
                        GlobalUnlock(stgm.hGlobal);
                    }

                    break;
                }

                case TYMED.TYMED_ISTREAM when stgm.pstm is not null:
                {
                    STATSTG stat;
                    if (stgm.pstm->Stat(&stat, (uint)STATFLAG.STATFLAG_NONAME).SUCCEEDED && stat.cbSize.QuadPart > 0)
                        ms.SetLength(sizeof(BITMAPFILEHEADER) + (int)stat.cbSize.QuadPart);
                    else
                        ms.SetLength(8192);

                    var offset = (uint)sizeof(BITMAPFILEHEADER);
                    for (var read = 1u; read != 0;)
                    {
                        if (offset == ms.Length)
                            ms.SetLength(ms.Length * 2);
                        fixed (byte* pMem = ms.GetBuffer().AsSpan((int)offset))
                        {
                            stgm.pstm->Read(pMem, (uint)(ms.Length - offset), &read).ThrowOnError();
                            offset += read;
                        }
                    }

                    ms.SetLength(offset);
                    break;
                }

                default:
                    return Task.FromException<IDalamudTextureWrap>(new NotSupportedException());
            }

            ref var bfh = ref Unsafe.As<byte, BITMAPFILEHEADER>(ref ms.GetBuffer()[0]);
            bfh.bfType = 0x4D42;
            bfh.bfSize = (uint)ms.Length;

            ref var bih = ref Unsafe.As<byte, BITMAPINFOHEADER>(ref ms.GetBuffer()[sizeof(BITMAPFILEHEADER)]);
            bfh.bfOffBits = (uint)(sizeof(BITMAPFILEHEADER) + bih.biSize);

            if (bih.biSize >= sizeof(BITMAPINFOHEADER))
            {
                if (bih.biBitCount > 8)
                {
                    if (bih.biCompression == BI.BI_BITFIELDS)
                        bfh.bfOffBits += (uint)(3 * sizeof(RGBQUAD));
                    else if (bih.biCompression == 6 /* BI_ALPHABITFIELDS */)
                        bfh.bfOffBits += (uint)(4 * sizeof(RGBQUAD));
                }
            }

            if (bih.biClrUsed > 0)
                bfh.bfOffBits += (uint)(bih.biClrUsed * sizeof(RGBQUAD));
            else if (bih.biBitCount <= 8)
                bfh.bfOffBits += (uint)(sizeof(RGBQUAD) << bih.biBitCount);

            using var pinned = ms.GetBuffer().AsMemory().Pin();
            using var strm = textureManager.Wic.CreateIStreamViewOfMemory(pinned, (int)ms.Length);
            return Task.FromResult(textureManager.Wic.NoThrottleCreateFromWicStream(strm, ct));
        }

        // Interprets a data as an image file using WIC.
        static unsafe Task<IDalamudTextureWrap> CreateTextureFromStorageMedium(
            TextureManager textureManager,
            scoped in STGMEDIUM stgm,
            CancellationToken ct)
        {
            switch ((TYMED)stgm.tymed)
            {
                case TYMED.TYMED_HGLOBAL when stgm.hGlobal != default:
                {
                    var pMem = GlobalLock(stgm.hGlobal);
                    if (pMem is null)
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    try
                    {
                        var size = (int)GlobalSize(stgm.hGlobal);
                        using var strm = textureManager.Wic.CreateIStreamViewOfMemory(pMem, size);
                        return Task.FromResult(textureManager.Wic.NoThrottleCreateFromWicStream(strm, ct));
                    }
                    finally
                    {
                        GlobalUnlock(stgm.hGlobal);
                    }
                }

                case TYMED.TYMED_FILE when stgm.lpszFileName is not null:
                {
                    var fileName = MemoryHelper.ReadString((nint)stgm.lpszFileName, Encoding.Unicode, short.MaxValue);
                    return textureManager.NoThrottleCreateFromFileAsync(fileName, ct);
                }

                case TYMED.TYMED_ISTREAM when stgm.pstm is not null:
                {
                    using var strm = new ComPtr<IStream>(stgm.pstm);
                    return Task.FromResult(textureManager.Wic.NoThrottleCreateFromWicStream(strm, ct));
                }

                default:
                    return Task.FromException<IDalamudTextureWrap>(new NotSupportedException());
            }
        }

        static unsafe bool TryGetClipboardDataAs(
            ComPtr<IDataObject> pdo,
            uint clipboardFormat,
            uint tymed,
            out STGMEDIUM stgm)
        {
            var fec = new FORMATETC
            {
                cfFormat = (ushort)clipboardFormat,
                ptd = null,
                dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = tymed,
            };
            fixed (STGMEDIUM* pstgm = &stgm)
                return pdo.Get()->GetData(&fec, pstgm).SUCCEEDED;
        }

        // Takes a data from clipboard for use with WIC.
        static unsafe (STGMEDIUM Stgm, uint ClipboardFormat) GetSupportedClipboardData()
        {
            using var pdo = StaThreadService.OleGetClipboard();
            const uint tymeds = (uint)TYMED.TYMED_HGLOBAL |
                                (uint)TYMED.TYMED_FILE |
                                (uint)TYMED.TYMED_ISTREAM;
            const uint sharedRead = STGM.STGM_READ | STGM.STGM_SHARE_DENY_WRITE;

            // Try taking data from clipboard as-is.
            if (TryGetClipboardDataAs(pdo, CF.CF_DIBV5, tymeds, out var stgm))
                return (stgm, CF.CF_DIBV5);
            if (TryGetClipboardDataAs(pdo, ClipboardFormats.FileContents, tymeds, out stgm))
                return (stgm, ClipboardFormats.FileContents);
            if (TryGetClipboardDataAs(pdo, ClipboardFormats.Png, tymeds, out stgm))
                return (stgm, ClipboardFormats.Png);
            if (TryGetClipboardDataAs(pdo, CF.CF_DIB, tymeds, out stgm))
                return (stgm, CF.CF_DIB);

            // Try reading file from the path stored in clipboard.
            if (TryGetClipboardDataAs(pdo, ClipboardFormats.FileNameW, (uint)TYMED.TYMED_HGLOBAL, out stgm))
            {
                var pPath = GlobalLock(stgm.hGlobal);
                try
                {
                    IStream* pfs;
                    SHCreateStreamOnFileW((ushort*)pPath, sharedRead, &pfs).ThrowOnError();

                    var stgm2 = new STGMEDIUM
                    {
                        tymed = (uint)TYMED.TYMED_ISTREAM,
                        pstm = pfs,
                        pUnkForRelease = (IUnknown*)pfs,
                    };
                    return (stgm2, ClipboardFormats.FileContents);
                }
                finally
                {
                    if (pPath is not null)
                        GlobalUnlock(stgm.hGlobal);
                    StaThreadService.ReleaseStgMedium(ref stgm);
                }
            }

            if (TryGetClipboardDataAs(pdo, ClipboardFormats.FileNameA, (uint)TYMED.TYMED_HGLOBAL, out stgm))
            {
                var pPath = GlobalLock(stgm.hGlobal);
                try
                {
                    IStream* pfs;
                    SHCreateStreamOnFileA((sbyte*)pPath, sharedRead, &pfs).ThrowOnError();

                    var stgm2 = new STGMEDIUM
                    {
                        tymed = (uint)TYMED.TYMED_ISTREAM,
                        pstm = pfs,
                        pUnkForRelease = (IUnknown*)pfs,
                    };
                    return (stgm2, ClipboardFormats.FileContents);
                }
                finally
                {
                    if (pPath is not null)
                        GlobalUnlock(stgm.hGlobal);
                    StaThreadService.ReleaseStgMedium(ref stgm);
                }
            }

            throw new InvalidOperationException("No compatible clipboard format found.");
        }
    }
}
