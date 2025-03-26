using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Interface.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;

using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.DragDrop;

/// <summary>
/// A manager that keeps state of external windows drag and drop events,
/// and can be used to create ImGui drag and drop sources and targets for those external events.
/// </summary>
[PluginInterface]
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IDragDropManager>]
#pragma warning restore SA1015
internal partial class DragDropManager : IInternalDisposableService, IDragDropManager
{
    private nint windowHandlePtr = nint.Zero;

    private int lastDropFrame = -2;
    private int lastTooltipFrame = -1;

    [ServiceManager.ServiceConstructor]
    private DragDropManager()
    {
        Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync()
            .ContinueWith(t =>
             {
                 this.windowHandlePtr = t.Result.Manager.WindowHandlePtr;
                 this.Enable();
             });
    }

    /// <summary> Gets a value indicating whether external drag and drop is available at all. </summary>
    public bool ServiceAvailable { get; private set; }

    /// <summary> Gets a value indicating whether a valid external drag and drop is currently active and hovering over any FFXIV-related viewport. </summary>
    public bool IsDragging { get; private set; }

    /// <summary> Gets a value indicating whether there are any files or directories currently being dragged, or stored from the last drop. </summary>
    public bool HasPaths
        => this.Files.Count + this.Directories.Count > 0;

    /// <summary> Gets the list of file paths currently being dragged from an external application over any FFXIV-related viewport, or stored from the last drop. </summary>
    public IReadOnlyList<string> Files { get; private set; } = Array.Empty<string>();

    /// <summary> Gets a set of all extensions available in the paths currently being dragged from an external application over any FFXIV-related viewport or stored from the last drop. </summary>
    public IReadOnlySet<string> Extensions { get; private set; } = new HashSet<string>();

    /// <summary> Gets the list of directory paths currently being dragged from an external application over any FFXIV-related viewport or stored from the last drop. </summary>
    public IReadOnlyList<string> Directories { get; private set; } = Array.Empty<string>();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Disable();

    /// <summary> Enable external drag and drop. </summary>
    public void Enable()
    {
        if (this.ServiceAvailable || this.windowHandlePtr == nint.Zero)
        {
            return;
        }

        try
        {
            var ret = DragDropInterop.RegisterDragDrop(this.windowHandlePtr, this);
            Log.Information($"[DragDrop] Registered window 0x{this.windowHandlePtr:X} for external drag and drop operations. ({ret})");
            Marshal.ThrowExceptionForHR(ret);
            this.ServiceAvailable = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Could not create windows drag and drop utility for window 0x{this.windowHandlePtr:X}:\n{ex}");
        }
    }

    /// <summary> Disable external drag and drop. </summary>
    public void Disable()
    {
        if (!this.ServiceAvailable)
        {
            return;
        }

        try
        {
            var ret = DragDropInterop.RevokeDragDrop(this.windowHandlePtr);
            Log.Information($"[DragDrop] Disabled external drag and drop operations for window 0x{this.windowHandlePtr:X}. ({ret})");
            Marshal.ThrowExceptionForHR(ret);
        }
        catch (Exception ex)
        {
            Log.Error($"Could not disable windows drag and drop utility for window 0x{this.windowHandlePtr:X}:\n{ex}");
        }

        this.ServiceAvailable = false;
    }

    /// <inheritdoc cref="IDragDropManager.CreateImGuiSource(string, Func{IDragDropManager, bool}, Func{IDragDropManager, bool})"/>
    public void CreateImGuiSource(string label, Func<IDragDropManager, bool> validityCheck, Func<IDragDropManager, bool> tooltipBuilder)
    {
        if (!this.IsDragging && !this.IsDropping())
        {
            return;
        }

        if (!validityCheck(this) || !ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceExtern))
        {
            return;
        }

        ImGui.SetDragDropPayload(label, nint.Zero, 0);
        if (this.CheckTooltipFrame(out var frame) && tooltipBuilder(this))
        {
            this.lastTooltipFrame = frame;
        }

        ImGui.EndDragDropSource();
    }

    /// <inheritdoc cref="IDragDropManager.CreateImGuiTarget"/>
    public bool CreateImGuiTarget(string label, out IReadOnlyList<string> files, out IReadOnlyList<string> directories)
    {
        files = Array.Empty<string>();
        directories = Array.Empty<string>();
        if (!this.HasPaths || !ImGui.BeginDragDropTarget())
        {
            return false;
        }

        unsafe
        {
            if (ImGui.AcceptDragDropPayload(label, ImGuiDragDropFlags.AcceptBeforeDelivery).NativePtr != null && this.IsDropping())
            {
                this.lastDropFrame = -2;
                files = this.Files;
                directories = this.Directories;
                return true;
            }
        }

        ImGui.EndDragDropTarget();
        return false;
    }

    private bool CheckTooltipFrame(out int frame)
    {
        frame = ImGui.GetFrameCount();
        return this.lastTooltipFrame < frame;
    }

    private bool IsDropping()
    {
        var frame = ImGui.GetFrameCount();
        return this.lastDropFrame == frame || this.lastDropFrame == frame - 1;
    }
}
