using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Common.Util;

public static class EnvironmentUtils
{
    /// <summary>
    /// Attempts to get an environment variable using the Try pattern.
    /// </summary>
    /// <param name="variableName">The env var to get.</param>
    /// <param name="value">An output containing the env var, if present.</param>
    /// <returns>A boolean indicating whether the var was present.</returns>
    public static bool TryGetEnvironmentVariable(string variableName, [NotNullWhen(true)] out string? value)
    {
        value = Environment.GetEnvironmentVariable(variableName);
        return value != null;
    }
}
