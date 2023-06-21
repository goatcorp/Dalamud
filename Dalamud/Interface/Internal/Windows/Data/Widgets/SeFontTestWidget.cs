﻿using Dalamud.Game.Text;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget for displaying test data for SE Font Symbols.
/// </summary>
internal class SeFontTestWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.SE_Font_Test;

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var specialChars = string.Empty;

        for (var i = 0xE020; i <= 0xE0DB; i++)
            specialChars += $"0x{i:X} - {(SeIconChar)i} - {(char)i}\n";

        ImGui.TextUnformatted(specialChars);
    }
}
