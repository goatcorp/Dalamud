using Dalamud.Interface.Windowing;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Class representing a Data Window Widget in its own window.
/// </summary>
internal class DataWidgetPopoutWindow : Window
{
    private readonly IDataWindowWidget widget;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DataWidgetPopoutWindow"/> class.
    /// </summary>
    /// <param name="widget">The widget to display.</param>
    public DataWidgetPopoutWindow(IDataWindowWidget widget)
        : base(widget.DisplayName)
    {
        this.widget = widget;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        if (this.widget.Ready)
        {
            this.widget.Draw();
        }
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        base.OnClose();
        
        Service<DalamudInterface>.Get().WindowSystem.RemoveWindow(this);
    }
}
