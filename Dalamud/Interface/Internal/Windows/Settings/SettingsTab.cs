using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal abstract class SettingsTab : IDisposable
{
    public abstract SettingsEntry[] Entries { get; }

    public abstract string Title { get; }

    public abstract SettingsOpenKind Kind { get; }

    public bool IsOpen { get; set; } = false;

    public virtual void OnOpen()
    {
        foreach (var settingsEntry in this.Entries)
        {
            settingsEntry.OnOpen();
        }
    }

    public virtual void OnClose()
    {
        foreach (var settingsEntry in this.Entries)
        {
            settingsEntry.OnClose();
        }
    }

    public virtual void Draw()
    {
        for (var i = 0; i < this.Entries.Length; i++)
        {
            var settingsEntry = this.Entries[i];
            if (!settingsEntry.IsVisible)
                continue;

            settingsEntry.Draw();

            var needsSpacing = settingsEntry is not GapSettingsEntry;

            for (var j = i + 1; j < this.Entries.Length; j++)
            {
                var nextEntry = this.Entries[j];
                if (!nextEntry.IsVisible)
                    continue;

                needsSpacing &= nextEntry is not GapSettingsEntry;
                break;
            }

            if (needsSpacing)
                ImGuiHelpers.ScaledDummy(5);
        }

        ImGuiHelpers.ScaledDummy(15);
    }

    public virtual void PostDraw()
    {
        foreach (var settingsEntry in this.Entries)
        {
            settingsEntry.PostDraw();
        }
    }

    public virtual void Load()
    {
        foreach (var settingsEntry in this.Entries)
        {
            settingsEntry.Load();
        }
    }

    public virtual void Save()
    {
        foreach (var settingsEntry in this.Entries)
        {
            settingsEntry.Save();
        }
    }

    public virtual void Discard()
    {
        foreach (var settingsEntry in this.Entries)
        {
            settingsEntry.Load();
        }
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        // ignored
    }
}
