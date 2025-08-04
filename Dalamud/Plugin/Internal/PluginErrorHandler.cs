using System.Collections.Generic;
using System.Linq.Expressions;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Internal.Types;

using Serilog;

namespace Dalamud.Plugin.Internal;

/// <summary>
/// Service responsible for notifying the user when a plugin is creating errors.
/// </summary>
[ServiceManager.ScopedService]
internal class PluginErrorHandler : IServiceType
{
    private readonly LocalPlugin plugin;
    private readonly NotificationManager notificationManager;
    private readonly DalamudInterface di;

    private readonly Dictionary<Type, Delegate> invokerCache = new();

    private DateTime lastErrorTime = DateTime.MinValue;
    private IActiveNotification? activeNotification;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginErrorHandler"/> class.
    /// </summary>
    /// <param name="plugin">The plugin we are notifying for.</param>
    /// <param name="notificationManager">The notification manager.</param>
    /// <param name="di">The dalamud interface class.</param>
    [ServiceManager.ServiceConstructor]
    public PluginErrorHandler(LocalPlugin plugin, NotificationManager notificationManager, DalamudInterface di)
    {
        this.plugin = plugin;
        this.notificationManager = notificationManager;
        this.di = di;
    }

    /// <summary>
    /// Invoke the specified delegate and catch any exceptions that occur.
    /// Writes an error message to the log if an exception occurs and shows
    /// a notification if the plugin is a dev plugin and the user has enabled error notifications.
    /// </summary>
    /// <param name="eventHandler">The delegate to invoke.</param>
    /// <param name="hint">A hint to show about the origin of the exception if an error occurs.</param>
    /// <param name="args">Arguments to the event handler.</param>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    /// <returns>Whether invocation was successful/did not throw an exception.</returns>
    public bool InvokeAndCatch<TDelegate>(
        TDelegate? eventHandler,
        string hint,
        params object[] args)
        where TDelegate : Delegate
    {
        if (eventHandler == null)
            return true;

        try
        {
            var invoker = this.GetInvoker<TDelegate>();
            invoker(eventHandler, args);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"[{this.plugin.InternalName}] Exception in event handler {{EventHandlerName}}", hint);
            this.NotifyError();
            return false;
        }
    }

    /// <summary>
    /// Show a notification, if the plugin is a dev plugin and the user has enabled error notifications.
    /// This function has a cooldown built-in.
    /// </summary>
    public void NotifyError()
    {
        if (this.plugin is not LocalDevPlugin devPlugin)
            return;

        if (!devPlugin.NotifyForErrors)
            return;

        // If the notification is already active, we don't need to show it again.
        if (this.activeNotification is { DismissReason: null })
            return;

        var now = DateTime.UtcNow;
        if (now - this.lastErrorTime < TimeSpan.FromMinutes(2))
            return;

        this.lastErrorTime = now;

        var creatingErrorsText = $"{devPlugin.Name} is creating errors";
        var notification = new Notification()
        {
            Title = creatingErrorsText,
            Icon = INotificationIcon.From(FontAwesomeIcon.Bolt),
            Type = NotificationType.Error,
            InitialDuration = TimeSpan.FromSeconds(15),
            MinimizedText = creatingErrorsText,
            Content = $"The plugin '{devPlugin.Name}' is creating errors. Click 'Show console' to learn more.\n\n" +
                      $"You are seeing this because '{devPlugin.Name}' is a Dev Plugin.",
            RespectUiHidden = false,
        };

        this.activeNotification = this.notificationManager.AddNotification(notification);
        this.activeNotification.DrawActions += _ =>
        {
            if (ImGui.Button("Show console"u8))
            {
                this.di.OpenLogWindow(this.plugin.InternalName);
                this.activeNotification.DismissNow();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Show the console filtered to this plugin"u8);
            }

            ImGui.SameLine();

            if (ImGui.Button("Disable notifications"u8))
            {
                devPlugin.NotifyForErrors = false;
                this.activeNotification.DismissNow();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Disable error notifications for this plugin"u8);
            }
        };
    }

    private static Action<TDelegate, object[]> CreateInvoker<TDelegate>() where TDelegate : Delegate
    {
        var delegateType = typeof(TDelegate);
        var method = delegateType.GetMethod("Invoke");
        if (method == null)
            throw new InvalidOperationException($"Delegate {delegateType} does not have an Invoke method.");

        var parameters = method.GetParameters();

        // Create parameters for the lambda
        var delegateParam = Expression.Parameter(delegateType, "d");
        var argsParam = Expression.Parameter(typeof(object[]), "args");

        // Create expressions to convert array elements to parameter types
        var callArgs = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var arrayAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            callArgs[i] = Expression.Convert(arrayAccess, paramType);
        }

        // Create the delegate invocation expression
        var callExpr = Expression.Call(delegateParam, method, callArgs);

        // If return type is not void, discard the result
        Expression bodyExpr;
        if (method.ReturnType != typeof(void))
        {
            // Create a block that executes the call and then returns void
            bodyExpr = Expression.Block(
                Expression.Call(delegateParam, method, callArgs),
                Expression.Empty());
        }
        else
        {
            bodyExpr = callExpr;
        }

        // Compile and return the lambda
        var lambda = Expression.Lambda<Action<TDelegate, object[]>>(
            bodyExpr, delegateParam, argsParam);
        return lambda.Compile();
    }

    private Action<TDelegate, object[]> GetInvoker<TDelegate>() where TDelegate : Delegate
    {
        var delegateType = typeof(TDelegate);

        if (!this.invokerCache.TryGetValue(delegateType, out var cachedInvoker))
        {
            cachedInvoker = CreateInvoker<TDelegate>();
            this.invokerCache[delegateType] = cachedInvoker;
        }

        return (Action<TDelegate, object[]>)cachedInvoker;
    }
}
