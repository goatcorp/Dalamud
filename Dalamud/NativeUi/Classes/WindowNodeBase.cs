using System.Numerics;

using Dalamud.NativeUi.BaseTypes.Component;
using Dalamud.NativeUi.Nodes;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Abstract base class implementation of a WindowNode and its associated component.
/// </summary>
internal abstract unsafe class WindowNodeBase : ComponentNode<AtkComponentWindow, AtkUldComponentDataWindow>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNodeBase"/> class.
    /// </summary>
    protected WindowNodeBase()
        => this.SetInternalComponentType(ComponentType.Window);

    /// <summary>
    /// Gets the area of the node minus the header size.
    /// </summary>
    public abstract Vector2 ContentSize { get; }

    /// <summary>
    /// Gets the position where the content starts, below the header node.
    /// </summary>
    public abstract Vector2 ContentStartPosition { get; }

    /// <summary>
    /// Gets the height of the header.
    /// </summary>
    public abstract float HeaderHeight { get; }

    /// <summary>
    /// Gets the node that should be focused.
    /// </summary>
    public abstract ResNode WindowHeaderFocusNode { get; }

    /// <summary>
    /// Sets the nodes title and subtitle.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="subtitle">Window subtitle.</param>
    public virtual void SetTitle(string title, string? subtitle = null)
        => Component->SetTitle(title, subtitle ?? string.Empty);
}
