using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.Win32.SafeHandles;

namespace Dalamud.Broker.Win32;

internal sealed class ProcessWaiter : IDisposable
{
    private readonly ManualResetEvent mDisposedEvent = new(false);
    private readonly ChannelReader<object?> mChannelReader;

    public ProcessWaiter(SafeProcessHandle process)
    {
        // Note that content is never written to the channel. It's only used for completion.
        var channelOptions = new BoundedChannelOptions(1)
        {
            SingleWriter = true
        };
        var channel = Channel.CreateBounded<object?>(channelOptions);
        this.mChannelReader = channel.Reader;

        // Start a working thread.
        var threadName = $"GameWaiter Thread ({process.DangerousGetHandle():X8}h)";
        var thread = new Thread(() => this.ThreadMain(process, channel.Writer))
        {
            Name = threadName,
            IsBackground = true
        };
        thread.Start();
    }

    public void Dispose()
    {
        // Also we should signal the working thread...
        this.mDisposedEvent.Set();
        this.mDisposedEvent.Dispose();
    }

    private void ThreadMain(SafeProcessHandle process, ChannelWriter<object?> writer)
    {
        // NOTE:
        // Even though SafeProcessHandle doesn't inherit WaitHandle (so we can't use WaitHandle.WaitAny),
        // you still can wait on the process handle to be notified when the associated process exits.[1]
        //
        // 
        //
        // [1]: https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitformultipleobjects#remarks
        //
        //
        var safeHandles = new SafeHandle[]
        {
            this.mDisposedEvent.SafeWaitHandle,
            process,
        };
        var acquiredArc = new bool[safeHandles.Length];
        var rawHandles = new HANDLE[safeHandles.Length];

        var numHandles = safeHandles.Length;
        try
        {
            // First, we acquire ref handle. This is essentially doing Arc::clone() manually.
            //
            // If any of them fails to acquire arc, an exception will be thrown. `finally` block will
            // catch this and correctly release refs we just acquired.
            for (var i = 0; i < numHandles; i++)
            {
                safeHandles[i].DangerousAddRef(ref acquiredArc[i]);
                rawHandles[i] = (HANDLE)safeHandles[i].DangerousGetHandle();
            }

            // Block until either disposed or process handle(onExit) is signaled.
            var errc = PInvoke.WaitForMultipleObjects(rawHandles, false, PInvoke.INFINITE);
            if (errc is WIN32_ERROR.WAIT_FAILED)
            {
                throw new Win32Exception();
            }
        } finally
        {
            // Lastly, we release arcs we acquired from the first step.
            for (var i = 0; i < numHandles; i++)
            {
                if (acquiredArc[i])
                {
                    safeHandles[i].DangerousRelease();
                }
            }

            // Notify the channel that we're done here.
            writer.Complete();
        }
    }

    /// <summary>
    /// Waits until the process exit.
    /// </summary>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        // Channel<T> only supports async operations. ¯\_(ツ)_/¯
        await this.mChannelReader.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
