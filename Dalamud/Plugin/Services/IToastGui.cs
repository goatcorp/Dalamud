using Dalamud.Game.Gui.Toast;

using Lumina.Text.ReadOnly;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class facilitates interacting with and creating native toast windows.
/// </summary>
public interface IToastGui : IDalamudService
{
    /// <summary>
    /// A delegate type used when a normal toast window appears.
    /// </summary>
    /// <param name="message">The message displayed.</param>
    /// <param name="options">Assorted toast options.</param>
    /// <param name="isHandled">Whether the toast has been handled or should be propagated.</param>
    public delegate void OnNormalToastDelegate(ref ReadOnlySeString message, ref ToastOptions options, ref bool isHandled);

    /// <summary>
    /// A delegate type used when a quest toast window appears.
    /// </summary>
    /// <param name="message">The message displayed.</param>
    /// <param name="options">Assorted toast options.</param>
    /// <param name="isHandled">Whether the toast has been handled or should be propagated.</param>
    public delegate void OnQuestToastDelegate(ref ReadOnlySeString message, ref QuestToastOptions options, ref bool isHandled);

    /// <summary>
    /// A delegate type used when an error toast window appears.
    /// </summary>
    /// <param name="message">The message displayed.</param>
    /// <param name="isHandled">Whether the toast has been handled or should be propagated.</param>
    public delegate void OnErrorToastDelegate(ref ReadOnlySeString message, ref bool isHandled);
    
    /// <summary>
    /// Event that will be fired when a toast is sent by the game or a plugin.
    /// </summary>
    public event OnNormalToastDelegate Toast;

    /// <summary>
    /// Event that will be fired when a quest toast is sent by the game or a plugin.
    /// </summary>
    public event OnQuestToastDelegate QuestToast;

    /// <summary>
    /// Event that will be fired when an error toast is sent by the game or a plugin.
    /// </summary>
    public event OnErrorToastDelegate ErrorToast;

    /// <summary>
    /// Show a toast message with the given content.
    /// </summary>
    /// <param name="message">The message to be shown.</param>
    /// <param name="options">Options for the toast.</param>
    public void ShowNormal(ReadOnlySeString message, ToastOptions? options = null);

    /// <summary>
    /// Show a quest toast message with the given content.
    /// </summary>
    /// <param name="message">The message to be shown.</param>
    /// <param name="options">Options for the toast.</param>
    public void ShowQuest(ReadOnlySeString message, QuestToastOptions? options = null);

    /// <summary>
    /// Show an error toast message with the given content.
    /// </summary>
    /// <param name="message">The message to be shown.</param>
    public void ShowError(ReadOnlySeString message);
}
