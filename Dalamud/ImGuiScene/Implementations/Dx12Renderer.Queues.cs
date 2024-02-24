using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.ImGuiScene.Helpers;
using Dalamud.Utility;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using Win32 = TerraFX.Interop.Windows.Windows;

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
    private sealed class GraphicsCommandListWrapper : IDisposable
    {
        private ComPtr<ID3D12CommandAllocator> allocator;
        private ComPtr<ID3D12GraphicsCommandList> list;

        public GraphicsCommandListWrapper(ID3D12Device* device, D3D12_COMMAND_LIST_TYPE type, string debugName)
        {
            try
            {
                fixed (Guid* piid = &IID.IID_ID3D12CommandAllocator)
                fixed (ID3D12CommandAllocator** pp = &this.allocator.GetPinnableReference())
                fixed (void* pName = $"{debugName}.{nameof(this.allocator)}")
                {
                    device->CreateCommandAllocator(type, piid, (void**)pp).ThrowOnError();
                    this.allocator.Get()->SetName((ushort*)pName).ThrowOnError();
                }

                fixed (Guid* piid = &IID.IID_ID3D12GraphicsCommandList)
                fixed (ID3D12GraphicsCommandList** pp = &this.list.GetPinnableReference())
                fixed (void* pName = $"{debugName}.{nameof(this.list)}")
                {
                    device->CreateCommandList(0, type, this.allocator, null, piid, (void**)pp).ThrowOnError();
                    this.list.Get()->Close().ThrowOnError();
                    this.list.Get()->SetName((ushort*)pName).ThrowOnError();
                }
            }
            catch
            {
                this.ReleaseUnmanagedResources();
                throw;
            }
        }

        ~GraphicsCommandListWrapper() => this.ReleaseUnmanagedResources();

        public ID3D12GraphicsCommandList* CommandList => this.list.Get();

        public CommandListCloser Record(out ID3D12GraphicsCommandList* commandList)
        {
            this.allocator.Get()->Reset().ThrowOnError();
            this.list.Get()->Reset(this.allocator, null).ThrowOnError();
            return new(commandList = this.list);
        }

        public void Dispose()
        {
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            this.allocator.Reset();
            this.list.Reset();
        }

        public struct CommandListCloser : IDisposable
        {
            private ComPtr<ID3D12GraphicsCommandList> list;

            public CommandListCloser(ID3D12GraphicsCommandList* list) => this.list = new(list);

            public void Dispose()
            {
                if (!this.list.IsEmpty())
                    this.list.Get()->Close();
                this.list.Reset();
            }
        }
    }

    private sealed class CommandQueueWrapper : IDisposable
    {
        private ComPtr<ID3D12CommandQueue> queue;
        private ComPtr<ID3D12Fence> fence;
        private ulong fenceCounter;

        public CommandQueueWrapper(ID3D12Device* device, in D3D12_COMMAND_QUEUE_DESC desc, string debugName)
        {
            try
            {
                fixed (Guid* piid = &IID.IID_ID3D12CommandQueue)
                fixed (ID3D12CommandQueue** pp = &this.queue.GetPinnableReference())
                fixed (D3D12_COMMAND_QUEUE_DESC* pDesc = &desc)
                fixed (void* pName = $"{debugName}:{nameof(this.queue)}")
                {
                    device->CreateCommandQueue(pDesc, piid, (void**)pp).ThrowOnError();
                    this.queue.Get()->SetName((ushort*)pName).ThrowOnError();
                }

                fixed (Guid* piid = &IID.IID_ID3D12Fence)
                fixed (ID3D12Fence** pp = &this.fence.GetPinnableReference())
                fixed (void* pName = $"{debugName}:{nameof(this.fence)}")
                {
                    device->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, piid, (void**)pp).ThrowOnError();
                    this.fence.Get()->SetName((ushort*)pName).ThrowOnError();
                }

                this.fenceCounter = 0;
            }
            catch
            {
                this.ReleaseUnmanagedResources();
                throw;
            }
        }

        public CommandQueueWrapper(ID3D12Device* device, ID3D12CommandQueue* queue, string debugName)
        {
            try
            {
                this.queue = new(queue);
                fixed (ComPtr<ID3D12CommandQueue>* ppQueue = &this.queue)
                    ReShadePeeler.PeelD3D12CommandQueue(ppQueue);
                fixed (void* pName = $"{debugName}:{nameof(this.queue)}")
                    this.queue.Get()->SetName((ushort*)pName).ThrowOnError();

                fixed (Guid* piid = &IID.IID_ID3D12Fence)
                fixed (ID3D12Fence** pp = &this.fence.GetPinnableReference())
                fixed (void* pName = $"{debugName}:{nameof(this.fence)}")
                {
                    device->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, piid, (void**)pp).ThrowOnError();
                    this.fence.Get()->SetName((ushort*)pName).ThrowOnError();
                }

                this.fenceCounter = 0;
            }
            catch
            {
                this.ReleaseUnmanagedResources();
                throw;
            }
        }

        ~CommandQueueWrapper() => this.ReleaseUnmanagedResources();

        public bool HasPendingWork =>
            !this.fence.IsEmpty() && this.fence.Get()->GetCompletedValue() != this.fenceCounter;

        public void Dispose()
        {
            this.ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public ulong Submit(GraphicsCommandListWrapper commandListWrapper) =>
            this.Submit(commandListWrapper.CommandList);

        public ulong Submit<T>(T* command)
            where T : unmanaged, ID3D12CommandList.Interface
        {
            var fenceValue = Interlocked.Increment(ref this.fenceCounter);
            this.queue.Get()->ExecuteCommandLists(1, (ID3D12CommandList**)&command);
            this.queue.Get()->Signal(this.fence, fenceValue).ThrowOnError();
            return fenceValue;
        }

        public ulong Submit<T>(T** commands, int count)
            where T : unmanaged, ID3D12CommandList.Interface
        {
            if (count < 1)
                return this.fenceCounter;
            var fenceValue = Interlocked.Increment(ref this.fenceCounter);
            this.queue.Get()->ExecuteCommandLists((uint)count, (ID3D12CommandList**)commands);
            this.queue.Get()->Signal(this.fence, fenceValue).ThrowOnError();
            return fenceValue;
        }

        public void Wait(HANDLE hEvent = default) => this.Wait(this.fenceCounter, hEvent);

        public void WaitMostRecent(HANDLE hEvent = default)
        {
            var fenceValue = Interlocked.Increment(ref this.fenceCounter);
            this.queue.Get()->Signal(this.fence, fenceValue).ThrowOnError();
            this.Wait(fenceValue, hEvent);
        }

        public void Wait(ulong fenceValue, HANDLE hEvent = default)
        {
            if (!this.HasPendingWork)
                return;

            var closeHandleAfter = false;
            if (hEvent == default)
            {
                hEvent = Win32.CreateEventW(null, true, false, null);
                if (hEvent == default)
                    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new();
                closeHandleAfter = true;
            }

            try
            {
                if (!Win32.ResetEvent(hEvent))
                    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new();
                this.fence.Get()->SetEventOnCompletion(fenceValue, hEvent).ThrowOnError();
                if (Win32.WaitForSingleObject(hEvent, Win32.INFINITE) == WAIT.WAIT_FAILED)
                    throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new();
            }
            finally
            {
                if (closeHandleAfter)
                    Win32.CloseHandle(hEvent);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            this.Wait();
            this.queue.Reset();
            this.fence.Reset();
        }
    }
}
