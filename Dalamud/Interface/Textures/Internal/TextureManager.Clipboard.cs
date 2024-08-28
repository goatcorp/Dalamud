using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Textures.TextureWraps;
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
                    cancellationToken: cancellationToken);
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
            AddToDataObject(
                pdo,
                ClipboardFormatFromName("PNG"),
                new()
                {
                    tymed = (uint)TYMED.TYMED_HGLOBAL,
                    hGlobal = CreateHGlobalFromMemory<byte>(ms.GetBuffer().AsSpan(0, (int)ms.Length)),
                });

            if (preferredFileNameWithoutExtension is not null)
            {
                preferredFileNameWithoutExtension += ".png";
                if (preferredFileNameWithoutExtension.Length >= 260)
                    preferredFileNameWithoutExtension = preferredFileNameWithoutExtension[..^4] + ".png";

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
                unsafe
                {
                    if (preferredFileNameWithoutExtension.Length >= 260)
                        preferredFileNameWithoutExtension.AsSpan(0, 260).CopyTo(new(fgdw.fgd.e0.cFileName, 260));
                    else
                        preferredFileNameWithoutExtension.AsSpan().CopyTo(new(fgdw.fgd.e0.cFileName, 260));
                }

                AddToDataObject(
                    pdo,
                    ClipboardFormatFromName(CFSTR.CFSTR_FILEDESCRIPTORW),
                    new()
                    {
                        tymed = (uint)TYMED.TYMED_HGLOBAL,
                        hGlobal = CreateHGlobalFromMemory<FILEGROUPDESCRIPTORW>(new(ref fgdw)),
                    });

                unsafe
                {
                    using var ims = default(ComPtr<IStream>);
                    fixed (byte* p = ms.GetBuffer())
                        ims.Attach(SHCreateMemStream(p, (uint)ms.Length));
                    if (ims.IsEmpty())
                        throw new OutOfMemoryException();

                    AddToDataObject(
                        pdo,
                        ClipboardFormatFromName(CFSTR.CFSTR_FILECONTENTS),
                        new()
                        {
                            tymed = (uint)TYMED.TYMED_ISTREAM,
                            pstm = ims.Get(),
                        });
                    ims.Detach();
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
                    hGlobal = CreateHGlobalFromMemory<byte>(ms.GetBuffer().AsSpan(0, (int)ms.Length)),
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
                    hGlobal = CreateHGlobalFromMemory<byte>(ms.GetBuffer().AsSpan(0, (int)ms.Length)),
                });
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        unsafe
        {
            var pdop = pdo.Get();
            this.oleThreadActions.Enqueue(
                () =>
                {
                    if (Marshal.GetExceptionForHR(OleSetClipboard(pdop)) is { } ex)
                        tcs.SetException(ex);
                    else
                        tcs.SetResult();
                });
        }

        this.oleThreadActionAvailable.Set();
        await tcs.Task;

        return;

        [DllImport("ole32.dll")]
        static extern unsafe int OleSetClipboard(void* pdo);

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

        static unsafe uint ClipboardFormatFromName(ReadOnlySpan<char> name)
        {
            uint cf;
            fixed (void* p = name)
                cf = RegisterClipboardFormatW((ushort*)p);
            if (cf != 0)
                return cf;
            throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ??
                  new InvalidOperationException($"RegisterClipboardFormatW({name}) failed.");
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

    private unsafe void OleThreadBody()
    {
        ((HRESULT)OleInitialize(0)).ThrowOnError();

        const uint ctshc = 2u;
        var ctsh = stackalloc HANDLE[(int)ctshc]
        {
            (HANDLE)this.disposeCts.Token.WaitHandle.SafeWaitHandle.DangerousGetHandle(),
            (HANDLE)this.oleThreadActionAvailable.SafeWaitHandle.DangerousGetHandle(),
        };
        while (true)
        {
            _ = MsgWaitForMultipleObjects(ctshc, ctsh, false, INFINITE, QS.QS_ALLINPUT);

            if (this.disposeCts.IsCancellationRequested)
                PostQuitMessage(0);

            HandleActions();

            MSG msg;
            while (PeekMessageW(&msg, default, 0, 0, PM.PM_REMOVE))
            {
                if (msg.message == WM.WM_QUIT)
                {
                    HandleActions();
                    _ = OleFlushClipboard();
                    OleUninitialize();
                    return;
                }

                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
        }

        void HandleActions()
        {
            while (true)
            {
                while (this.oleThreadActions.TryDequeue(out var a))
                    a.InvokeSafely();

                this.oleThreadActionAvailable.Reset();
                if (this.oleThreadActions.IsEmpty)
                    break;
            }
        }

        [DllImport("ole32.dll")]
        static extern int OleInitialize(nint reserved);

        [DllImport("ole32.dll")]
        static extern void OleUninitialize();

        [DllImport("ole32.dll")]
        static extern int OleFlushClipboard();
    }
}
