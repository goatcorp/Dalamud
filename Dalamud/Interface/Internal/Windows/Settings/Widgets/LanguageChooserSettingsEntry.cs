using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public sealed class LanguageChooserSettingsEntry : SettingsEntry
{
    private readonly string[] languages;
    private readonly string[] locLanguages;

    private int langIndex = -1;

    public LanguageChooserSettingsEntry()
    {
        this.languages = Localization.ApplicableLangCodes.Prepend("en").ToArray();

        this.Name = Loc.Localize("DalamudSettingsLanguage", "Language");
        this.IsValid = true;
        this.IsVisible = true;

        try
        {
            var locLanguagesList = new List<string>();
            foreach (var language in this.languages)
            {
                switch (language)
                {
                    case "ko":
                        // We're intentionally keeping this in English, as the Korean fonts are not loaded in unless
                        // the language is already Korean or other preconditions are met. It's excessive to load a font
                        // for two characters.
                        locLanguagesList.Add("Korean");
                        break;
                    default:
                        var loc = Localization.GetCultureInfoFromLangCode(language);
                        locLanguagesList.Add(loc.TextInfo.ToTitleCase(loc.NativeName));
                        break;
                }
            }

            this.locLanguages = locLanguagesList.ToArray();
        }
        catch (Exception)
        {
            this.locLanguages = this.languages; // Languages not localized, only codes.
        }
    }

    public override void Load()
    {
        this.langIndex = Array.IndexOf(this.languages, Service<DalamudConfiguration>.Get().EffectiveLanguage);
        if (this.langIndex == -1)
            this.langIndex = 0;
    }

    public override void Save()
    {
        Service<Localization>.Get().SetupWithLangCode(this.languages[this.langIndex]);
        Service<DalamudConfiguration>.Get().LanguageOverride = this.languages[this.langIndex];
    }

    public override void Draw()
    {
        ImGui.Text(this.Name);
        ImGui.Combo("##XlLangCombo", ref this.langIndex, this.locLanguages, this.locLanguages.Length);
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsLanguageHint", "Select the language Dalamud will be displayed in."));
    }
}
