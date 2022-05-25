using System;

namespace Dalamud.Fixes;

/// <summary>
/// Base interface to be implemented by game fixes.
/// </summary>
internal interface IGameFix
{
    /// <summary>
    /// Apply the patch to the game.
    /// </summary>
    public void Apply();
}
