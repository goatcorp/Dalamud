using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.SpannedStrings;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.Utility;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class HintSettingsEntry : SettingsEntry
{
    private readonly SpannedString text;
    private readonly Vector4 color;

    public HintSettingsEntry(string text, Vector4? color = null)
    {
        try
        {
            this.text = SpannedString.Parse(text, CultureInfo.InvariantCulture);
        }
        catch (Exception e)
        {
            Log.Error(e, $"{nameof(HintSettingsEntry)}: failed to parse");
            this.text = new SpannedStringBuilder().Append(text).Build();
        }

        this.color = color ?? ImGuiColors.DalamudGrey;
    }

    public override void Load()
    {
        // ignore
    }

    public override void Save()
    {
        // ignore
    }

    public override void Draw()
    {
        using var renderer = Service<SpannableFactory>.Get().Rent(
            true,
            new()
            {
                WordBreak = WordBreakType.BreakWord,
                InitialStyle = new()
                {
                    ForeColorU32 = this.color,
                },
            });
        renderer.Render(this.text, out _, out _);
    }
}
