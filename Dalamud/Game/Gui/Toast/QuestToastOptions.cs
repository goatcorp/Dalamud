namespace Dalamud.Game.Gui.Toast;

/// <summary>
/// This class represents options that can be used with the <see cref="ToastGui"/> class for the quest toast variant.
/// </summary>
public sealed class QuestToastOptions
{
    /// <summary>
    /// Gets or sets the position of the toast on the screen.
    /// </summary>
    public QuestToastPosition Position { get; set; } = QuestToastPosition.Centre;

    /// <summary>
    /// Gets or sets the ID of the icon that will appear in the toast.
    ///
    /// This may be 0 for no icon.
    /// </summary>
    public uint IconId { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether the toast will show a checkmark after appearing.
    /// </summary>
    public bool DisplayCheckmark { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the toast will play a completion sound.
    ///
    /// This only works if <see cref="IconId"/> is non-zero or <see cref="DisplayCheckmark"/> is true.
    /// </summary>
    public bool PlaySound { get; set; } = false;
}
