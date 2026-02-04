using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal.UiDebug.Browsing;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Interface.Internal.UiDebug.UiDebug;

namespace Dalamud.Interface.Internal.UiDebug;

/// <summary>
/// A popout window for a <see cref="ResNodeTree"/>.
/// </summary>
internal unsafe class NodePopoutWindow : Window, IDisposable
{
    private readonly ResNodeTree resNodeTree;

    private bool firstDraw = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodePopoutWindow"/> class.
    /// </summary>
    /// <param name="nodeTree">The node tree this window will show.</param>
    /// <param name="windowName">The name of the window.</param>
    public NodePopoutWindow(ResNodeTree nodeTree, string windowName)
        : base(windowName)
    {
        this.resNodeTree = nodeTree;

        var pos = ImGui.GetMousePos() + new Vector2(50, -50);
        var workSize = ImGui.GetMainViewport().WorkSize;
        var pos2 = new Vector2(Math.Min(workSize.X - 750, pos.X), Math.Min(workSize.Y - 250, pos.Y));

        this.Position = pos2;
        this.IsOpen = true;
        this.PositionCondition = ImGuiCond.Once;
        this.SizeCondition = ImGuiCond.Once;
        this.Size = new(700, 200);
        this.SizeConstraints = new() { MinimumSize = new Vector2(100, 100) };
    }

    private AddonTree AddonTree => this.resNodeTree.AddonTree;

    private AtkResNode* Node => this.resNodeTree.Node;

    /// <inheritdoc/>
    public override void Draw()
    {
        if (this.Node != null && this.AddonTree.ContainsNode(this.Node))
        {
            using var ch = ImRaii.Child($"{(nint)this.Node:X}popoutChild", Vector2.Zero, true);
            if (ch.Success)
            {
                ResNodeTree.GetOrCreate(this.Node, this.AddonTree).Print(null, this.firstDraw);
                this.firstDraw = false;
            }
        }
        else
        {
            Log.Warning($"Popout closed ({this.WindowName}); Node or Addon no longer exists.");
            this.IsOpen = false;
            this.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
