using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.ImGuiScene.Implementations;

/// <summary>
/// Deals with rendering ImGui using DirectX 12.
/// See https://github.com/ocornut/imgui/blob/master/examples/imgui_impl_dx12.cpp for the original implementation.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed/using scopes")]
internal unsafe partial class Dx12Renderer
{
    private class TextureManager : IDisposable
    {
        private readonly FixedObjectPool<StructWrapper<Win32Handle>> eventPool;
        private readonly FixedObjectPool<CommandQueueWrapper> commandQueuePool;
        private readonly FixedObjectPool<GraphicsCommandListWrapper> commandListPool;
        private readonly List<FixedObjectPool<GraphicsCommandListWrapper>.Returner> flushTempList;

        private readonly AutoResourceHeap uploadHeap;
        private readonly AutoResourceHeap textureHeap;

        private readonly object uploadListLock = new();
        private List<TextureData> pending1 = new();
        private List<TextureData> pending2 = new();
        private List<FixedObjectPool<CommandQueueWrapper>.Returner> activeQueues1;
        private List<FixedObjectPool<CommandQueueWrapper>.Returner> activeQueues2;

        private ComPtr<ID3D12Device> device;
        private bool disposed;

        public TextureManager(
            ID3D12Device* device,
            int queuePoolCapacity = 8,
            ulong commonUploadHeapSize = 16 * 1048576,
            ulong commonTextureHeapSize = 64 * 1048576)
        {
            if (queuePoolCapacity <= 0)
                queuePoolCapacity = Environment.ProcessorCount;

            try
            {
                device->AddRef();
                this.device.Attach(device);
                this.eventPool = new(_ => new(Win32Handle.CreateEvent()), queuePoolCapacity);

                this.uploadHeap = new(
                    device,
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_CREATE_NOT_ZEROED |
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
                    new() { Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD },
                    commonUploadHeapSize,
                    debugName: $"{nameof(TextureManager)}.{nameof(this.uploadHeap)}");

                this.textureHeap = new(
                    device,
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_CREATE_NOT_ZEROED |
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES,
                    new() { Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT },
                    commonTextureHeapSize,
                    debugName: $"{nameof(TextureManager)}.{nameof(this.textureHeap)}");

                this.commandQueuePool = new(
                    i => new(
                        device,
                        new D3D12_COMMAND_QUEUE_DESC
                        {
                            Type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_COPY,
                            Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_DISABLE_GPU_TIMEOUT,
                        },
                        $"{nameof(TextureManager)}.{nameof(this.commandQueuePool)}[{i}]"),
                    queuePoolCapacity);
                
                this.commandListPool = new(
                    i => new(
                        device,
                        D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_COPY,
                        $"{nameof(TextureManager)}.{nameof(this.commandListPool)}[{i}]"),
                    queuePoolCapacity);
                
                this.flushTempList = new(queuePoolCapacity);
                this.activeQueues1 = new(queuePoolCapacity);
                this.activeQueues2 = new(queuePoolCapacity);
            }
            catch
            {
                this.commandListPool?.Dispose();
                this.commandQueuePool?.Dispose();
                this.eventPool?.Dispose();
                this.uploadHeap?.Dispose();
                this.textureHeap?.Dispose();
                this.ReleaseUnmanagedResources();
                throw;
            }
        }

        ~TextureManager() => this.ReleaseUnmanagedResources();

        public void Dispose()
        {
            if (this.disposed)
                return;
            this.disposed = true;

            this.FlushPendingTextureUploads();

            lock (this.uploadListLock)
            {
                foreach (var x in this.activeQueues1)
                {
                    x.O.Wait();
                    x.Dispose();
                }

                foreach (ref var v in CollectionsMarshal.AsSpan(this.pending1))
                    v.Dispose();

                this.activeQueues1.Clear();
                this.pending1.Clear();
            }

            this.commandQueuePool.Dispose();
            this.commandListPool.Dispose();
            this.eventPool.Dispose();
            this.uploadHeap.Dispose();
            this.textureHeap.Dispose();
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public TextureWrap CreateTexture(
            ReadOnlySpan<byte> data,
            int pitch,
            int width,
            int height,
            DXGI_FORMAT format,
            string debugName)
        {
            uint numRows;
            int uploadPitch;
            int uploadSize;

            // Create an empty texture of same specifications with the request
            using var texture = default(ComPtr<ID3D12Resource>);
            this.textureHeap.CreateResource(
                out var resDesc,
                new()
                {
                    Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                    Alignment = D3D12.D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT,
                    Width = (ulong)width,
                    Height = (uint)height,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = format,
                    SampleDesc = { Count = 1, Quality = 0 },
                    Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,
                    Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
                },
                debugName: debugName).Swap(&texture);

            ulong cbRow;
            this.device.Get()->GetCopyableFootprints(&resDesc, 0, 1, 0, null, &numRows, &cbRow, null);
            if (pitch == (int)cbRow)
            {
                uploadPitch = ((checked((int)cbRow) + D3D12.D3D12_TEXTURE_DATA_PITCH_ALIGNMENT) - 1) &
                              ~(D3D12.D3D12_TEXTURE_DATA_PITCH_ALIGNMENT - 1);
                uploadSize = checked((int)(numRows * uploadPitch));
            }
            else
            {
                throw new ArgumentException(
                    $"The provided pitch {pitch} does not match the calculated pitch of {cbRow}.",
                    nameof(pitch));
            }

            // Upload texture to graphics system
            using var uploadBuffer = default(ComPtr<ID3D12Resource>);
            this.uploadHeap.CreateResource(
                out resDesc,
                new()
                {
                    Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                    Alignment = 0,
                    Width = (ulong)uploadSize,
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                    SampleDesc = { Count = 1, Quality = 0 },
                    Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                    Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
                },
                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_SOURCE,
                debugName: debugName).Swap(&uploadBuffer);
            
            try
            {
                void* mapped;
                var range = new D3D12_RANGE(0, (nuint)uploadSize);
                uploadBuffer.Get()->Map(0, &range, &mapped).ThrowHr();
                var source = data;
                var target = new Span<byte>(mapped, uploadSize);
                for (var y = 0; y < numRows; y++)
                {
                    source[..pitch].CopyTo(target);
                    source = source[pitch..];
                    target = target[uploadPitch..];
                }
            }
            finally
            {
                uploadBuffer.Get()->Unmap(0, null);
            }

            using var texData = new TextureData(format, width, height, uploadPitch, texture, uploadBuffer);

            // deal with completed ones in the active queue, so that the following TryRent is more likely to succeed.
            lock (this.uploadListLock)
            {
                this.activeQueues1.RemoveAll(
                    x =>
                    {
                        if (x.O.HasPendingWork)
                            return false;
                        x.Dispose();
                        return true;
                    });
            }

            if (this.commandListPool.TryRent(out var rentedList))
            {
                FixedObjectPool<CommandQueueWrapper>.Returner rentedQueue = default;
                var texDataPtrCopy = texData.CloneRef();
                var giveup = () =>
                {
                    texDataPtrCopy?.ClearUploadBuffer();
                    texDataPtrCopy?.Dispose();
                    texDataPtrCopy = null;
                    rentedList.Dispose();
                };
                try
                {
                    rentedQueue = this.commandQueuePool.Rent(giveup);
                    using (rentedList.O.Record(out var cmdList))
                        texDataPtrCopy.WriteCopyCommand(cmdList);
                    rentedQueue.O.Submit(rentedList.O);
                }
                catch
                {
                    rentedQueue.Dispose();
                    giveup.Invoke();
                    throw;
                }

                lock (this.uploadListLock)
                    this.activeQueues1.Add(rentedQueue);
            }
            else
            {
                lock (this.uploadListLock)
                    this.pending1.Add(texData.CloneRef());
            }

            return TextureWrap.NewReference(texData);
        }

        public void FlushPendingTextureUploads()
        {
            // don't care about incoherent view of the internal size variable at this point;
            // expectation is that the draw list is already complete, and the textures used
            // are either already finished uploading, or in progress of doing so.
            if (this.pending1.Count != 0)
            {
                lock (this.uploadListLock)
                    (this.pending2, this.pending1) = (this.pending1, this.pending2);

                using var queue = this.commandQueuePool.Rent(
                    () =>
                    {
                        foreach (var x in this.flushTempList)
                            x.Dispose();
                        this.flushTempList.Clear();
                    });

                var numIters = Math.Max(this.pending2.Count, this.commandListPool.Capacity);
                this.flushTempList.Add(this.commandListPool.Rent());
                for (var i = 1; i < numIters && this.commandListPool.TryRent(out var r); i++)
                    this.flushTempList.Add(r);

                var commandLists = stackalloc ComPtr<ID3D12GraphicsCommandList>[this.flushTempList.Count];
                for (int i = 0, j = 0; i < this.flushTempList.Count; i++)
                {
                    var listWrapper = this.flushTempList[i].O;
                    using (listWrapper.Record(out var cmdList))
                    {
                        var jTo = (this.pending2.Count * (i + 1)) / this.flushTempList.Count;
                        for (; j < jTo; j++)
                            this.pending2[j].WriteCopyCommand(cmdList);
                    }

                    commandLists[i] = listWrapper.CommandList;
                }

                queue.O.Submit(commandLists->GetAddressOf(), this.flushTempList.Count);
                using (var waiter = this.eventPool.Rent())
                    queue.O.Wait(waiter.O.O);

                foreach (ref var v in CollectionsMarshal.AsSpan(this.pending2))
                {
                    v.ClearUploadBuffer();
                    v.Dispose();
                }

                this.pending2.Clear();
            }

            // same reasoning with the above, but for uploads already in progress.
            if (this.activeQueues1.Count != 0)
            {
                lock (this.uploadListLock)
                    (this.activeQueues2, this.activeQueues1) = (this.activeQueues1, this.activeQueues2);
            
                using (var waiter = this.eventPool.Rent())
                {
                    foreach (var x in this.activeQueues2)
                    {
                        x.O.Wait(waiter.O.O);
                        x.Dispose();
                    }
                }
            
                this.activeQueues2.Clear();
            }
            
            this.uploadHeap.ClearEmptyHeaps();
            this.textureHeap.ClearEmptyHeaps();
        }

        private void ReleaseUnmanagedResources()
        {
            this.device.Reset();
        }
    }
}
