namespace Dalamud.Game.Text.SeStringHandling;

/// <summary>
/// An interface binding for a payload that can provide readable Text.
/// </summary>
public interface ITextProvider
{
    /// <summary>
    /// Gets the readable text.
    /// </summary>
    string Text { get; }
}
