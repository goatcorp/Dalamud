using System.Diagnostics.CodeAnalysis;

using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public abstract class SettingsTab : IDisposable
{
    public abstract SettingsEntry[] Entries { get; }

    public abstract string Title { get; }

    public bool IsOpen { get; set; } = false;

    public virtual bool IsVisible { get; } = true;

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
        foreach (var settingsEntry in this.Entries)
        {
            if (settingsEntry.IsVisible)
                settingsEntry.Draw();

            ImGuiHelpers.ScaledDummy(5);
        }

        ImGuiHelpers.ScaledDummy(15);
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
