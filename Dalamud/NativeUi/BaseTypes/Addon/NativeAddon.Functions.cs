using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Addon;

/// <summary>
/// .
/// </summary>
internal partial class NativeAddon
{
    /// <summary>
    /// Initializes and Opens this instance of Addon.
    /// </summary>
    /// <remarks>
    /// Must be invoked from the games main thread.
    /// </remarks>
    public unsafe void Open()
    {
        ThreadSafety.AssertMainThread();

        if (this.InternalAddon is null)
        {
            this.AllocateAddon();

            if (this.InternalAddon is not null)
            {
                this.InternalAddon->Open((uint)this.DepthLayer - 1);
            }
        }
        else
        {
            this.Log.Verbose("[{InternalName}] Already open, skipping call.", this.InternalName);
        }
    }

    /// <summary>
    /// Closes addon, this will cause it to fully close and deallocate.
    /// This NativeAddon object will remain valid, you can call Open to re-allocate this addon.
    /// </summary>
    /// <remarks>
    /// Must be called from the games main thread.
    /// </remarks>
    public unsafe void Close()
    {
        ThreadSafety.AssertMainThread();
        if (this.InternalAddon is null) return;

        if (this.InternalAddon is null)
        {
            this.Log.Verbose("[{InternalName}] Already closed, skipping call.", this.InternalName);
            return;
        }

        this.InternalAddon->Close(false);
    }

    /// <summary>
    /// Closes addon, but awaits for it to be fully unloaded before reporting completed.
    /// </summary>
    /// <remarks>
    /// <em>Must not be called from the main thread</em>
    /// </remarks>
    /// <returns>A task representing the close operation for this addon.</returns>
    public async Task CloseAsync()
    {
        if (Service<Framework>.Get().IsFrameworkUnloading) return;
        ThreadSafety.AssertNotMainThread();

        unsafe
        {
            if (this.InternalAddon is null)
            {
                this.Log.Verbose("[{InternalName}] Already closed, skipping call.", this.InternalName);
                return;
            }
        }

        await Service<Framework>.Get().Run(this.Close);

        var gameGuiService = Service<GameGui>.Get();
        while (!gameGuiService.GetAddonByName(this.InternalName).IsNull)
        {
            await Task.Delay(16);
        }
    }

    /// <summary>
    /// Toggles the addon from Open to Closed and vice versa.
    /// </summary>
    public void Toggle()
    {
        if (this.IsOpen)
        {
            this.Close();
        }
        else
        {
            this.Open();
        }
    }

    /// <summary>
    /// Attaches a collection nodes to this addon's root node.
    /// </summary>
    /// <param name="nodes">Nodes to add to this addon.</param>
    public void AddNode(ICollection<NodeBase> nodes)
    {
        foreach (var node in nodes)
        {
            this.AddNode(node);
        }
    }

    /// <summary>
    /// Attaches a specific node to this addon's root node.
    /// </summary>
    /// <param name="node">Node to add to this addon.</param>
    public void AddNode(NodeBase? node)
        => node?.AttachNode(this);

    /// <summary>
    /// Sets the addon's position.
    /// </summary>
    /// <remarks>
    /// Can only be used on an already open addon.
    /// </remarks>
    /// <param name="windowPosition">The window position to set.</param>
    public unsafe void SetWindowPosition(Vector2 windowPosition)
    {
        if (this.InternalAddon is null) return;
        this.InternalAddon->SetPosition((short)windowPosition.X, (short)windowPosition.Y);
    }

    /// <summary>
    /// Sets the addon's size via Vector2.
    /// </summary>
    /// <param name="windowSize">The window size to set.</param>
    public unsafe void SetWindowSize(Vector2 windowSize)
    {
        if (this.InternalAddon is null) return;

        this.Size = windowSize;
        this.InternalAddon->SetSize((ushort)this.Size.X, (ushort)this.Size.Y);

        this.WindowNode?.Size = this.Size;
    }

    /// <summary>
    /// Sets the windows size via floats.
    /// </summary>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public void SetWindowSize(float width, float height)
        => this.SetWindowSize(new Vector2(width, height));

    private unsafe Vector2 GetScreenClampedPosition(Vector2 position)
    {
        if (!this.OpenInBounds) return position;

        var screenSize = (Vector2)AtkStage.Instance()->ScreenSize;
        var clampedX = Math.Clamp(position.X, 0.0f, screenSize.X - this.Size.X);
        var clampedY = Math.Clamp(position.Y, 0.0f, screenSize.Y - this.Size.Y);
        return new Vector2(clampedX, clampedY);
    }
}
