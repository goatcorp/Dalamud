using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Interface.Internal;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.DragDrop;

/// <summary>
/// A manager that keeps state of external windows drag and drop events,
/// and can be used to create ImGui drag and drop sources and targets for those external events.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal partial class DragDropManager : IDisposable, IDragDropManager, IServiceType
{
    private InterfaceManager? interfaceManager;
    private int lastDropFrame = -2;
    private int lastTooltipFrame = -1;

    [ServiceManager.ServiceConstructor]
    private DragDropManager()
    {
        Service<InterfaceManager.InterfaceManagerWithScene>.GetAsync().ContinueWith(task =>
        {
            this.interfaceManager = task.Result.Manager;
            this.Enable();
        });
    }

    /// <summary> Gets a value indicating whether external drag and drop is available at all. </summary>
    public bool ServiceAvailable { get; private set; }

    /// <summary> Gets a value indicating whether a valid external drag and drop is currently active and hovering over any FFXIV-related viewport. </summary>
    public bool IsDragging { get; private set; }

    /// <summary> Gets a value indicating whether there are any files or directories currently being dragged. </summary>
    public bool HasPaths { get; private set; }

    /// <summary> Gets the list of file paths currently being dragged from an external application over any FFXIV-related viewport. </summary>
    public IReadOnlyList<string> Files { get; private set; } = Array.Empty<string>();

    /// <summary> Gets a set of all extensions available in the paths currently being dragged from an external application over any FFXIV-related viewport. </summary>
    public IReadOnlySet<string> Extensions { get; private set; } = new HashSet<string>();

    /// <summary> Gets the list of directory paths currently being dragged from an external application over any FFXIV-related viewport. </summary>
    public IReadOnlyList<string> Directories { get; private set; } = Array.Empty<string>();

    /// <summary> Enable external drag and drop. </summary>
    public void Enable()
    {
        if (this.ServiceAvailable || this.interfaceManager == null)
        {
            return;
        }

        try
        {
            var ret2 = DragDropInterop.RegisterDragDrop(this.interfaceManager.WindowHandlePtr, this);
            Log.Information($"[DragDrop] Registered window {this.interfaceManager.WindowHandlePtr} for external drag and drop operations. ({ret2})");
            Marshal.ThrowExceptionForHR(ret2);
            this.ServiceAvailable = true;
        }
        catch (Exception ex)
        {
            Log.Error($"Could not create windows drag and drop utility:\n{ex}");
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
            DragDropInterop.RevokeDragDrop(this.interfaceManager!.WindowHandlePtr);
            Log.Information($"[DragDrop] Disabled external drag and drop operations for window {this.interfaceManager.WindowHandlePtr}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Could not disable windows drag and drop utility:\n{ex}");
        }

        this.ServiceAvailable = false;
    }

    /// <inheritdoc cref="Disable"/>
    public void Dispose()
        => this.Disable();

    /// <inheritdoc cref="IDragDropManager.CreateImGuiSource(string, Func{IDragDropManager, bool}, Func{IDragDropManager, bool})"/>
    public void CreateImGuiSource(string label, Func<IDragDropManager, bool> validityCheck, Func<IDragDropManager, bool> tooltipBuilder)
    {
        if (!this.HasPaths && !this.IsDropping())
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
        if (!this.IsDragging || !ImGui.BeginDragDropTarget())
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
