using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

using Dalamud.Logging.Internal;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.Glyphs;

/// <summary>The texture arrays of the glyph atlas.</summary>
internal sealed unsafe partial class GlyphAtlas
{
    /// <summary>What a plane holds.</summary>
    public enum PlaneKind
    {
        /// <summary>Coverage, one byte per texel.</summary>
        Coverage,

        /// <summary>Colors, RGBA, four bytes per texel.</summary>
        Color,
    }

    /// <summary>What a slot of a plane is doing.</summary>
    public enum SlotState
    {
        /// <summary>Not holding anything. Cleared when taken.</summary>
        Free,

        /// <summary>Holding glyphs in use.</summary>
        Current,

        /// <summary>Holding glyphs no longer valid, for a few frames, so that they can be copied back.</summary>
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
        // The frames a retired slot keeps its pixels for.
        private const int RetiredFrames = 4;

        // The most bytes of queued texels kept allocated after a flush.
        private const int MaxKeptData = 4 << 20;

        // HardMaxSlices as a multiple of MaxSlices.
        private const int HardLimitFactor = 8;

        // The largest width in texels and height of images in the lanes of each size class but the last, which takes
        // up to MaxPackedSize. Shelves are as tall as their tallest image, so mixing sizes in a lane wastes the room
        // above smaller ones.
        private static readonly int[] ClassSizes = [128, 256];

        private static readonly ModuleLog Log = ModuleLog.Create<GlyphAtlas>();

        // How long the plane keeps to its texture after the device could not make a bigger one.
        private static readonly long GrowRetryTicks = Stopwatch.Frequency;

        // Process-wide, so that images cached by fonts that outlive an atlas never match a slot of the next one.
        private static int lastGeneration;

        private readonly List<SlotInfo> slots = [];

        // Glyphs copied back are the ones drawn often, so slots of their own stay in use, and slots of new glyphs, many
        // drawn once, fall out of use whole. Each has the lanes of each size class.
        private readonly List<Lane>[] newLanes = NewLaneLists();
        private readonly List<Lane>[] restoredLanes = NewLaneLists();
        private readonly List<Op> ops = [];
        private byte[] data = new byte[64 * 1024];
        private byte[] strip = [];
        private int dataLength;

        private ComPtr<ID3D11Device> device;
        private ComPtr<ID3D11Texture2D> texture;
        private ComPtr<ID3D11ShaderResourceView> view;
        private long growFailedAt;

        /// <summary>Initializes a new instance of the <see cref="Plane"/> class.</summary>
        /// <param name="kind">What it holds.</param>
        /// <param name="maxSlices">The value of <see cref="MaxSlices"/>.</param>
        public Plane(PlaneKind kind, int maxSlices)
        {
            this.Kind = kind;
            this.MaxSlices = maxSlices;
        }

        /// <summary>Gets what it holds.</summary>
        public PlaneKind Kind { get; }

        /// <summary>Gets or sets the number of slices in use the plane keeps to, once the text shown needs no more.
        /// </summary>
        /// <remarks>Slices drawn from in this frame or the frame before are kept beyond it, up to
        /// <see cref="HardMaxSlices"/>. The end of a frame retires the others down to it.</remarks>
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

        // The most slices the plane has in use. Glyphs beyond are not drawn.
        private int HardMaxSlices => Math.Min(this.MaxSlices * HardLimitFactor, SliceLimit);

        private int MaxRetired => Math.Max(2, this.MaxSlices / 4);

        // Whether slots may be added beyond the texture. Not for a while after the device could not grow it.
        private bool CanGrow =>
            this.slots.Count < SliceLimit &&
            (this.growFailedAt == 0 || Stopwatch.GetTimestamp() - this.growFailedAt >= GrowRetryTicks);

        private int BytesPerTexel => this.Kind == PlaneKind.Color ? 4 : 1;

        private DXGI_FORMAT Format =>
            this.Kind == PlaneKind.Color ? DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM : DXGI_FORMAT.DXGI_FORMAT_R8_UNORM;

        /// <summary>Gets what a slot is doing.</summary>
        /// <param name="slot">The slot.</param>
        /// <returns>The state.</returns>
        public SlotState StateOf(int slot) => this.slots[slot].State;

        /// <inheritdoc/>
        public void Dispose() => this.Release();

        /// <summary>Makes the queued changes, and gets the texture array for the renderers to bind.</summary>
        /// <param name="context">The immediate context.</param>
        /// <param name="bytes">Receives the bytes uploaded.</param>
        /// <returns>The texture array, or null if there is nothing in it.</returns>
        /// <remarks><ul>
        /// <li>The plane keeps the device of the context of the first call. Later calls must use the same device.</li>
        /// <li>The view is valid until the next call.</li>
        /// <li>If the device cannot make a bigger texture, the slots beyond the old one are dropped with their glyphs.
        /// New glyphs that do not fit are not drawn until it is tried again, a second later.</li>
        /// <li>The time it takes is counted by <see cref="GlyphRasterBudget.CountUpload"/>.</li>
        /// </ul></remarks>
        public ID3D11ShaderResourceView* Flush(ID3D11DeviceContext* context, out long bytes)
        {
            bytes = 0;

            // The game has one device for the process. The atlas is disposed before the device goes away.
            if (this.device.IsEmpty())
                this.TakeDevice(context);
            Debug.Assert(this.IsDeviceOf(context), "The glyph atlas was flushed with another device.");

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

            // A burst of new glyphs may have grown them, which is not kept.
            if (this.data.Length > MaxKeptData)
                this.data = new byte[64 * 1024];
            if (this.strip.Length > MaxKeptData)
                this.strip = [];
            GlyphRasterBudget.CountUpload(Stopwatch.GetTimestamp() - start);
            return this.view.Get();
        }

        /// <summary>Reads a slot back from the GPU.</summary>
        /// <param name="slot">The slot.</param>
        /// <returns>Its texels, row by row.</returns>
        /// <exception cref="InvalidOperationException">If it has not been flushed yet.</exception>
        /// <remarks>For tests.</remarks>
        public byte[] Read(int slot)
        {
            if (this.device.IsEmpty())
                throw new InvalidOperationException("The plane has not been flushed to a device yet.");

            this.FlushNow();
            using var context = default(ComPtr<ID3D11DeviceContext>);
            this.device.Get()->GetImmediateContext(context.GetAddressOf());
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = SliceSize,
                Height = SliceSize,
                MipLevels = 1,
                ArraySize = 1,
                Format = this.Format,
                SampleDesc = new(1, 0),
                Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
                CPUAccessFlags = (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
            };
            using var staging = default(ComPtr<ID3D11Texture2D>);
            this.device.Get()->CreateTexture2D(&desc, null, staging.GetAddressOf()).ThrowOnError();
            context.Get()->CopySubresourceRegion(
                (ID3D11Resource*)staging.Get(), 0, 0, 0, 0, (ID3D11Resource*)this.texture.Get(), (uint)slot, null);

            var mapped = default(D3D11_MAPPED_SUBRESOURCE);
            context.Get()->Map((ID3D11Resource*)staging.Get(), 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped).ThrowOnError();
            try
            {
                var rowBytes = SliceSize * this.BytesPerTexel;
                var result = new byte[rowBytes * SliceSize];
                for (var y = 0; y < SliceSize; y++)
                {
                    new ReadOnlySpan<byte>((byte*)mapped.pData + (y * mapped.RowPitch), rowBytes)
                        .CopyTo(result.AsSpan(y * rowBytes));
                }

                return result;
            }
            finally
            {
                context.Get()->Unmap((ID3D11Resource*)staging.Get(), 0);
            }
        }

        /// <summary>Tests whether a slot holds glyphs in use of a generation.</summary>
        /// <param name="slot">The slot.</param>
        /// <param name="generation">The generation.</param>
        /// <returns>Whether it does.</returns>
        internal bool IsCurrent(int slot, int generation) =>
            this.IsAvailable(slot, generation) && this.slots[slot].State == SlotState.Current;

        /// <summary>Tests whether a slot still has the pixels of a generation, in use or retired.</summary>
        /// <param name="slot">The slot.</param>
        /// <param name="generation">The generation.</param>
        /// <returns>Whether it does.</returns>
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

            // Allocating may take the slot copied from, if it had been retired. Copying within a subresource is
            // undefined in D3D11, and may hang the GPU.
            var available = this.IsAvailable(sourceSlot, sourceGeneration);
            Debug.Assert(!available || slot != sourceSlot, "A glyph was to be copied within its slot.");
            if (!available || slot == sourceSlot)
            {
                FreeLast(this.restoredLanes, slot, x, y, width);
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

        /// <summary>Takes back the last <see cref="TryPut"/> or <see cref="TryCopy"/>, for a glyph that is not added
        /// after all.</summary>
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
            FreeLast(op.Kind == OpKind.Upload ? this.newLanes : this.restoredLanes, slot, x, y, op.Width);
        }

        /// <summary>Retires unused slices beyond <see cref="MaxSlices"/>, frees old retired ones, and makes the queued
        /// changes.</summary>
        /// <param name="frame">The frame that ends.</param>
        internal void EndFrame(long frame)
        {
            // Those still drawn from stay, as the text shown needs them.
            for (var current = this.SliceCount; current > this.MaxSlices; current--)
            {
                var unused = this.LeastRecentlyUsed(frame);
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
                this.slots[this.OldestRetired()].State = SlotState.Free;

            if (!this.device.IsEmpty() && this.ops.Count > 0)
                this.FlushNow();
        }

        private static List<Lane>[] NewLaneLists()
        {
            var lists = new List<Lane>[ClassSizes.Length + 1];
            for (var i = 0; i < lists.Length; i++)
                lists[i] = [];
            return lists;
        }

        private static void RemoveLanes(List<Lane>[] classes, int from, int to)
        {
            foreach (var lanes in classes)
            {
                for (var i = lanes.Count - 1; i >= 0; i--)
                {
                    if (lanes[i].Slot >= from && lanes[i].Slot < to)
                        lanes.RemoveAt(i);
                }
            }
        }

        // A slot has one lane.
        private static void FreeLast(List<Lane>[] classes, int slot, int x, int y, int width)
        {
            foreach (var lanes in classes)
            {
                foreach (var lane in lanes)
                {
                    if (lane.Slot == slot)
                    {
                        lane.FreeLast(x, y, width + 1);
                        return;
                    }
                }
            }
        }

        // Shelf packing in the open lanes of the size class, taking a slot for a new lane if none has room.
        private bool TryAllocate(
            List<Lane>[] classes, int width, int height, long frame, out int slot, out int x, out int y)
        {
            var size = Math.Max(width, height);
            var sizeClass = 0;
            while (sizeClass < ClassSizes.Length && size > ClassSizes[sizeClass])
                sizeClass++;
            var lanes = classes[sizeClass];

            // With a gap of one texel, which taking a slot cleared, for bilinear sampling at the edges.
            foreach (var lane in lanes)
            {
                if (!lane.Full && lane.TryAllocate(width + 1, height + 1, out x, out y))
                {
                    slot = lane.Slot;
                    return true;
                }
            }

            var taken = this.Take(frame);
            if (taken >= 0)
            {
                var lane = new Lane(taken);
                lanes.Add(lane);
                if (lane.TryAllocate(width + 1, height + 1, out x, out y))
                {
                    slot = taken;
                    return true;
                }
            }

            slot = x = y = 0;
            return false;
        }

        // A cleared slot, or -1 if there can be no more. At MaxSlices, retires a slice not drawn from in this frame or
        // the frame before first.
        private int Take(long frame)
        {
            if (this.SliceCount >= this.MaxSlices)
            {
                // A slice drawn from in the frame before is likely drawn from again, and copying all its glyphs back
                // costs more than going past MaxSlices for a while.
                var lru = this.LeastRecentlyUsed(frame - 1);
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
                take = this.OldestRetired();
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

        // The slice in use that was used least recently, and of those the oldest, or -1 if all were used in a frame or
        // later.
        private int LeastRecentlyUsed(long frame)
        {
            var lru = -1;
            for (var i = 0; i < this.slots.Count; i++)
            {
                var s = this.slots[i];
                if (s.State != SlotState.Current || s.LastUsedFrame >= frame)
                    continue;
                var l = lru < 0 ? null : this.slots[lru];
                if (l is null || s.LastUsedFrame < l.LastUsedFrame ||
                    (s.LastUsedFrame == l.LastUsedFrame && s.Generation < l.Generation))
                {
                    lru = i;
                }
            }

            return lru;
        }

        private void Retire(int index, long frame)
        {
            var slot = this.slots[index];
            slot.State = SlotState.Retired;
            slot.RetiredFrame = frame;
            this.GlyphCount -= slot.GlyphCount;
            slot.GlyphCount = 0;
            RemoveLanes(this.newLanes, index, index + 1);
            RemoveLanes(this.restoredLanes, index, index + 1);
            this.EvictionCount++;
        }

        // The retired slot retired longest ago, and of those the one taken longest ago, or -1.
        private int OldestRetired()
        {
            var oldest = -1;
            for (var i = 0; i < this.slots.Count; i++)
            {
                var s = this.slots[i];
                if (s.State != SlotState.Retired)
                    continue;
                var o = oldest < 0 ? null : this.slots[oldest];
                if (o is null || s.RetiredFrame < o.RetiredFrame ||
                    (s.RetiredFrame == o.RetiredFrame && s.Generation < o.Generation))
                {
                    oldest = i;
                }
            }

            return oldest;
        }

        private int Count(SlotState state)
        {
            var n = 0;
            foreach (var s in this.slots)
                n += s.State == state ? 1 : 0;
            return n;
        }

        // Forgets everything, and lets go of the texture and the device.
        private void Release()
        {
            this.slots.Clear();
            RemoveLanes(this.newLanes, 0, int.MaxValue);
            RemoveLanes(this.restoredLanes, 0, int.MaxValue);
            this.ops.Clear();
            this.dataLength = 0;
            this.GlyphCount = 0;
            this.ReleaseTexture();
            this.device.Reset();
        }

        private void FlushNow()
        {
            using var context = default(ComPtr<ID3D11DeviceContext>);
            this.device.Get()->GetImmediateContext(context.GetAddressOf());
            _ = this.Flush(context, out _);
        }

        // Uploads a run of glyphs put next to each other on a shelf with one call, as one strip with zeros in the gaps.
        // No other glyph is in the strip, and taking the slot cleared the gaps. Returns the index of the run's last op.
        private int UploadRun(ID3D11DeviceContext* context, ID3D11Resource* resource, int first, ref long bytes)
        {
            var op = this.ops[first];
            var last = first;
            var height = op.Height;
            while (last + 1 < this.ops.Count &&
                   this.ops[last + 1] is { Kind: OpKind.Upload } next &&
                   next.Slot == op.Slot &&
                   next.Y == op.Y &&
                   next.X == this.ops[last].X + this.ops[last].Width + 1)
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

        // Through a local, as the field is not pinned.
        private void TakeDevice(ID3D11DeviceContext* context)
        {
            var contextDevice = default(ComPtr<ID3D11Device>);
            context->GetDevice(contextDevice.GetAddressOf());
            contextDevice.Swap(ref this.device);
        }

        // For debug builds only, as it calls into COM.
        private bool IsDeviceOf(ID3D11DeviceContext* context)
        {
            using var contextDevice = default(ComPtr<ID3D11Device>);
            context->GetDevice(contextDevice.GetAddressOf());
            return contextDevice.Get() == this.device.Get();
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
            if (this.device.Get()->CreateRenderTargetView(
                    (ID3D11Resource*)this.texture.Get(), &desc, targetView.GetAddressOf()).FAILED)
            {
                return;
            }

            var zero = default(Vector4);
            context->ClearRenderTargetView(targetView, (float*)&zero);
        }

        // Makes a bigger texture array with the texels of the old one, with room for more slots than there are. False
        // if the device could not make one even for the slots there are.
        private bool TryGrow(ID3D11DeviceContext* context)
        {
            var needed = this.slots.Count;
            var target = this.GrowTarget();
            var hr = this.TryReplaceTexture(context, target);
            if (hr.FAILED && target > needed)
                hr = this.TryReplaceTexture(context, needed);
            if (hr.FAILED)
                return this.OnGrowFailed(needed, hr);

            if (this.growFailedAt != 0)
            {
                this.growFailedAt = 0;
                Log.Information("The {Kind} glyph texture has grown to {Count} slices", this.Kind, this.Capacity);
            }

            return true;
        }

        // The slices to grow to. Half as many again as before and at least two more, so that the texture array is made
        // and copied a few times only, but no more than the plane keeps to unless more slots are in use already.
        private int GrowTarget()
        {
            var keptTo = this.MaxSlices + this.MaxRetired;
            var limit = this.slots.Count <= keptTo ? keptTo : this.HardMaxSlices + this.MaxRetired;
            var grown = this.Capacity + Math.Max(2, this.Capacity / 2);
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
            var hr = this.device.Get()->CreateTexture2D(&desc, null, newTexture.GetAddressOf());
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
            hr = this.device.Get()->CreateShaderResourceView(
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

        private bool OnGrowFailed(int newCapacity, HRESULT hr)
        {
            this.growFailedAt = Math.Max(Stopwatch.GetTimestamp(), 1);
            Log.Verbose(
                "Could not grow the {Kind} glyph texture to {Count} slices (0x{Result:X8}). Glyphs that do not fit are not drawn",
                this.Kind,
                newCapacity,
                hr.Value);
            return false;
        }

        // Forgets the slots beyond the texture, with their glyphs and the changes queued for them. Quads of this frame
        // that are already laid out on them draw texels of the last slice for one frame, as D3D11 clamps the index.
        private void DropSlotsBeyondCapacity()
        {
            var keep = this.Capacity;
            for (var i = keep; i < this.slots.Count; i++)
                this.GlyphCount -= this.slots[i].GlyphCount;
            this.slots.RemoveRange(keep, this.slots.Count - keep);
            RemoveLanes(this.newLanes, keep, int.MaxValue);
            RemoveLanes(this.restoredLanes, keep, int.MaxValue);
            this.ops.RemoveAll(x => x.Slot >= keep || (x.Kind == OpKind.Copy && x.SourceSlot >= keep));
        }

        private void ReleaseTexture()
        {
            this.view.Reset();
            this.texture.Reset();
            this.Capacity = 0;
        }
    }

    // What is known of a slot of a plane's texture array.
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

        // The shelf keeps its height.
        public void FreeLast(int x, int y, int w)
        {
            if (this.cursorY == y && this.cursorX == x + w)
                this.cursorX = x;
        }
    }
}
