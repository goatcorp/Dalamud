using System.Runtime.InteropServices;

using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Text.Internal;

/// <summary>Maps rectangular boundary to a spannable record.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct TsBoundaryToRecord
{
    /// <summary>Rectangular boundary.</summary>
    public RectVector4 Boundary;
    
    /// <summary>Index of spannable record.</summary>
    public int RecordIndex;

    /// <summary>Initializes a new instance of the <see cref="TsBoundaryToRecord"/> struct.</summary>
    /// <param name="recordIndex">Index of spannable record.</param>
    /// <param name="boundary">Rectangular boundary.</param>
    public TsBoundaryToRecord(int recordIndex, RectVector4 boundary)
    {
        this.RecordIndex = recordIndex;
        this.Boundary = boundary;
    }
}
