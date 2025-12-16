using System.Collections.Generic;
using System.Linq;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using Serilog;

namespace Dalamud.Game.ClientState.Conditions;

/// <summary>
/// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class Condition : IInternalDisposableService, ICondition
{
    /// <summary>
    /// Gets the current max number of conditions. You can get this just by looking at the condition sheet and how many rows it has.
    /// </summary>
    internal const int MaxConditionEntries = 112;

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly bool[] cache = new bool[MaxConditionEntries];

    private bool isDisposed;

    [ServiceManager.ServiceConstructor]
    private unsafe Condition()
    {
        this.Address = (nint)FFXIVClientStructs.FFXIV.Client.Game.Conditions.Instance();

        // Initialization
        for (var i = 0; i < MaxConditionEntries; i++)
            this.cache[i] = this[i];

        this.framework.Update += this.FrameworkUpdate;
    }

    /// <summary>Finalizes an instance of the <see cref="Condition" /> class.</summary>
    ~Condition() => this.Dispose(false);

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
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    /// <inheritdoc/>
    public IReadOnlySet<ConditionFlag> AsReadOnlySet()
    {
        var result = new HashSet<ConditionFlag>();

        for (var i = 0; i < MaxConditionEntries; i++)
        {
            if (this[i])
            {
                result.Add((ConditionFlag)i);
            }
        }

        return result;
    }

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

    /// <inheritdoc/>
    public bool AnyExcept(params ConditionFlag[] excluded)
    {
        return !this.AsReadOnlySet().Intersect(excluded).Any();
    }

    /// <inheritdoc/>
    public bool OnlyAny(params ConditionFlag[] other)
    {
        return !this.AsReadOnlySet().Except(other).Any();
    }

    /// <inheritdoc/>
    public bool EqualTo(params ConditionFlag[] other)
    {
        var resultSet = this.AsReadOnlySet();
        return resultSet.SetEquals(other);
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed)
            return;

        if (disposing)
        {
            this.framework.Update -= this.FrameworkUpdate;
        }

        this.isDisposed = true;
    }

    private void FrameworkUpdate(IFramework unused)
    {
        for (var i = 0; i < MaxConditionEntries; i++)
        {
            var value = this[i];

            if (value != this.cache[i])
            {
                this.cache[i] = value;

                foreach (var d in Delegate.EnumerateInvocationList(this.ConditionChange))
                {
                    try
                    {
                        d((ConditionFlag)i, value);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"While invoking {d.Method.Name}, an exception was thrown.");
                    }
                }
            }
        }
    }
}

/// <summary>
/// Plugin-scoped version of a Condition service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<ICondition>]
#pragma warning restore SA1015
internal class ConditionPluginScoped : IInternalDisposableService, ICondition
{
    [ServiceManager.ServiceDependency]
    private readonly Condition conditionService = Service<Condition>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionPluginScoped"/> class.
    /// </summary>
    internal ConditionPluginScoped()
    {
        this.conditionService.ConditionChange += this.ConditionChangedForward;
    }

    /// <inheritdoc/>
    public event ICondition.ConditionChangeDelegate? ConditionChange;

    /// <inheritdoc/>
    public int MaxEntries => this.conditionService.MaxEntries;

    /// <inheritdoc/>
    public IntPtr Address => this.conditionService.Address;

    /// <inheritdoc/>
    public bool this[int flag] => this.conditionService[flag];

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.conditionService.ConditionChange -= this.ConditionChangedForward;

        this.ConditionChange = null;
    }

    /// <inheritdoc/>
    public IReadOnlySet<ConditionFlag> AsReadOnlySet() => this.conditionService.AsReadOnlySet();

    /// <inheritdoc/>
    public bool Any() => this.conditionService.Any();

    /// <inheritdoc/>
    public bool Any(params ConditionFlag[] flags) => this.conditionService.Any(flags);

    /// <inheritdoc/>
    public bool AnyExcept(params ConditionFlag[] except) => this.conditionService.AnyExcept(except);

    /// <inheritdoc/>
    public bool OnlyAny(params ConditionFlag[] other) => this.conditionService.OnlyAny(other);

    /// <inheritdoc/>
    public bool EqualTo(params ConditionFlag[] other) => this.conditionService.EqualTo(other);

    private void ConditionChangedForward(ConditionFlag flag, bool value) => this.ConditionChange?.Invoke(flag, value);
}
