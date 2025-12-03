using System.Collections.Generic;

using Dalamud.Game.ClientState.Conditions;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Provides access to conditions (generally player state). You can check whether a player is in combat, mounted, etc.
/// </summary>
public interface ICondition : IDalamudService
{
    /// <summary>
    /// A delegate type used with the <see cref="ConditionChange"/> event.
    /// </summary>
    /// <param name="flag">The changed condition.</param>
    /// <param name="value">The value the condition is set to.</param>
    public delegate void ConditionChangeDelegate(ConditionFlag flag, bool value);
    
    /// <summary>
    /// Event that gets fired when a condition is set.
    /// Should only get fired for actual changes, so the previous value will always be !value.
    /// </summary>
    public event ConditionChangeDelegate? ConditionChange;
    
    /// <summary>
    /// Gets the current max number of conditions.
    /// </summary>
    public int MaxEntries { get; }
    
    /// <summary>
    /// Gets the condition array base pointer.
    /// </summary>
    public nint Address { get; }
    
    /// <summary>
    /// Check the value of a specific condition/state flag.
    /// </summary>
    /// <param name="flag">The condition flag to check.</param>
    public bool this[int flag] { get; }
    
    /// <inheritdoc cref="this[int]"/>
    public bool this[ConditionFlag flag] => this[(int)flag];
    
    /// <summary>
    /// Convert the conditions array to a set of all set condition flags.
    /// </summary>
    /// <returns>Returns a set.</returns>
    public IReadOnlySet<ConditionFlag> AsReadOnlySet();

    /// <summary>
    /// Check if any condition flags are set.
    /// </summary>
    /// <returns>Whether any single flag is set.</returns>
    public bool Any();

    /// <summary>
    /// Check if any provided condition flags are set.
    /// </summary>
    /// <returns>Whether any single provided flag is set.</returns>
    /// <param name="flags">The condition flags to check.</param>
    public bool Any(params ConditionFlag[] flags);

    /// <summary>
    /// Check that the specified condition flags are *not* present in the current conditions.
    /// </summary>
    /// <param name="except">The array of flags to check.</param>
    /// <returns>Returns false if any of the listed conditions are present, true otherwise.</returns>
    public bool AnyExcept(params ConditionFlag[] except);

    /// <summary>
    /// Check that *only* any of the condition flags specified are set.
    /// </summary>
    /// <param name="other">The array of flags to check.</param>
    /// <returns>Returns a bool.</returns>
    public bool OnlyAny(params ConditionFlag[] other);

    /// <summary>
    /// Check that *only* the specified flags are set. Unlike <see cref="OnlyAny"/>, this method requires that all the
    /// specified flags are set and no others are present.
    /// </summary>
    /// <param name="other">The array of flags to check.</param>
    /// <returns>Returns a bool.</returns>
    public bool EqualTo(params ConditionFlag[] other);
}
