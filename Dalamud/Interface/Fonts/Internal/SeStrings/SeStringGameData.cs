using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Excel.Sheets;
using Dalamud.Interface.Fonts.Internal.SeStrings.Formats;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Excel;
using Lumina.Text.Payloads;

namespace Dalamud.Interface.Fonts.Internal.SeStrings;

/// <summary>The game data that the SeString stylers read, loaded once on first use.</summary>
internal static unsafe class SeStringGameData
{
    private const string GfdPath = "common/font/gfdata.gfd";
    private const int Icon2RemapCount = 30;

    private static readonly ModuleLog Log = new(typeof(SeStringGameData));

    private static readonly Lazy<(GfdFileView? Gfd, uint[,] ColorTypes)> Data = new(Load);

    private static (long Frame, long Key) iconRemap = (-1, 0);
    private static (long Frame, int Theme) gameTheme = (-1, 0);

    /// <summary>Gets the bitmap font icon table, from <c>gfdata.gfd</c>.</summary>
    /// <value>The table, or null if the game data could not be read.</value>
    public static GfdFileView? Gfd => Data.Value.Gfd;

    /// <summary>Gets the colors of each color type and theme, from the UIColor sheet.</summary>
    /// <value>The colors, or none if the game data could not be read.</value>
    public static uint[,] ColorTypes => Data.Value.ColorTypes;

    /// <summary>Gets the theme the game is set to.</summary>
    /// <returns>The theme, read once a frame. It is 0 (Dark) if the game is not there.</returns>
    public static int GameThemeIndex()
    {
        var frame = FontClock.Frame;
        if (gameTheme.Frame != frame)
            gameTheme = (frame, ReadGameThemeIndex());
        return gameTheme.Theme;
    }

    /// <summary>Gets a value that changes when the game maps <see cref="MacroCode.Icon2"/> icons to others, as for
    /// the gamepad in use.</summary>
    /// <returns>The value, read once a frame.</returns>
    public static long IconRemapKey()
    {
        var frame = FontClock.Frame;
        if (iconRemap.Frame != frame)
            iconRemap = (frame, ReadIconRemapKey());
        return iconRemap.Key;
    }

    /// <summary>Maps an <see cref="MacroCode.Icon2"/> icon as the game does, as for the gamepad in use.</summary>
    /// <param name="iconId">The icon.</param>
    /// <returns>The icon to display.</returns>
    public static int RemapIcon2(int iconId)
    {
        foreach (ref readonly var entry in Icon2RemapTable())
        {
            if (entry.IconId == iconId)
                return (int)entry.RemappedIconId;
        }

        return iconId;
    }

    /// <summary>Gets the game's texture of the icons of <see cref="Gfd"/>.</summary>
    /// <returns>The texture, or default if there is none. The game makes it only after logging in.</returns>
    public static ImTextureRef GetGfdTexture()
    {
        var uim = UIModule.Instance();
        if (uim is null)
            return default;
        var ram = uim->GetRaptureAtkModule();
        if (ram is null)
            return default;
        var gfd = ram->AtkModule.AtkFontManager.Gfd;
        if (gfd is null)
            return default;
        var tex = gfd->Texture;
        return tex is null || tex->D3D11ShaderResourceView is null
                   ? default
                   : new ImTextureRef(new((nint)tex->D3D11ShaderResourceView));
    }

    // Reads UIColor entries into ImGui-packed 0xAABBGGRR colors, indexed by row and theme.
    private static uint[,] BuildColorTypes(ExcelSheet<UIColor> uiColor)
    {
        var maxId = 0;
        foreach (var row in uiColor)
            maxId = (int)Math.Max(row.RowId, maxId);

        var colorTypes = new uint[maxId + 1, 9];
        foreach (var row in uiColor)
        {
            // UIColor stores packed 0xRRGGBBAA.
            colorTypes[row.RowId, 0] = row.Dark;
            colorTypes[row.RowId, 1] = row.Light;
            colorTypes[row.RowId, 2] = row.ClassicFF;
            colorTypes[row.RowId, 3] = row.ClearBlue;

            // Theme index 4 is unused by ActiveColorThemeType.
            colorTypes[row.RowId, 5] = row.ClearWhite;
            colorTypes[row.RowId, 6] = row.ClearGreen;
            colorTypes[row.RowId, 7] = row.ClearGrey;
            colorTypes[row.RowId, 8] = row.ClearPink;
        }

        if (BitConverter.IsLittleEndian)
        {
            // Reverse 0xRRGGBBAA into ImGui's packed 0xAABBGGRR.
            fixed (uint* p = colorTypes)
            {
                foreach (ref var r in new Span<uint>(p, colorTypes.GetLength(0) * colorTypes.GetLength(1)))
                    r = BinaryPrimitives.ReverseEndianness(r);
            }
        }

        return colorTypes;
    }

    // A failure is logged once, and kept for the session, as the game data does not change while the game runs.
    // SeStrings are then read without icons and color types.
    private static (GfdFileView? Gfd, uint[,] ColorTypes) Load()
    {
        try
        {
            var dm = Service<DataManager>.Get();
            var gfd = dm.GetFile(GfdPath) ?? throw new FileNotFoundException("The game has no icon table.", GfdPath);
            return (new(gfd.Data), BuildColorTypes(dm.Excel.GetSheet<UIColor>()));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to read the icons and colors of SeStrings. SeStrings are drawn without them");
            return (null, new uint[0, 0]);
        }
    }

    // Outlined so the try/catch when running outside the game stays off the cached fast path.
    private static int ReadGameThemeIndex()
    {
        try
        {
            var stage = AtkStage.Instance();
            return stage is null || stage->AtkUIColorHolder is null
                       ? 0
                       : stage->AtkUIColorHolder->ActiveColorThemeType;
        }
        catch (InvalidOperationException)
        {
            return 0;
        }
    }

    // Returns the Icon2 remap table, or empty outside the game or before AtkFontManager initializes.
    private static ReadOnlySpan<AtkFontManager.Icon2RemapEntry> Icon2RemapTable()
    {
        try
        {
            var ram = RaptureAtkModule.Instance();
            return ram is null || ram->AtkFontManager.Icon2RemapTable is null
                       ? default
                       : new(ram->AtkFontManager.Icon2RemapTable, Icon2RemapCount);
        }
        catch (InvalidOperationException)
        {
            return default;
        }
    }

    private static long ReadIconRemapKey()
    {
        var table = Icon2RemapTable();
        if (table.IsEmpty)
            return 0;
        var hash = default(HashCode);
        hash.AddBytes(MemoryMarshal.AsBytes(table));
        return hash.ToHashCode();
    }
}
