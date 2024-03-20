using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
                        locLanguagesList.Add("Korean");
                        break;
                    case "tw":
                        locLanguagesList.Add("中華民國國語");
                        break;
                    default:
                        string locLanguage = CultureInfo.GetCultureInfo(language).NativeName;
                        locLanguagesList.Add(char.ToUpper(locLanguage[0]) + locLanguage[1..]);
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
