using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Internal.UiDebug.Browsing;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace Dalamud.Interface.Internal.UiDebug;

/// <summary>
/// A popout window for an <see cref="AddonTree"/>.
/// </summary>
internal class AddonPopoutWindow : Window, IDisposable
{
    private readonly AddonTree addonTree;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddonPopoutWindow"/> class.
    /// </summary>
    /// <param name="tree">The AddonTree this popout will show.</param>
    /// <param name="name">the window's name.</param>
    public AddonPopoutWindow(AddonTree tree, string name)
        : base(name)
    {
        this.addonTree = tree;
        this.PositionCondition = ImGuiCond.Once;

        var pos = ImGui.GetMousePos() + new Vector2(50, -50);
        var workSize = ImGui.GetMainViewport().WorkSize;
        var pos2 = new Vector2(Math.Min(workSize.X - 750, pos.X), Math.Min(workSize.Y - 250, pos.Y));

        this.Position = pos2;
        this.SizeCondition = ImGuiCond.Once;
        this.Size = new(700, 200);
        this.IsOpen = true;
        this.SizeConstraints = new() { MinimumSize = new(100, 100) };
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        using var ch = ImRaii.Child($"{this.WindowName}child", Vector2.Zero, true);
        if (ch.Success)
        {
            this.addonTree.Draw();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
