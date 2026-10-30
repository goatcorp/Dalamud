using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Utility.Internal;

/// <summary>Helper methods for throwing exceptions.</summary>
internal static class ThrowHelper
{
    /// <summary>Throws a <see cref="ArgumentException"/> with a specified <paramref name="message"/>.</summary>
    /// <param name="message">Message for the exception.</param>
    /// <exception cref="ArgumentException">Thrown by this method.</exception>
    [DoesNotReturn]
    public static void ThrowArgumentException(string message)
        => throw new ArgumentException(message);

    /// <summary>Throws a <see cref="ArgumentOutOfRangeException"/> with a specified <paramref name="message"/> for a specified <paramref name="paramName"/>.</summary>
    /// <param name="paramName">Parameter name.</param>
    /// <param name="message">Message for the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown by this method.</exception>
    [DoesNotReturn]
    public static void ThrowArgumentOutOfRangeException(string paramName, string message)
        => throw new ArgumentOutOfRangeException(paramName, message);

    /// <summary>Throws a <see cref="ArgumentOutOfRangeException"/> if the specified <paramref name="value"/> is less than <paramref name="comparand"/>.</summary>
    /// <typeparam name="T"><see cref="IComparable{T}"/> value type.</typeparam>
    /// <param name="paramName">Parameter name.</param>
    /// <param name="value">Value to compare from.</param>
    /// <param name="comparand">Value to compare with.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown by this method if <paramref name="value"/> is less than <paramref name="comparand"/>.</exception>
    public static void ThrowArgumentOutOfRangeExceptionIfLessThan<T>(string paramName, T value, T comparand) where T : IComparable<T>
        => ArgumentOutOfRangeException.ThrowIfLessThan(value, comparand);

    /// <summary>Throws a <see cref="ArgumentOutOfRangeException"/> if the specified <paramref name="value"/> is greater than or equal to <paramref name="comparand"/>.</summary>
    /// <typeparam name="T"><see cref="IComparable{T}"/> value type.</typeparam>
    /// <param name="paramName">Parameter name.</param>
    /// <param name="value">Value to compare from.</param>
    /// <param name="comparand">Value to compare with.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown by this method if <paramref name="value"/> is greater than or equal to<paramref name="comparand"/>.</exception>
    public static void ThrowArgumentOutOfRangeExceptionIfGreaterThanOrEqual<T>(string paramName, T value, T comparand) where T : IComparable<T>
        => ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, comparand);
}
