namespace Dalamud.Game.Gui.Toast;

/// <summary>
/// This class represents options that can be used with the <see cref="ToastGui"/> class.
/// </summary>
public sealed class ToastOptions
{
    /// <summary>
    /// Gets or sets the position of the toast on the screen.
    /// </summary>
    public ToastPosition Position { get; set; } = ToastPosition.Bottom;

    /// <summary>
    /// Gets or sets the speed of the toast.
    /// </summary>
    public ToastSpeed Speed { get; set; } = ToastSpeed.Slow;
}
