using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Internal;
using Dalamud.Interface.Utility.Raii;

using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display FontAwesome Symbols.
/// </summary>
internal class FontAwesomeTestWidget : IDataWindowWidget
{
    private static readonly string[] First = ["(Show All)", "(Undefined)"];

    private List<FontAwesomeIcon>? icons;
    private List<string>? iconNames;
    private string[]? iconCategories;
    private int selectedIconCategory;
    private string iconSearchInput = string.Empty;
    private bool iconSearchChanged = true;
    private bool useFixedWidth;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["fa", "fatest", "fontawesome"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Font Awesome Test";

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
        using var pushedStyle = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        this.iconCategories ??= First.Concat(FontAwesomeHelpers.GetCategories().Skip(1)).ToArray();

        if (this.iconSearchChanged)
        {
            if (this.iconSearchInput == string.Empty && this.selectedIconCategory <= 1)
            {
                var en = InterfaceManager.IconFont.GlyphsWrapped()
                                         .Select(x => (FontAwesomeIcon)x.Codepoint)
                                         .Where(x => (ushort)x is >= 0xE000 and < 0xF000);
                en = this.selectedIconCategory == 0
                         ? en.Concat(FontAwesomeHelpers.SearchIcons(string.Empty, string.Empty))
                         : en.Except(FontAwesomeHelpers.SearchIcons(string.Empty, string.Empty));
                this.icons = en.Distinct().Order().ToList();
            }
            else
            {
                this.icons = FontAwesomeHelpers.SearchIcons(
                    this.iconSearchInput,
                    this.selectedIconCategory <= 1 ? string.Empty : this.iconCategories[this.selectedIconCategory]);
            }

            this.iconNames = this.icons.Select(icon => Enum.GetName(icon)!).ToList();
            this.iconSearchChanged = false;
        }

        ImGui.SetNextItemWidth(160f);
        var categoryIndex = this.selectedIconCategory;
        if (ImGui.Combo("####FontAwesomeCategorySearch", ref categoryIndex, this.iconCategories))
        {
            this.selectedIconCategory = categoryIndex;
            this.iconSearchChanged = true;
        }

        ImGui.SameLine(170f);
        ImGui.SetNextItemWidth(180f);
        if (ImGui.InputTextWithHint($"###FontAwesomeInputSearch", "search icons"u8, ref this.iconSearchInput, 50))
        {
            this.iconSearchChanged = true;
        }

        ImGui.Checkbox("Use fixed width font"u8, ref this.useFixedWidth);

        ImGuiHelpers.ScaledDummy(10f);
        for (var i = 0; i < this.icons?.Count; i++)
        {
            if (this.icons[i] == FontAwesomeIcon.None)
                continue;

            ImGui.AlignTextToFramePadding();
            ImGui.Text($"0x{(int)this.icons[i].ToIconChar():X}");
            ImGuiHelpers.ScaledRelativeSameLine(50f);
            ImGui.Text($"{this.iconNames?[i]}");
            ImGuiHelpers.ScaledRelativeSameLine(280f);

            using var pushedFont = ImRaii.PushFont(this.useFixedWidth ? InterfaceManager.IconFontFixedWidth : InterfaceManager.IconFont);
            ImGui.Text(this.icons[i].ToIconString());
            ImGuiHelpers.ScaledRelativeSameLine(320f);
            if (this.useFixedWidth
                    ? ImGui.Button($"{(char)this.icons[i]}##FontAwesomeIconButton{i}")
                    : ImGuiComponents.IconButton($"##FontAwesomeIconButton{i}", this.icons[i]))
            {
                _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                    this.DisplayName,
                    this.icons[i].ToString(),
                    Task.FromResult(
                        Service<TextureManager>.Get().CreateTextureFromSeString(
                            ReadOnlySeString.FromText(this.icons[i].ToIconString()),
                            new SeStringDrawParams { Font = ImGui.GetFont(), FontSize = ImGui.GetFontSize(), ScreenOffset = Vector2.Zero })));
            }

            ImGuiHelpers.ScaledDummy(2f);
        }
    }
}
