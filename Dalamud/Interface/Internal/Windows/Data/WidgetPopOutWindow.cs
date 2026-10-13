using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// General purpose window for use with <see cref="DataWindow"/> to display <see cref="IDataWindowWidget"/>
/// widgets in their own popup windows that are not dependent on the <see cref="DataWindow"/> being open.
/// </summary>
internal class WidgetPopOutWindow : Window
{
    private readonly WindowSystem windowSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetPopOutWindow"/> class.
    /// </summary>
    /// <param name="windowSystem">Reference to window system.</param>
    /// <param name="widget">Reference to widget to display.</param>
    public WidgetPopOutWindow(WindowSystem windowSystem, IDataWindowWidget widget)
        : base($"{widget.DisplayName}##DataWindowPopOutWidget")
    {
        this.Size = new Vector2(400.0f, 300.0f);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.windowSystem = windowSystem;
        this.Widget = widget;

        this.windowSystem.AddWindow(this);
        this.IsOpen = true;
    }

    /// <summary>
    /// Gets the widget that this window is drawing.
    /// </summary>
    public IDataWindowWidget Widget { get; init; }

    /// <summary>
    /// Gets or sets an action that is invoked when the window closes.
    /// </summary>
    public Action<WidgetPopOutWindow>? OnCloseAction { get; set; }

    /// <inheritdoc/>
    public override void Draw()
    {
        this.Widget.Draw();
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        base.OnClose();
        this.windowSystem.RemoveWindow(this);

        this.OnCloseAction?.Invoke(this);
    }
}
