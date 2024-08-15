using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Excel.Rsv;
using Lumina.Text.ReadOnly;

namespace Dalamud.Data;

/// <summary>
/// An implementation of <see cref="IRsvProvider"/> that persistently stores resolved RSV strings.
/// </summary>
internal sealed unsafe class RsvProvider : IRsvProvider, IDisposable
{
    private static readonly ModuleLog Log = new("RsvProvider");

    private readonly Hook<LayoutWorld.Delegates.AddRsvString> addRsvStringHook;

    /// <summary>
    /// Initializes a new instance of the <see cref="RsvProvider"/> class.
    /// </summary>
    public RsvProvider()
    {
        this.addRsvStringHook = Hook<LayoutWorld.Delegates.AddRsvString>.FromAddress((nint)LayoutWorld.MemberFunctionPointers.AddRsvString, this.AddRsvStringDetour);

        this.addRsvStringHook.Enable();
    }

    private Dictionary<ReadOnlySeString, ReadOnlySeString> Lookup { get; } = [];

    /// <inheritdoc/>
    public bool CanResolve(ReadOnlySeString rsvString) =>
        this.Lookup.ContainsKey(rsvString);

    /// <inheritdoc/>
    public ReadOnlySeString Resolve(ReadOnlySeString rsvString) =>
        this.Lookup[rsvString];

    /// <inheritdoc/>
    public ReadOnlySeString ResolveOrSelf(ReadOnlySeString rsvString) =>
        this.Lookup.GetValueOrDefault(rsvString, rsvString);

    /// <inheritdoc/>
    public ReadOnlySeString? TryResolve(ReadOnlySeString rsvString) =>
        this.Lookup.TryGetValue(rsvString, out var resolvedString) ?
            resolvedString :
            rsvString;

    /// <inheritdoc/>
    public void Dispose()
    {
        this.addRsvStringHook.Dispose();
    }

    private bool AddRsvStringDetour(LayoutWorld* @this, byte* rsvString, byte* resolvedString, nint resolvedStringSize)
    {
        var rsv = new ReadOnlySeString(MemoryHelper.ReadRawNullTerminated((nint)rsvString));
        var resolved = new ReadOnlySeString(new ReadOnlySpan<byte>(resolvedString, (int)resolvedStringSize).ToArray());
        Log.Verbose($"Resolving RSV \"{rsv}\" to \"{resolved}\".");
        this.Lookup[rsv] = resolved;
        return this.addRsvStringHook.Original(@this, rsvString, resolvedString, resolvedStringSize);
    }
}
