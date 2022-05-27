using System;
using System.Linq;

using Serilog;

namespace Dalamud.Fixes;

/// <summary>
/// Class responsible for executing game fixes.
/// </summary>
internal class GameFixes : IDisposable
{
    private readonly IGameFix[] fixes =
    {
        new WndProcNullRefFix(),
    };

    /// <summary>
    /// Apply all game fixes.
    /// </summary>
    public void Apply()
    {
        foreach (var gameFix in this.fixes)
        {
            try
            {
                gameFix.Apply();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not apply game fix: {FixName}", gameFix.GetType().FullName);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var disposable in this.fixes.OfType<IDisposable>())
        {
            disposable.Dispose();
        }
    }
}
