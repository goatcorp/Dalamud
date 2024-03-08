using System.Linq;

using Dalamud.Game;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using Serilog;

namespace Dalamud.Utility;

/// <summary>
/// Extensions for Events.
/// </summary>
internal static class EventHandlerExtensions
{
    /// <summary>
    /// Replacement for Invoke() on EventHandlers to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside of an invocation.
    /// </summary>
    /// <param name="eh">The EventHandler in question.</param>
    /// <param name="sender">Default sender for Invoke equivalent.</param>
    /// <param name="e">Default EventArgs for Invoke equivalent.</param>
    public static void InvokeSafely(this EventHandler? eh, object sender, EventArgs e)
    {
        if (eh == null)
            return;

        foreach (var handler in eh.GetInvocationList().Cast<EventHandler>())
        {
            HandleInvoke(() => handler(sender, e));
        }
    }

    /// <summary>
    /// Replacement for Invoke() on generic EventHandlers to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside of an invocation.
    /// </summary>
    /// <param name="eh">The EventHandler in question.</param>
    /// <param name="sender">Default sender for Invoke equivalent.</param>
    /// <param name="e">Default EventArgs for Invoke equivalent.</param>
    /// <typeparam name="T">Type of EventArgs.</typeparam>
    public static void InvokeSafely<T>(this EventHandler<T>? eh, object sender, T e)
    {
        if (eh == null)
            return;

        foreach (var handler in eh.GetInvocationList().Cast<EventHandler<T>>())
        {
            HandleInvoke(() => handler(sender, e));
        }
    }

    /// <summary>
    /// Replacement for Invoke() on event Actions to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside of an invocation.
    /// </summary>
    /// <param name="act">The Action in question.</param>
    public static void InvokeSafely(this Action? act)
    {
        if (act == null)
            return;

        foreach (var action in act.GetInvocationList().Cast<Action>())
        {
            HandleInvoke(action);
        }
    }

    /// <summary>
    /// Replacement for Invoke() on event Actions to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside of an invocation.
    /// </summary>
    /// <param name="act">The Action in question.</param>
    /// <param name="argument">Templated argument for Action.</param>
    /// <typeparam name="T">Type of Action args.</typeparam>
    public static void InvokeSafely<T>(this Action<T>? act, T argument)
    {
        if (act == null)
            return;

        foreach (var action in act.GetInvocationList().Cast<Action<T>>())
        {
            HandleInvoke(action, argument);
        }
    }

    /// <summary>
    /// Replacement for Invoke() on OnUpdateDelegate to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside of an invocation.
    /// </summary>
    /// <param name="updateDelegate">The OnUpdateDelegate in question.</param>
    /// <param name="framework">Framework to be passed on to OnUpdateDelegate.</param>
    public static void InvokeSafely(this IFramework.OnUpdateDelegate? updateDelegate, Framework framework)
    {
        if (updateDelegate == null)
            return;

        foreach (var action in updateDelegate.GetInvocationList().Cast<IFramework.OnUpdateDelegate>())
        {
            HandleInvoke(() => action(framework));
        }
    }

    /// <summary>
    /// Replacement for Invoke() on OnMenuOpenedDelegate to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside of an invocation.
    /// </summary>
    /// <param name="openedDelegate">The OnMenuOpenedDelegate in question.</param>
    /// <param name="argument">Templated argument for Action.</param>
    public static void InvokeSafely(this IContextMenu.OnMenuOpenedDelegate? openedDelegate, MenuOpenedArgs argument)
    {
        if (openedDelegate == null)
            return;

        foreach (var action in openedDelegate.GetInvocationList().Cast<IContextMenu.OnMenuOpenedDelegate>())
        {
            HandleInvoke(() => action(argument));
        }
    }

    private static void HandleInvoke(Action act)
    {
        try
        {
            act();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception during raise of {handler}", act.Method);
        }
    }

    private static void HandleInvoke<T>(Action<T> act, T argument)
    {
        try
        {
            act(argument);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception during raise of {handler}", act.Method);
        }
    }
}
