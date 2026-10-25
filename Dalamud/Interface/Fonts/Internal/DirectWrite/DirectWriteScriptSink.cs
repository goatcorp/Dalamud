using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>An <see cref="IDWriteTextAnalysisSink"/> that keeps the results of
/// <see cref="IDWriteTextAnalyzer.AnalyzeScript"/> or <see cref="IDWriteTextAnalyzer.AnalyzeLineBreakpoints"/> in a
/// caller's buffer.</summary>
/// <remarks>Stack-allocated for a single call and populates whichever buffer was supplied.</remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DirectWriteScriptSink
{
    private static readonly void** Vtbl = CreateVtbl();

    // Must remain the first field because &sink is cast directly to IDWriteTextAnalysisSink*.
    private void** vtbl;
    private ScriptRun* runs;
    private DWRITE_LINE_BREAKPOINT* breakpoints;
    private int capacity;

    /// <summary>Initializes a new instance of the <see cref="DirectWriteScriptSink"/> struct, for script runs.
    /// </summary>
    /// <param name="runs">Receives the runs.</param>
    /// <param name="capacity">Capacity of <paramref name="runs"/>.</param>
    /// <remarks>One run per code unit is always enough.</remarks>
    public DirectWriteScriptSink(ScriptRun* runs, int capacity)
    {
        this.vtbl = Vtbl;
        this.runs = runs;
        this.breakpoints = null;
        this.capacity = capacity;
    }

    /// <summary>Initializes a new instance of the <see cref="DirectWriteScriptSink"/> struct, for line breakpoints.
    /// </summary>
    /// <param name="breakpoints">Receives the breakpoints, one per code unit.</param>
    /// <param name="capacity">Capacity of <paramref name="breakpoints"/>.</param>
    public DirectWriteScriptSink(DWRITE_LINE_BREAKPOINT* breakpoints, int capacity)
    {
        this.vtbl = Vtbl;
        this.runs = null;
        this.breakpoints = breakpoints;
        this.capacity = capacity;
    }

    /// <summary>Gets the number of script runs recorded, up to capacity (<c>0</c> in breakpoint mode).</summary>
    public int Count { get; private set; }

    private static void** CreateVtbl()
    {
        var v = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(DirectWriteScriptSink), sizeof(void*) * 7);
        Debug.Assert(v is not null, "AllocateTypeAssociatedMemory throws rather than returning null.");
        v[0] = (delegate* unmanaged<DirectWriteScriptSink*, Guid*, void**, int>)&QueryInterface;
        v[1] = (delegate* unmanaged<DirectWriteScriptSink*, uint>)&AddRefOrRelease;
        v[2] = (delegate* unmanaged<DirectWriteScriptSink*, uint>)&AddRefOrRelease;
        v[3] =
            (delegate* unmanaged<DirectWriteScriptSink*, uint, uint, DWRITE_SCRIPT_ANALYSIS*, int>)&SetScriptAnalysis;
        v[4] =
            (delegate* unmanaged<DirectWriteScriptSink*, uint, uint, DWRITE_LINE_BREAKPOINT*, int>)&SetLineBreakpoints;
        v[5] = (delegate* unmanaged<DirectWriteScriptSink*, uint, uint, byte, byte, int>)&SetBidiLevel;
        v[6] = (delegate* unmanaged<DirectWriteScriptSink*, uint, uint, void*, int>)&Ignore;
        return v;
    }

    [UnmanagedCallersOnly]
    private static int QueryInterface(DirectWriteScriptSink* self, Guid* iid, void** obj)
    {
        if (*iid == IID.IID_IUnknown || *iid == IID.IID_IDWriteTextAnalysisSink)
        {
            *obj = self;
            return 0;
        }

        *obj = null;
        return E.E_NOINTERFACE;
    }

    // Not reference counted, as it lives on the stack.
    [UnmanagedCallersOnly]
    private static uint AddRefOrRelease(DirectWriteScriptSink* self) => 1;

    [UnmanagedCallersOnly]
    private static int SetScriptAnalysis(
        DirectWriteScriptSink* self, uint position, uint length, DWRITE_SCRIPT_ANALYSIS* analysis)
    {
        if (self->runs is not null && self->Count < self->capacity)
            self->runs[self->Count++] = new() { Position = position, Length = length, Analysis = *analysis };
        return 0;
    }

    [UnmanagedCallersOnly]
    private static int SetLineBreakpoints(
        DirectWriteScriptSink* self, uint position, uint length, DWRITE_LINE_BREAKPOINT* breakpoints)
    {
        if (self->breakpoints is not null && position < (uint)self->capacity)
        {
            var n = (int)Math.Min(length, (uint)self->capacity - position);
            new ReadOnlySpan<DWRITE_LINE_BREAKPOINT>(breakpoints, n).CopyTo(new(self->breakpoints + position, n));
        }

        return 0;
    }

    [UnmanagedCallersOnly]
    private static int SetBidiLevel(
        DirectWriteScriptSink* self, uint position, uint length, byte explicitLevel, byte resolvedLevel) => 0;

    // No-op vtable slot 6 for IDWriteTextAnalysisSink.SetNumberSubstitution.
    [UnmanagedCallersOnly]
    private static int Ignore(DirectWriteScriptSink* self, uint position, uint length, void* data) => 0;

    /// <summary>A run of one script.</summary>
    public struct ScriptRun
    {
        /// <summary>Start, in code units.</summary>
        public uint Position;

        /// <summary>Length, in code units.</summary>
        public uint Length;

        /// <summary>The script.</summary>
        public DWRITE_SCRIPT_ANALYSIS Analysis;
    }
}
