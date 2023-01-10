using Dalamud.Game;

namespace Dalamud.Utility.Signatures;

/// <summary>
/// The type of scan to perform with a signature.
/// </summary>
public enum ScanType
{
    /// <summary>
    /// Scan the text section of the executable. Uses
    /// <see cref="SigScanner.TryScanText"/>.
    /// </summary>
    Text,

    /// <summary>
    /// Scans the text section of the executable in order to find a data section
    /// address. Uses <see cref="SigScanner.TryGetStaticAddressFromSig"/>.
    /// </summary>
    StaticAddress,
}
