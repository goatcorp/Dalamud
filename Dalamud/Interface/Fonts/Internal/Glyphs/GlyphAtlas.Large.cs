using System.Collections.Generic;
using System.Threading;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <content>Oversized glyph textures outside the packed atlas slices.</content>
internal sealed unsafe partial class GlyphAtlas
{
    /// <summary>Standalone single-slice 2D texture arrays for glyph images exceeding <see cref="MaxPackedSize"/>,
    /// matching the text shader's bindings.</summary>
    public sealed class LargeImages : IDisposable
    {
        private const long MaxKeptBytes = 64 * 1024 * 1024;
        private const int Margin = 1;

        // Process-wide, so that images cached by fonts that outlive an atlas never match an image of the next one.
        private static int lastGeneration;

        private readonly List<Entry?> entries = [];
        private readonly GlyphAtlas atlas;

        /// <summary>Initializes a new instance of the <see cref="LargeImages"/> class.</summary>
        /// <param name="atlas">The atlas, whose device it uses.</param>
        public LargeImages(GlyphAtlas atlas) => this.atlas = atlas;

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
        }

        /// <summary>Makes a handle of an image, for <see cref="GetTextures"/>.</summary>
        /// <param name="slot">The slot of the image.</param>
        /// <param name="generation">Its generation.</param>
        /// <returns>The handle, never 0.</returns>
        internal static nint HandleOf(int slot, int generation) => (nint)(((long)generation << 32) | (uint)slot);

        /// <summary>Gets the coverage and color views of an image.</summary>
        /// <param name="handle">The handle from <see cref="HandleOf"/>.</param>
        /// <returns>The views, or <see langword="default"/> if evicted, since cached draw data from earlier frames can
        /// outlive an image.</returns>
        internal (nint Coverage, nint Color) GetTextures(nint handle)
        {
            var slot = (int)(uint)(long)handle;
            return this.IsCurrent(slot, (int)((long)handle >> 32))
                       ? (this.entries[slot]!.Coverage, this.entries[slot]!.Color)
                       : default;
        }

        /// <summary>Makes the texture arrays of a bitmap.</summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="texelWidth">The width of the bitmap in texels.</param>
        /// <param name="frame">The frame.</param>
        /// <returns>The image, <see cref="CachedImage.Full"/> if there is no device yet, or
        /// <see cref="CachedImage.Empty"/> if the device could not make the arrays.</returns>
        internal CachedImage Add(in GlyphBitmap bitmap, int texelWidth, long frame)
        {
            if (this.atlas.Device is null)
                return CachedImage.Full;

            var color = bitmap.Kind == GlyphKind.Color;
            using var view = default(ComPtr<ID3D11ShaderResourceView>);
            using var maskView = default(ComPtr<ID3D11ShaderResourceView>);
            var hr = this.CreateOneSliceTexture2DArray(
                color ? PlaneKind.Color : PlaneKind.Coverage,
                texelWidth,
                bitmap.Height,
                bitmap.Pixels,
                view.GetAddressOf());
            if (hr.SUCCEEDED && bitmap.Mask is { } mask && color)
            {
                hr = this.CreateOneSliceTexture2DArray(
                    PlaneKind.Coverage,
                    bitmap.Width,
                    bitmap.Height,
                    mask,
                    maskView.GetAddressOf());
            }

            if (hr.FAILED)
            {
                Log.Verbose(
                    "Could not make the texture of a {Width}x{Height} glyph (0x{Result:X8}). It is not drawn",
                    texelWidth,
                    bitmap.Height,
                    hr.Value);
                return CachedImage.Empty;
            }

            // A color glyph without a mask leaves the coverage view null, as its quads never read it.
            var hasMask = maskView.Get() is not null;
            var entry = color ? new Entry(maskView.Detach(), view.Detach()) : new Entry(view.Detach(), null);
            entry.LastUsedFrame = frame;

            // Color glyphs are 1 texel per pixel, so the mask has the same padded dimensions.
            var area = (long)(texelWidth + (2 * Margin)) * (bitmap.Height + (2 * Margin));
            entry.Bytes = (area * (color ? 4 : 1)) + (hasMask ? area : 0);
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
                hasMask ? 0 : -1,
                Margin,
                Margin,
                entry.Generation,
                entry.Generation,
                true);
        }

        /// <summary>Tests whether <paramref name="slot"/> still holds <paramref name="generation"/>.</summary>
        /// <param name="slot">The slot index.</param>
        /// <param name="generation">Expected generation.</param>
        /// <returns><see langword="true"/> if still resident.</returns>
        internal bool IsCurrent(int slot, int generation) =>
            (uint)slot < (uint)this.entries.Count && this.entries[slot] is { } e && e.Generation == generation;

        /// <summary>Marks an image as used in a frame.</summary>
        /// <param name="slot">The slot of the image, which must be current.</param>
        /// <param name="frame">The frame.</param>
        internal void MarkUsed(int slot, long frame) => this.entries[slot]!.LastUsedFrame = frame;

        /// <summary>Evicts least-recently-used images from earlier frames until GPU memory is within
        /// <see cref="MaxKeptBytes"/>.</summary>
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
            PlaneKind kind, int width, int height, byte[] texels, ID3D11ShaderResourceView** view)
        {
            var bpp = kind == PlaneKind.Color ? 4 : 1;
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
                Format = kind == PlaneKind.Color ? DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM : DXGI_FORMAT.DXGI_FORMAT_R8_UNORM,
                SampleDesc = new(1, 0),
                Usage = D3D11_USAGE.D3D11_USAGE_IMMUTABLE,
                BindFlags = (uint)D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE,
            };
            using var texture = default(ComPtr<ID3D11Texture2D>);
            fixed (byte* p = padded)
            {
                var data = new D3D11_SUBRESOURCE_DATA { pSysMem = p, SysMemPitch = (uint)(paddedWidth * bpp) };
                var hr = this.atlas.Device->CreateTexture2D(&desc, &data, texture.GetAddressOf());
                if (hr.FAILED)
                    return hr;
            }

            var viewDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
            {
                Format = desc.Format,
                ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2DARRAY,
                Texture2DArray = new() { MostDetailedMip = 0, MipLevels = 1, FirstArraySlice = 0, ArraySize = 1 },
            };
            return this.atlas.Device->CreateShaderResourceView((ID3D11Resource*)texture.Get(), &viewDesc, view);
        }

        private sealed class Entry : IDisposable
        {
            private ComPtr<ID3D11ShaderResourceView> coverage;
            private ComPtr<ID3D11ShaderResourceView> color;

            public Entry(ID3D11ShaderResourceView* coverage, ID3D11ShaderResourceView* color)
            {
                this.coverage.Attach(coverage);
                this.color.Attach(color);
            }

            public int Generation { get; } = Interlocked.Increment(ref lastGeneration);

            public long LastUsedFrame { get; set; }

            public long Bytes { get; set; }

            public nint Coverage => (nint)this.coverage.Get();

            public nint Color => (nint)this.color.Get();

            public void Dispose()
            {
                this.coverage.Dispose();
                this.color.Dispose();
            }
        }
    }
}
