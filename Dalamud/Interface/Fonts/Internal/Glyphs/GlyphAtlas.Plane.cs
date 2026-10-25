using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <content>Packed 2D texture array planes of the glyph atlas.</content>
internal sealed unsafe partial class GlyphAtlas
{
    /// <summary>Texel format of an atlas plane.</summary>
    public enum PlaneKind
    {
        /// <summary>Coverage, one byte per texel.</summary>
        Coverage,

        /// <summary>Colors, RGBA, four bytes per texel.</summary>
        Color,
    }

    /// <summary>Lifecycle state of a slice slot in a <see cref="Plane"/>.</summary>
    public enum SlotState
    {
        /// <summary>Unused and cleared on allocation.</summary>
        Free,

        /// <summary>Holding glyphs in use.</summary>
        Current,

        /// <summary>Evicted, but retained for a few frames so recently used glyphs can be GPU-copied back without
        /// re-rasterizing.</summary>
        Retired,
    }

    private enum OpKind
    {
        Clear,
        Upload,
        Copy,
    }

    // A change queued for the GPU. Uploaded texels are at DataOffset in the plane's data.
    private readonly record struct Op(
        OpKind Kind,
        int Slot,
        int X,
        int Y,
        int Width,
        int Height,
        int DataOffset = 0,
        int SourceSlot = 0,
        int SourceX = 0,
        int SourceY = 0);

    /// <summary>A texture array of glyphs of one format, and the slots in it.</summary>
    /// <remarks>Changes are queued, and made on the GPU in order.</remarks>
    /// <seealso cref="Flush"/>
    public sealed class Plane : IDisposable
    {
        // Frames a retired slice retains its pixels before becoming Free.
        private const int RetiredFrames = 4;

        // Maximum staging buffer bytes retained after Flush.
        private const int MaxKeptData = 4 << 20;

        private const int HardLimitFactor = 8;

        // Upper bounds on texel width and height for size-class lanes, with the final class holding up to MaxPackedSize.
        // Separating size classes limits vertical waste in shelf packing.
        private static readonly int[] ClassSizes = [128, 256];

        // Cooldown before retrying texture array growth after an allocation failure.
        private static readonly long GrowRetryTicks = Stopwatch.Frequency;

        // Process-wide, so that images cached by fonts that outlive an atlas never match a slot of the next one.
        private static int lastGeneration;

        private readonly List<SlotInfo> slots = [];

        // Keep restored hot glyphs in separate lanes from newly uploaded glyphs so slices of one-off glyphs age out together.
        private readonly List<Lane>[] newLanes = NewLaneLists();
        private readonly List<Lane>[] restoredLanes = NewLaneLists();
        private readonly List<Op> ops = [];
        private readonly GlyphAtlas atlas;
        private byte[] data = new byte[64 * 1024];
        private byte[] strip = [];
        private int dataLength;

        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11ShaderResourceView> view;
        private long growFailedAt;

        /// <summary>Initializes a new instance of the <see cref="Plane"/> class.</summary>
        /// <param name="atlas">The atlas, whose device it uses.</param>
        /// <param name="kind">Texel format of the plane.</param>
        /// <param name="maxSlices">Initial value of <see cref="MaxSlices"/>.</param>
        public Plane(GlyphAtlas atlas, PlaneKind kind, int maxSlices)
        {
            this.atlas = atlas;
            this.Kind = kind;
            this.MaxSlices = maxSlices;
        }

        /// <summary>Gets the texel format of this plane.</summary>
        public PlaneKind Kind { get; }

        /// <summary>Gets or sets the target maximum number of active slices.</summary>
        /// <remarks>Slices used in the current or previous frame may exceed this target up to
        /// <see cref="HardMaxSlices"/>. Unused slices above it are retired at frame end.</remarks>
        public int MaxSlices { get; set; }

        /// <summary>Gets the number of slices in use.</summary>
        public int SliceCount => this.Count(SlotState.Current);

        /// <summary>Gets the number of retired slices, still holding their pixels.</summary>
        public int RetiredCount => this.Count(SlotState.Retired);

        /// <summary>Gets the number of slots in the texture array, in any state.</summary>
        public int SlotCount => this.slots.Count;

        /// <summary>Gets the number of slices the texture array has, as it grows in steps.</summary>
        public int Capacity { get; private set; }

        /// <summary>Gets a value indicating whether <see cref="Flush"/> has changes to make.</summary>
        public bool HasChanges => this.ops.Count != 0 || this.Capacity < this.slots.Count;

        /// <summary>Gets the number of glyph images in the slices in use, not counting masks.</summary>
        public int GlyphCount { get; private set; }

        /// <summary>Gets the number of times a slice was retired.</summary>
        public int EvictionCount { get; private set; }

        // Hard cap on active slices. Glyphs that do not fit are skipped.
        private int HardMaxSlices => Math.Min(this.MaxSlices * HardLimitFactor, SliceLimit);

        private int MaxRetired => Math.Max(2, this.MaxSlices / 4);

        // Allow allocating new slots unless at SliceLimit or cooling down after a failed texture resize.
        private bool CanGrow =>
            this.slots.Count < SliceLimit &&
            (this.growFailedAt == 0 || Stopwatch.GetTimestamp() - this.growFailedAt >= GrowRetryTicks);

        private int BytesPerTexel => this.Kind == PlaneKind.Color ? 4 : 1;

        // Subpixel glyphs sample one texel left and right, which with linear filtering reaches the second texel beyond
        // the glyph. Two clear texels between coverage glyphs keep such a sample off the next glyph.
        private int Gap => this.Kind == PlaneKind.Coverage ? 2 : 1;

        private DXGI_FORMAT Format =>
            this.Kind == PlaneKind.Color ? DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM : DXGI_FORMAT.DXGI_FORMAT_R8_UNORM;

        /// <summary>Gets the state of <paramref name="slot"/>.</summary>
        /// <param name="slot">The slot index.</param>
        /// <returns>The slot's state.</returns>
        public SlotState StateOf(int slot) => this.slots[slot].State;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.slots.Clear();
            this.RemoveLanes(0, int.MaxValue);
            this.ops.Clear();
            this.dataLength = 0;
            this.GlyphCount = 0;
            this.view.Reset();
            this.texture.Reset();
            this.Capacity = 0;
        }

        /// <summary>Flushes queued clears, uploads, and copies to the GPU and returns the SRV to bind.</summary>
        /// <param name="context">The immediate context of the device of the atlas.</param>
        /// <param name="bytes">Receives the bytes uploaded.</param>
        /// <returns>The texture array view, or <see langword="null"/> if empty.</returns>
        /// <remarks><ul>
        /// <li>The view is valid until the next call.</li>
        /// <li>If growing the texture array fails, slices beyond the old capacity are dropped and new allocations wait
        /// for the retry cooldown.</li>
        /// <li>Elapsed time is recorded via <see cref="GlyphRasterBudget.CountUpload"/>.</li>
        /// </ul></remarks>
        public ID3D11ShaderResourceView* Flush(ID3D11DeviceContext* context, out long bytes)
        {
            bytes = 0;
            if (this.slots.Count == 0)
                return null;
            if (!this.HasChanges)
                return this.view.Get();

            var start = Stopwatch.GetTimestamp();
            if (this.Capacity < this.slots.Count && !this.TryGrow(context))
                this.DropSlotsBeyondCapacity();

            var resource = (ID3D11Resource*)this.texture.Get();
            for (var i = 0; i < this.ops.Count; i++)
            {
                var op = this.ops[i];
                switch (op.Kind)
                {
                    case OpKind.Clear:
                        this.Clear(context, op.Slot);
                        break;

                    case OpKind.Upload:
                        i = this.UploadRun(context, resource, i, ref bytes);
                        break;

                    case OpKind.Copy:
                    {
                        var box = new D3D11_BOX(
                            op.SourceX, op.SourceY, 0, op.SourceX + op.Width, op.SourceY + op.Height, 1);
                        context->CopySubresourceRegion(
                            resource, (uint)op.Slot, (uint)op.X, (uint)op.Y, 0, resource, (uint)op.SourceSlot, &box);
                        break;
                    }
                }
            }

            this.ops.Clear();
            this.dataLength = 0;

            // Shrink staging buffers after a transient upload burst.
            if (this.data.Length > MaxKeptData)
                this.data = new byte[64 * 1024];
            if (this.strip.Length > MaxKeptData)
                this.strip = [];
            GlyphRasterBudget.CountUpload(Stopwatch.GetTimestamp() - start);
            return this.view.Get();
        }

        /// <summary>Tests whether <paramref name="slot"/> is active with <paramref name="generation"/>.</summary>
        /// <param name="slot">The slot index.</param>
        /// <param name="generation">Expected generation.</param>
        /// <returns><see langword="true"/> if active with matching generation.</returns>
        internal bool IsCurrent(int slot, int generation) =>
            this.IsAvailable(slot, generation) && this.slots[slot].State == SlotState.Current;

        /// <summary>Tests whether <paramref name="slot"/> still holds <paramref name="generation"/>, active or retired.
        /// </summary>
        /// <param name="slot">The slot index.</param>
        /// <param name="generation">Expected generation.</param>
        /// <returns><see langword="true"/> if active or retired with matching generation.</returns>
        internal bool IsAvailable(int slot, int generation) =>
            slot < this.slots.Count && this.slots[slot] is { State: not SlotState.Free } s &&
            s.Generation == generation;

        /// <summary>Gets the generation of a slot.</summary>
        /// <param name="slot">The slot.</param>
        /// <returns>The generation.</returns>
        internal int GenerationOf(int slot) => this.slots[slot].Generation;

        /// <summary>Marks a slot as used in a frame.</summary>
        /// <param name="slot">The slot.</param>
        /// <param name="frame">The frame.</param>
        internal void MarkUsed(int slot, long frame) => this.slots[slot].LastUsedFrame = frame;

        /// <summary>Counts a glyph added to a slot.</summary>
        /// <param name="slot">The slot.</param>
        internal void AddGlyph(int slot)
        {
            this.slots[slot].GlyphCount++;
            this.GlyphCount++;
        }

        /// <summary>Allocates a rectangle, and queues an upload of texels to it.</summary>
        /// <param name="width">The width in texels.</param>
        /// <param name="height">The height.</param>
        /// <param name="texels">The texels, row by row.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="slot">Receives the slot.</param>
        /// <param name="x">Receives the left.</param>
        /// <param name="y">Receives the top.</param>
        /// <returns>Whether there was room.</returns>
        internal bool TryPut(
            int width, int height, ReadOnlySpan<byte> texels, long frame, out int slot, out int x, out int y)
        {
            if (!this.TryAllocate(this.newLanes, width, height, frame, out slot, out x, out y))
                return false;

            var length = width * height * this.BytesPerTexel;
            if (this.dataLength + length > this.data.Length)
                Array.Resize(ref this.data, Math.Max(this.dataLength + length, this.data.Length * 2));
            texels[..length].CopyTo(this.data.AsSpan(this.dataLength));
            this.ops.Add(new(OpKind.Upload, slot, x, y, width, height, DataOffset: this.dataLength));
            this.dataLength += length;
            return true;
        }

        /// <summary>Allocates a rectangle, and queues a copy of texels from another slot to it.</summary>
        /// <param name="sourceSlot">The slot to copy from.</param>
        /// <param name="sourceGeneration">The generation of the texels in it.</param>
        /// <param name="sourceX">The left of the texels.</param>
        /// <param name="sourceY">The top of the texels.</param>
        /// <param name="width">The width in texels.</param>
        /// <param name="height">The height.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="slot">Receives the slot.</param>
        /// <param name="x">Receives the left.</param>
        /// <param name="y">Receives the top.</param>
        /// <returns>Whether there was room, and the texels are still there.</returns>
        internal bool TryCopy(
            int sourceSlot, int sourceGeneration, int sourceX, int sourceY, int width, int height, long frame,
            out int slot, out int x, out int y)
        {
            if (!this.TryAllocate(this.restoredLanes, width, height, frame, out slot, out x, out y))
                return false;

            // TryAllocate may reuse sourceSlot if it was retired, changing its generation and making available false.
            // Also guard against self-copy within one subresource, which is undefined in D3D11.
            var available = this.IsAvailable(sourceSlot, sourceGeneration);
            Debug.Assert(!available || slot != sourceSlot, "A glyph was to be copied within its slot.");
            if (!available || slot == sourceSlot)
            {
                this.FreeLast(this.restoredLanes, slot, x, y, width);
                return false;
            }

            this.ops.Add(
                new(
                    OpKind.Copy,
                    slot,
                    x,
                    y,
                    width,
                    height,
                    SourceSlot: sourceSlot,
                    SourceX: sourceX,
                    SourceY: sourceY));
            return true;
        }

        /// <summary>Rolls back the most recent <see cref="TryPut"/> or <see cref="TryCopy"/> when a multi-plane upload
        /// fails partway.</summary>
        /// <param name="slot">The slot it gave.</param>
        /// <param name="x">The left it gave.</param>
        /// <param name="y">The top it gave.</param>
        internal void Unput(int slot, int x, int y)
        {
            var last = this.ops.Count - 1;
            if (last < 0 || this.ops[last] is not { Kind: OpKind.Upload or OpKind.Copy } op ||
                op.Slot != slot || op.X != x || op.Y != y)
            {
                Debug.Fail("Only the last rectangle put can be taken back.");
                return;
            }

            this.ops.RemoveAt(last);
            if (op.Kind == OpKind.Upload)
                this.dataLength = op.DataOffset;
            this.FreeLast(op.Kind == OpKind.Upload ? this.newLanes : this.restoredLanes, slot, x, y, op.Width);
        }

        /// <summary>Retires unused slices beyond <see cref="MaxSlices"/>, frees old retired ones, and flushes queued
        /// changes.</summary>
        /// <param name="frame">The frame that ends.</param>
        internal void EndFrame(long frame)
        {
            for (var current = this.SliceCount; current > this.MaxSlices; current--)
            {
                var unused = this.Oldest(SlotState.Current, frame);
                if (unused < 0)
                    break;
                this.Retire(unused, frame);
            }

            foreach (var s in this.slots)
            {
                if (s.State == SlotState.Retired && frame - s.RetiredFrame >= RetiredFrames)
                    s.State = SlotState.Free;
            }

            for (var retired = this.RetiredCount; retired > this.MaxRetired; retired--)
                this.slots[this.Oldest(SlotState.Retired, long.MaxValue)].State = SlotState.Free;

            if (this.atlas.Device is not null && this.ops.Count > 0)
            {
                using var context = default(ComPtr<ID3D11DeviceContext>);
                this.atlas.Device->GetImmediateContext(context.GetAddressOf());
                _ = this.Flush(context, out _);
            }
        }

        private static List<Lane>[] NewLaneLists()
        {
            var lists = new List<Lane>[ClassSizes.Length + 1];
            for (var i = 0; i < lists.Length; i++)
                lists[i] = [];
            return lists;
        }

        private void FreeLast(List<Lane>[] classes, int slot, int x, int y, int width)
        {
            foreach (var lanes in classes)
            {
                foreach (var lane in lanes)
                {
                    if (lane.Slot == slot)
                    {
                        lane.FreeLast(x, y, width + this.Gap);
                        return;
                    }
                }
            }
        }

        // Shelf packing in the open lanes of the size class, taking a slot for a new lane if none has room. A packed
        // image is at most MaxPackedSize, so it always fits in a new lane.
        private bool TryAllocate(
            List<Lane>[] classes, int width, int height, long frame, out int slot, out int x, out int y)
        {
            var size = Math.Max(width, height);
            var sizeClass = 0;
            while (sizeClass < ClassSizes.Length && size > ClassSizes[sizeClass])
                sizeClass++;
            var lanes = classes[sizeClass];

            // Each allocation includes Gap clear texels, for sampling at the edges. A taken slot is cleared.
            foreach (var lane in lanes)
            {
                if (!lane.Full && lane.TryAllocate(width + this.Gap, height + this.Gap, out x, out y))
                {
                    slot = lane.Slot;
                    this.MarkUsed(slot, frame);
                    return true;
                }
            }

            slot = this.Take(frame);
            if (slot < 0)
            {
                slot = x = y = 0;
                return false;
            }

            var newLane = new Lane(slot);
            lanes.Add(newLane);
            newLane.TryAllocate(width + this.Gap, height + this.Gap, out x, out y);
            return true;
        }

        // Allocates and queues a clear for a slot, retiring a slice unused in the last two frames if at MaxSlices, or returns -1 if full.
        private int Take(long frame)
        {
            if (this.SliceCount >= this.MaxSlices)
            {
                // Keep slices used in the previous frame because re-copying all their glyphs costs more than exceeding MaxSlices briefly.
                var lru = this.Oldest(SlotState.Current, frame - 1);
                if (lru >= 0)
                {
                    this.Retire(lru, frame);
                }
                else if (this.SliceCount >= this.HardMaxSlices)
                {
                    Log.Verbose(
                        "{Count} slices of {Kind} glyphs are drawn from in two frames. Glyphs beyond are not drawn",
                        this.SliceCount,
                        this.Kind);
                    return -1;
                }
            }

            var canGrow = this.CanGrow;
            var take = this.slots.FindIndex(x => x.State == SlotState.Free);
            if (take < 0 && (this.RetiredCount > this.MaxRetired || !canGrow))
                take = this.Oldest(SlotState.Retired, long.MaxValue);
            if (take < 0)
            {
                if (!canGrow)
                    return -1;
                this.slots.Add(new());
                take = this.slots.Count - 1;
            }

            var slot = this.slots[take];
            slot.State = SlotState.Current;
            slot.Generation = Interlocked.Increment(ref lastGeneration);
            slot.LastUsedFrame = frame;
            slot.GlyphCount = 0;
            this.ops.Add(new(OpKind.Clear, take, 0, 0, SliceSize, SliceSize));
            return take;
        }

        // Finds the oldest slot in state with timestamp strictly before 'before', breaking ties by lowest Generation, or -1.
        private int Oldest(SlotState state, long before)
        {
            var oldest = -1;
            var oldestFrame = 0L;
            for (var i = 0; i < this.slots.Count; i++)
            {
                var s = this.slots[i];
                var frame = state == SlotState.Retired ? s.RetiredFrame : s.LastUsedFrame;
                if (s.State != state || frame >= before)
                    continue;
                if (oldest < 0 || frame < oldestFrame ||
                    (frame == oldestFrame && s.Generation < this.slots[oldest].Generation))
                {
                    (oldest, oldestFrame) = (i, frame);
                }
            }

            return oldest;
        }

        private void RemoveLanes(int from, int to)
        {
            foreach (var lanes in this.newLanes.Concat(this.restoredLanes))
                lanes.RemoveAll(lane => lane.Slot >= from && lane.Slot < to);
        }

        private void Retire(int index, long frame)
        {
            var slot = this.slots[index];
            slot.State = SlotState.Retired;
            slot.RetiredFrame = frame;
            this.GlyphCount -= slot.GlyphCount;
            slot.GlyphCount = 0;
            this.RemoveLanes(index, index + 1);
            this.EvictionCount++;
        }

        private int Count(SlotState state)
        {
            var n = 0;
            foreach (var s in this.slots)
                n += s.State == state ? 1 : 0;
            return n;
        }

        // Coalesces horizontally adjacent shelf uploads into one UpdateSubresource strip with zeroed gaps, returning the last coalesced op index.
        // Safe to span the tallest glyph's height because the shelf reserves at least that height and no other glyph overlaps it.
        private int UploadRun(ID3D11DeviceContext* context, ID3D11Resource* resource, int first, ref long bytes)
        {
            var op = this.ops[first];
            var last = first;
            var height = op.Height;
            while (last + 1 < this.ops.Count &&
                   this.ops[last + 1] is { Kind: OpKind.Upload } next &&
                   next.Slot == op.Slot &&
                   next.Y == op.Y &&
                   next.X == this.ops[last].X + this.ops[last].Width + this.Gap)
            {
                last++;
                height = Math.Max(height, next.Height);
            }

            var bpp = this.BytesPerTexel;
            var right = this.ops[last].X + this.ops[last].Width;
            var pitch = (right - op.X) * bpp;
            var box = new D3D11_BOX(op.X, op.Y, 0, right, op.Y + height, 1);
            if (last == first)
            {
                fixed (byte* p = &this.data[op.DataOffset])
                    context->UpdateSubresource(resource, (uint)op.Slot, &box, p, (uint)pitch, 0);
                bytes += pitch * height;
                return first;
            }

            var length = pitch * height;
            if (this.strip.Length < length)
                this.strip = new byte[Math.Max(length, this.strip.Length * 2)];
            var target = this.strip.AsSpan(0, length);
            target.Clear();
            for (var i = first; i <= last; i++)
            {
                var g = this.ops[i];
                var row = g.Width * bpp;
                var offset = (g.X - op.X) * bpp;
                for (var y = 0; y < g.Height; y++)
                    this.data.AsSpan(g.DataOffset + (y * row), row).CopyTo(target[((y * pitch) + offset)..]);
            }

            fixed (byte* p = target)
                context->UpdateSubresource(resource, (uint)op.Slot, &box, p, (uint)pitch, 0);
            bytes += length;
            return last;
        }

        private void Clear(ID3D11DeviceContext* context, int slot)
        {
            var desc = new D3D11_RENDER_TARGET_VIEW_DESC
            {
                Format = this.Format,
                ViewDimension = D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2DARRAY,
                Texture2DArray = new() { MipSlice = 0, FirstArraySlice = (uint)slot, ArraySize = 1 },
            };
            using var targetView = default(ComPtr<ID3D11RenderTargetView>);

            // Only fails when out of memory. The gaps between glyphs then keep old texels, which their edges may show.
            if (this.atlas.Device->CreateRenderTargetView(
                    (ID3D11Resource*)this.texture.Get(), &desc, targetView.GetAddressOf()).FAILED)
            {
                return;
            }

            var zero = default(Vector4);
            context->ClearRenderTargetView(targetView, (float*)&zero);
        }

        // Reallocates the GPU texture array with growth headroom (falling back to exact slot count) and copies live slices.
        private bool TryGrow(ID3D11DeviceContext* context)
        {
            var needed = this.slots.Count;
            var target = this.GrowTarget();
            var hr = this.TryReplaceTexture(context, target);
            if (hr.FAILED && target > needed)
                hr = this.TryReplaceTexture(context, needed);
            if (hr.FAILED)
            {
                this.growFailedAt = Math.Max(Stopwatch.GetTimestamp(), 1);
                Log.Verbose(
                    "Could not grow the {Kind} glyph texture to {Count} slices (0x{Result:X8}). Glyphs that do not fit are not drawn",
                    this.Kind,
                    needed,
                    hr.Value);
                return false;
            }

            if (this.growFailedAt != 0)
            {
                this.growFailedAt = 0;
                Log.Information("The {Kind} glyph texture has grown to {Count} slices", this.Kind, this.Capacity);
            }

            return true;
        }

        // Coverage grows by 50% (at least 2 slices) to amortize copies, while Color grows by 1 slice (64 MiB each).
        // Capped at the steady-state limit unless active slots already exceed it.
        private int GrowTarget()
        {
            var keptTo = this.MaxSlices + this.MaxRetired;
            var limit = this.slots.Count <= keptTo ? keptTo : this.HardMaxSlices + this.MaxRetired;
            var step = this.Kind == PlaneKind.Color ? 1 : Math.Max(2, this.Capacity / 2);
            var grown = this.Capacity + step;
            return Math.Min(Math.Max(this.slots.Count, Math.Min(grown, limit)), SliceLimit);
        }

        // Whether a new texture array needs the texels of a slot. A free one is cleared when taken, but a queued copy
        // may still read it.
        private bool HasTexelsToKeep(int slot)
        {
            if (this.slots[slot].State != SlotState.Free)
                return true;
            foreach (var op in this.ops)
            {
                if (op.Kind == OpKind.Copy && op.SourceSlot == slot)
                    return true;
            }

            return false;
        }

        private HRESULT TryReplaceTexture(ID3D11DeviceContext* context, int newCapacity)
        {
            // A render target, so that taking a slot clears it on the GPU instead of uploading zeros.
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = SliceSize,
                Height = SliceSize,
                MipLevels = 1,
                ArraySize = (uint)newCapacity,
                Format = this.Format,
                SampleDesc = new(1, 0),
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags =
                    (uint)(D3D11_BIND_FLAG.D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_FLAG.D3D11_BIND_RENDER_TARGET),
            };
            using var newTexture = default(ComPtr<ID3D11Texture2D>);
            var hr = this.atlas.Device->CreateTexture2D(&desc, null, newTexture.GetAddressOf());
            if (hr.FAILED)
                return hr;

            var viewDesc = new D3D11_SHADER_RESOURCE_VIEW_DESC
            {
                Format = desc.Format,
                ViewDimension = D3D_SRV_DIMENSION.D3D11_SRV_DIMENSION_TEXTURE2DARRAY,
                Texture2DArray = new()
                    { MostDetailedMip = 0, MipLevels = 1, FirstArraySlice = 0, ArraySize = desc.ArraySize },
            };
            using var newView = default(ComPtr<ID3D11ShaderResourceView>);
            hr = this.atlas.Device->CreateShaderResourceView(
                (ID3D11Resource*)newTexture.Get(), &viewDesc, newView.GetAddressOf());
            if (hr.FAILED)
                return hr;

            for (var i = 0; i < this.Capacity; i++)
            {
                if (!this.HasTexelsToKeep(i))
                    continue;
                context->CopySubresourceRegion(
                    (ID3D11Resource*)newTexture.Get(),
                    (uint)i,
                    0,
                    0,
                    0,
                    (ID3D11Resource*)this.texture.Get(),
                    (uint)i,
                    null);
            }

            newTexture.Swap(ref this.texture);
            newView.Swap(ref this.view);
            this.Capacity = newCapacity;
            return hr;
        }

        // Forgets the slots beyond the texture, with their glyphs and the changes queued for them. Quads of this frame
        // that are already laid out on them draw texels of the last slice for one frame, as D3D11 clamps the index.
        private void DropSlotsBeyondCapacity()
        {
            var keep = this.Capacity;
            for (var i = keep; i < this.slots.Count; i++)
                this.GlyphCount -= this.slots[i].GlyphCount;
            this.slots.RemoveRange(keep, this.slots.Count - keep);
            this.RemoveLanes(keep, int.MaxValue);
            this.ops.RemoveAll(x => x.Slot >= keep || (x.Kind == OpKind.Copy && x.SourceSlot >= keep));
        }
    }

    private sealed class SlotInfo
    {
        public SlotState State { get; set; }

        // Unique to each time the slot was taken, and larger for later ones.
        public int Generation { get; set; }

        // The frame its glyphs were last drawn or added in.
        public long LastUsedFrame { get; set; }

        public long RetiredFrame { get; set; }

        // Glyphs whose image is here, not counting masks.
        public int GlyphCount { get; set; }
    }

    // The packing of a slot, in shelves.
    private sealed class Lane(int slot)
    {
        private int cursorX;
        private int cursorY;
        private int shelfHeight;

        public int Slot { get; } = slot;

        public bool Full { get; private set; }

        public bool TryAllocate(int w, int h, out int x, out int y)
        {
            if (this.cursorX + w > SliceSize)
            {
                this.cursorX = 0;
                this.cursorY += this.shelfHeight;
                this.shelfHeight = 0;
            }

            if (this.cursorY + h > SliceSize)
            {
                // Glyphs come in similar sizes, so a lane that cannot take one is as good as full.
                this.Full = true;
                x = y = 0;
                return false;
            }

            x = this.cursorX;
            y = this.cursorY;
            this.cursorX += w;
            this.shelfHeight = Math.Max(this.shelfHeight, h);
            return true;
        }

        public void FreeLast(int x, int y, int w)
        {
            if (this.cursorY == y && this.cursorX == x + w)
                this.cursorX = x;
        }
    }
}
