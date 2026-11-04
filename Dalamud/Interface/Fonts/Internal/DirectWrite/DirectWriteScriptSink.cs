using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Fonts.Internal.DirectWrite;

/// <summary>An <see cref="IDWriteTextAnalysisSink"/> that keeps the results of
/// <see cref="IDWriteTextAnalyzer.AnalyzeScript"/> in a caller's buffer.</summary>
/// <remarks>Lives on the stack for the duration of one call.</remarks>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DirectWriteScriptSink
{
    private static readonly void** Vtbl = CreateVtbl();

    private void** vtbl;
    private ScriptRun* runs;
    private int capacity;

    /// <summary>Initializes a new instance of the <see cref="DirectWriteScriptSink"/> struct.</summary>
    /// <param name="runs">Receives the runs.</param>
    /// <param name="capacity">Capacity of <paramref name="runs"/>.</param>
    /// <remarks>One run per code unit is always enough.</remarks>
    public DirectWriteScriptSink(ScriptRun* runs, int capacity)
    {
        this.vtbl = Vtbl;
        this.runs = runs;
        this.capacity = capacity;
    }

    /// <summary>Gets the number of runs received.</summary>
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
        v[4] = (delegate* unmanaged<DirectWriteScriptSink*, uint, uint, void*, int>)&Ignore;
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
        if (self->Count < self->capacity)
            self->runs[self->Count++] = new() { Position = position, Length = length, Analysis = *analysis };
        return 0;
    }

    [UnmanagedCallersOnly]
    private static int SetBidiLevel(
        DirectWriteScriptSink* self, uint position, uint length, byte explicitLevel, byte resolvedLevel) => 0;

    // SetLineBreakpoints, SetNumberSubstitution.
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
