using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>The images too large for the slices of the glyph atlas.</summary>
internal sealed unsafe partial class GlyphAtlas
{
    /// <summary>Glyph images too large for the slices, each in texture arrays of one slice of its own size.</summary>
    public sealed class LargeImages : IDisposable
    {
        private const long MaxKeptBytes = 64 * 1024 * 1024;
        private const int Margin = 1;

        private static readonly ModuleLog Log = ModuleLog.Create<GlyphAtlas>();

        // Process-wide, so that images cached by fonts that outlive an atlas never match an image of the next one.
        private static int lastGeneration;

        private readonly List<Entry?> entries = [];
        private ComPtr<ID3D11Device> device;

        /// <summary>Gets the number of images.</summary>
        public int Count { get; private set; }

        /// <summary>Gets the bytes the images take on the GPU.</summary>
        public long Bytes { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var e in this.entries)
                e?.Dispose();
            this.entries.Clear();
            this.Count = 0;
            this.Bytes = 0;
            this.device.Reset();
        }

        /// <summary>Takes the device of a context if it has none yet.</summary>
        /// <param name="context">The context.</param>
        internal void SetDeviceFrom(ID3D11DeviceContext* context)
        {
            if (!this.device.IsEmpty())
                return;
            var contextDevice = default(ComPtr<ID3D11Device>);
            context->GetDevice(contextDevice.GetAddressOf());
            contextDevice.Swap(ref this.device);
        }

        /// <summary>Makes the texture arrays of a bitmap.</summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="texelWidth">The width of the bitmap in texels.</param>
        /// <param name="frame">The frame.</param>
        /// <returns>The image, <see cref="CachedImage.Full"/> if there is no device yet, or
        /// <see cref="CachedImage.Empty"/> if the device could not make the arrays.</returns>
        internal CachedImage Add(in GlyphBitmap bitmap, int texelWidth, long frame)
        {
            if (this.device.IsEmpty())
                return CachedImage.Full;

            var color = bitmap.Kind == GlyphKind.Color;
            ID3D11ShaderResourceView* view = null;
            ID3D11ShaderResourceView* maskView = null;
            var hr = this.CreateOneSliceTexture2DArray(color, texelWidth, bitmap.Height, bitmap.Pixels, &view);
            if (hr.SUCCEEDED && bitmap.Mask is { } mask && color)
                hr = this.CreateOneSliceTexture2DArray(false, bitmap.Width, bitmap.Height, mask, &maskView);

            if (hr.FAILED)
            {
                if (view is not null)
                    view->Release();
                if (maskView is not null)
                    maskView->Release();
                Log.Verbose(
                    "Could not make the texture of a {Width}x{Height} glyph (0x{Result:X8}). It is not drawn",
                    texelWidth,
                    bitmap.Height,
                    hr.Value);
                return CachedImage.Empty;
            }

            // The mask is bound at t1 with the colors at t2, as the shader reads them from the atlas.
            var entry = color ? new Entry(maskView, view) : new Entry(view, null);
            entry.LastUsedFrame = frame;

            var area = (long)(texelWidth + (2 * Margin)) * (bitmap.Height + (2 * Margin));
            var maskArea = (long)(bitmap.Width + (2 * Margin)) * (bitmap.Height + (2 * Margin));
            entry.Bytes = (area * (color ? 4 : 1)) + (bitmap.Mask is null ? 0 : maskArea);
            var slot = this.entries.IndexOf(null);
            if (slot < 0)
            {
                slot = this.entries.Count;
                this.entries.Add(entry);
            }
            else
            {
                this.entries[slot] = entry;
            }

            this.Count++;
            this.Bytes += entry.Bytes;
            return new(
                slot,
                bitmap.Kind,
                Margin,
                Margin,
                bitmap.Width,
                bitmap.Height,
                bitmap.OffsetX,
                bitmap.OffsetY,
                bitmap.Mask is null ? -1 : 0,
                Margin,
                Margin,
                entry.Generation,
                entry.Generation,
                true);
        }

        /// <summary>Tests whether an image is still there.</summary>
        /// <param name="slot">The slot of the image.</param>
        /// <param name="generation">Its generation.</param>
        /// <returns>Whether it is.</returns>
        internal bool IsCurrent(int slot, int generation) =>
            slot < this.entries.Count && this.entries[slot] is { } e && e.Generation == generation;

        /// <summary>Marks an image as used in a frame.</summary>
        /// <param name="slot">The slot of the image, which must be current.</param>
        /// <param name="frame">The frame.</param>
        /// <returns>The views to bind at <c>t1</c> and <c>t2</c>, either null if the image has none.</returns>
        internal nint* GetTextures(int slot, long frame)
        {
            var e = this.entries[slot]!;
            e.LastUsedFrame = frame;
            return e.Textures;
        }

        /// <summary>Releases images not drawn in a frame while they take more than they may.</summary>
        /// <param name="frame">The frame that ends. Its draw data has been rendered.</param>
        internal void EndFrame(long frame)
        {
            while (this.Bytes > MaxKeptBytes)
            {
                var lru = -1;
                for (var i = 0; i < this.entries.Count; i++)
                {
                    if (this.entries[i] is { } e && e.LastUsedFrame < frame &&
                        (lru < 0 || e.LastUsedFrame < this.entries[lru]!.LastUsedFrame))
                    {
                        lru = i;
                    }
                }

                if (lru < 0)
                    break;
                var entry = this.entries[lru]!;
                this.entries[lru] = null;
                this.Count--;
                this.Bytes -= entry.Bytes;
                entry.Dispose();
            }
        }

        private HRESULT CreateOneSliceTexture2DArray(
            bool color, int width, int height, byte[] texels, ID3D11ShaderResourceView** view)
        {
            var bpp = color ? 4 : 1;
            var row = width * bpp;
            var paddedWidth = width + (2 * Margin);
            var paddedHeight = height + (2 * Margin);
            var padded = new byte[paddedWidth * paddedHeight * bpp];
            for (var y = 0; y < height; y++)
            {
                texels.AsSpan(y * row, row)
                      .CopyTo(padded.AsSpan((((y + Margin) * paddedWidth) + Margin) * bpp));
            }

            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = (uint)paddedWidth,
                Height = (uint)paddedHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = color ? DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM : DXGI_FORMAT.DXGI_FORMAT_R8_UNORM,
                SampleDesc = new(1, 0),
                Usage = D3D11_USAGE.D3D11_USAGE_IMMUTABLE,
                BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
            };
            using var texture = default(ComPtr<ID3D11Texture2D>);
            fixed (byte* p = padded)
            {
                var data = new D3D11_SUBRESOURCE_DATA { pSysMem = p, SysMemPitch = (uint)(paddedWidth * bpp) };
                var hr = this.device.Get()->CreateTexture2D(&desc, &data, texture.GetAddressOf());
                if (hr.FAILED)
                    return hr;
            }

            var viewDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
            {
                Format = desc.Format,
                ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2DARRAY,
                Texture2DArray = new() { MostDetailedMip = 0, MipLevels = 1, FirstArraySlice = 0, ArraySize = 1 },
            };
            return this.device.Get()->CreateShaderResourceView((ID3D11Resource*)texture.Get(), &viewDesc, view);
        }

        private sealed class Entry : IDisposable
        {
            // Takes the references of both views.
            public Entry(ID3D11ShaderResourceView* mask, ID3D11ShaderResourceView* color)
            {
                this.Textures = (nint*)NativeMemory.Alloc(2, (nuint)sizeof(nint));
                this.Textures[0] = (nint)(mask is null ? color : mask);
                this.Textures[1] = (nint)color;
            }

            ~Entry() => this.ReleaseUnmanagedResources();

            public int Generation { get; } = Interlocked.Increment(ref lastGeneration);

            public long LastUsedFrame { get; set; }

            public long Bytes { get; set; }

            // The views to bind at t1 and t2, in native memory, so that draw lists can point to them.
            public nint* Textures { get; private set; }

            public void Dispose()
            {
                this.ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            private void ReleaseUnmanagedResources()
            {
                if (this.Textures is null)
                    return;
                if (this.Textures[0] != 0)
                    ((ID3D11ShaderResourceView*)this.Textures[0])->Release();

                // A color glyph without a mask binds its one view at both slots.
                if (this.Textures[1] != 0 && this.Textures[1] != this.Textures[0])
                    ((ID3D11ShaderResourceView*)this.Textures[1])->Release();

                NativeMemory.Free(this.Textures);
                this.Textures = null;
            }
        }
    }
}
