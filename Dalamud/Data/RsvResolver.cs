using System.Collections.Generic;

using Dalamud.Hooking;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using Lumina.Text.ReadOnly;

namespace Dalamud.Data;

/// <summary>
/// Provides functionality for resolving RSV strings.
/// </summary>
internal sealed unsafe class RsvResolver : IDisposable
{
    private static readonly ModuleLog Log = ModuleLog.Create<RsvResolver>();

    private readonly Hook<LayoutWorld.Delegates.AddRsvString> addRsvStringHook;

    /// <summary>
    /// Initializes a new instance of the <see cref="RsvResolver"/> class.
    /// </summary>
    public RsvResolver()
    {
        this.addRsvStringHook = Hook<LayoutWorld.Delegates.AddRsvString>.FromAddress((nint)LayoutWorld.MemberFunctionPointers.AddRsvString, this.AddRsvStringDetour);

        this.addRsvStringHook.Enable();
    }

    private Dictionary<ReadOnlySeString, ReadOnlySeString> Lookup { get; } = [];

    /// <summary>Attemps to resolve an RSV string.</summary>
    /// <inheritdoc cref="Lumina.Excel.ExcelModule.ResolveRsvDelegate"/>
    public bool TryResolve(ReadOnlySeString rsvString, out ReadOnlySeString resolvedString) =>
        this.Lookup.TryGetValue(rsvString, out resolvedString);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.addRsvStringHook.Dispose();
    }

    private bool AddRsvStringDetour(LayoutWorld* @this, byte* rsvString, byte* resolvedString, nuint resolvedStringSize)
    {
        var rsv = new ReadOnlySeString(MemoryHelper.ReadRawNullTerminated((nint)rsvString));
        var resolved = new ReadOnlySeString(new ReadOnlySpan<byte>(resolvedString, (int)resolvedStringSize).ToArray());
        Log.Debug($"Resolving RSV \"{rsv}\" to \"{resolved}\".");
        this.Lookup[rsv] = resolved;
        return this.addRsvStringHook.Original(@this, rsvString, resolvedString, resolvedStringSize);
    }
}
