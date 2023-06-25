using System;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Serilog;

namespace Dalamud.Game.ClientState.Conditions;

/// <summary>
/// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<ICondition>]
#pragma warning restore SA1015
public sealed partial class Condition : IServiceType, ICondition
{
    /// <summary>
    /// Gets the current max number of conditions. You can get this just by looking at the condition sheet and how many rows it has.
    /// </summary>
    internal const int MaxConditionEntries = 104;
    
    private readonly bool[] cache = new bool[MaxConditionEntries];

    [ServiceManager.ServiceConstructor]
    private Condition(ClientState clientState)
    {
        var resolver = clientState.AddressResolver;
        this.Address = resolver.ConditionFlags;
    }
    
    /// <inheritdoc/>
    public event ICondition.ConditionChangeDelegate? ConditionChange;

    /// <inheritdoc/>
    public int MaxEntries => MaxConditionEntries;

    /// <inheritdoc/>
    public IntPtr Address { get; private set; }

    /// <inheritdoc/>
    public unsafe bool this[int flag]
    {
        get
        {
            if (flag < 0 || flag >= MaxConditionEntries)
                return false;

            return *(bool*)(this.Address + flag);
        }
    }

    /// <inheritdoc/>
    public bool this[ConditionFlag flag]
        => this[(int)flag];

    /// <inheritdoc/>
    public bool Any()
    {
        for (var i = 0; i < MaxConditionEntries; i++)
        {
            var cond = this[i];

            if (cond)
                return true;
        }

        return false;
    }
    
    /// <inheritdoc/>
    public bool Any(params ConditionFlag[] flags)
    {
        foreach (var flag in flags)
        {
            // this[i] performs range checking, so no need to check here
            if (this[flag]) 
            {
                return true;
            }
        }

        return false;
    }

    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction(Framework framework)
    {
        // Initialization
        for (var i = 0; i < MaxConditionEntries; i++)
            this.cache[i] = this[i];

        framework.Update += this.FrameworkUpdate;
    }

    private void FrameworkUpdate(Framework framework)
    {
        for (var i = 0; i < MaxConditionEntries; i++)
        {
            var value = this[i];

            if (value != this.cache[i])
            {
                this.cache[i] = value;

                try
                {
                    this.ConditionChange?.Invoke((ConditionFlag)i, value);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"While invoking {nameof(this.ConditionChange)}, an exception was thrown.");
                }
            }
        }
    }
}

/// <summary>
/// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
/// </summary>
public sealed partial class Condition : IDisposable
{
    private bool isDisposed;

    /// <summary>
    /// Finalizes an instance of the <see cref="Condition" /> class.
    /// </summary>
    ~Condition()
    {
        this.Dispose(false);
    }

    /// <summary>
    /// Disposes this instance, alongside its hooks.
    /// </summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        this.Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed)
            return;

        if (disposing)
        {
            Service<Framework>.Get().Update -= this.FrameworkUpdate;
        }

        this.isDisposed = true;
    }
}
