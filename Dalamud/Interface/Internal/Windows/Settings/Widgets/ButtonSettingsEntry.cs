﻿using System.Diagnostics.CodeAnalysis;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class ButtonSettingsEntry : SettingsEntry
{
    private readonly string description;
    private readonly Action runs;

    public ButtonSettingsEntry(string name, string description, Action runs)
    {
        this.description = description;
        this.runs = runs;
        this.Name = name;
    }

    public override void Load()
    {
        // ignored
    }

    public override void Save()
    {
        // ignored
    }

    public override void Draw()
    {
        if (ImGui.Button(this.Name))
        {
            this.runs.Invoke();
        }

        ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, this.description);
    }
}
