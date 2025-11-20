using System.Collections.Generic;

using Dalamud.Game;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Gui.NamePlate;
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
    /// of a thrown Exception inside an invocation.
    /// </summary>
    /// <param name="eh">The EventHandler in question.</param>
    /// <param name="sender">Default sender for Invoke equivalent.</param>
    /// <param name="e">Default EventArgs for Invoke equivalent.</param>
    public static void InvokeSafely(this EventHandler? eh, object sender, EventArgs e)
    {
        foreach (var handler in Delegate.EnumerateInvocationList(eh))
        {
            try
            {
                handler(sender, e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", handler.Method);
            }
        }
    }

    /// <summary>
    /// Replacement for Invoke() on generic EventHandlers to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside an invocation.
    /// </summary>
    /// <param name="eh">The EventHandler in question.</param>
    /// <param name="sender">Default sender for Invoke equivalent.</param>
    /// <param name="e">Default EventArgs for Invoke equivalent.</param>
    /// <typeparam name="T">Type of EventArgs.</typeparam>
    public static void InvokeSafely<T>(this EventHandler<T>? eh, object sender, T e)
    {
        foreach (var handler in Delegate.EnumerateInvocationList(eh))
        {
            try
            {
                handler(sender, e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", handler.Method);
            }
        }
    }

    /// <summary>
    /// Replacement for Invoke() on event Actions to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside an invocation.
    /// </summary>
    /// <param name="act">The Action in question.</param>
    public static void InvokeSafely(this Action? act)
    {
        foreach (var action in Delegate.EnumerateInvocationList(act))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
    }

    /// <inheritdoc cref="InvokeSafely(Action)"/>
    public static void InvokeSafely<T>(this Action<T>? act, T argument)
    {
        foreach (var action in Delegate.EnumerateInvocationList(act))
        {
            try
            {
                action(argument);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
    }

    /// <inheritdoc cref="InvokeSafely(Action)"/>
    public static void InvokeSafely<T1, T2>(this Action<T1, T2>? act, T1 arg1, T2 arg2)
    {
        foreach (var action in Delegate.EnumerateInvocationList(act))
        {
            try
            {
                action(arg1, arg2);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
    }

    /// <summary>
    /// Replacement for Invoke() on OnUpdateDelegate to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside an invocation.
    /// </summary>
    /// <param name="updateDelegate">The OnUpdateDelegate in question.</param>
    /// <param name="framework">Framework to be passed on to OnUpdateDelegate.</param>
    public static void InvokeSafely(this IFramework.OnUpdateDelegate? updateDelegate, Framework framework)
    {
        foreach (var action in Delegate.EnumerateInvocationList(updateDelegate))
        {
            try
            {
                action(framework);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
    }

    /// <summary>
    /// Replacement for Invoke() on OnMenuOpenedDelegate to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside an invocation.
    /// </summary>
    /// <param name="openedDelegate">The OnMenuOpenedDelegate in question.</param>
    /// <param name="argument">Templated argument for Action.</param>
    public static void InvokeSafely(this IContextMenu.OnMenuOpenedDelegate? openedDelegate, MenuOpenedArgs argument)
    {
        foreach (var action in Delegate.EnumerateInvocationList(openedDelegate))
        {
            try
            {
                action(argument);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
    }

    /// <summary>
    /// Replacement for Invoke() on OnMenuOpenedDelegate to catch exceptions that stop event propagation in case
    /// of a thrown Exception inside an invocation.
    /// </summary>
    /// <param name="updatedDelegate">The OnMenuOpenedDelegate in question.</param>
    /// <param name="context">An object containing information about the pending data update.</param>
    /// <param name="handlers>">A list of handlers used for updating nameplate data.</param>
    public static void InvokeSafely(
        this INamePlateGui.OnPlateUpdateDelegate? updatedDelegate,
        INamePlateUpdateContext context,
        IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var action in Delegate.EnumerateInvocationList(updatedDelegate))
        {
            try
            {
                action(context, handlers);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", action.Method);
            }
        }
    }
}
