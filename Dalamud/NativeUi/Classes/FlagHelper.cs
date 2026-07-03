using System.Numerics;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Helper class for reading and setting bitflags.
/// </summary>
internal static class FlagHelper
{
    /// <summary>
    /// Read the flag value of bit index.
    /// </summary>
    /// <typeparam name="T">Flag field type.</typeparam>
    /// <param name="flagsField">This field.</param>
    /// <param name="flag">Flag index to read.</param>
    /// <returns>Flag value at index.</returns>
    public static bool ReadFlag<T>(ref T flagsField, int flag) where T : struct, IBinaryInteger<T>
        => (flagsField & (T.One << BitOperations.Log2((uint)flag))) != T.Zero;

    /// <summary>
    /// Read the flag value of bit index.
    /// </summary>
    /// <typeparam name="T">Flag field type.</typeparam>
    /// <param name="flagsField">This field.</param>
    /// <param name="flag">Flag index to set.</param>
    public static void SetFlag<T>(ref T flagsField, int flag) where T : struct, IBinaryInteger<T>
        => flagsField |= T.One << BitOperations.Log2((uint)flag);

    /// <summary>
    /// Clears the flag value of bit index.
    /// </summary>
    /// <typeparam name="T">Flag field type.</typeparam>
    /// <param name="flagsField">This field.</param>
    /// <param name="flag">Flag index to clear.</param>
    public static void ClearFlag<T>(ref T flagsField, int flag) where T : struct, IBinaryInteger<T>
        => flagsField &= ~(T.One << BitOperations.Log2((uint)flag));

    /// <summary>
    /// Sets of Clears the flag value of bit index with the value of enable.
    /// </summary>
    /// <typeparam name="T">Flag field type.</typeparam>
    /// <param name="flagsField">This field.</param>
    /// <param name="flag">Flag index to toggle.</param>
    /// <param name="enable">If the specified bit should be set.</param>
    public static void UpdateFlag<T>(ref T flagsField, int flag, bool enable) where T : struct, IBinaryInteger<T>
    {
        if (enable)
        {
            SetFlag(ref flagsField, flag);
        }
        else
        {
            ClearFlag(ref flagsField, flag);
        }
    }
}
