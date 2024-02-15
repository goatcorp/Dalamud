using System.Runtime.InteropServices;

namespace Dalamud.Interface.Internal.ImGuiInternalStructs;

/// <summary>
/// Ported from imstb_textedit.h.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 0xE2C)]
internal struct StbTextEditState
{
    /// <summary>
    /// Position of the text cursor within the string.
    /// </summary>
    public int Cursor;

    /// <summary>
    /// Selection start point.
    /// </summary>
    public int SelectStart;

    /// <summary>
    /// selection start and end point in characters; if equal, no selection.
    /// </summary>
    /// <remarks>
    /// Note that start may be less than or greater than end (e.g. when dragging the mouse,
    /// start is where the initial click was, and you can drag in either direction.)
    /// </remarks>
    public int SelectEnd;

    /// <summary>
    /// Each text field keeps its own insert mode state.
    /// To keep an app-wide insert mode, copy this value in/out of the app state.
    /// </summary>
    public byte InsertMode;

    /// <summary>
    /// Page size in number of row.
    /// This value MUST be set to >0 for pageup or pagedown in multilines documents.
    /// </summary>
    public int RowCountPerPage;

    // Remainder is stb-private data.
}
