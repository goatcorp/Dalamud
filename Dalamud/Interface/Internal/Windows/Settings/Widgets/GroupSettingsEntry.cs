using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

/// <summary>
/// Bundles a primary entry with attached entries (i.e. hints).
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class GroupSettingsEntry : SettingsEntry
{
    private readonly SettingsEntry[] entries;

    public GroupSettingsEntry(SettingsEntry primary, params SettingsEntry[] attached)
    {
        this.entries = [primary, .. attached];
        this.Name = primary.Name;
    }

    public override bool IsValid => this.entries.All(x => x.IsValid);

    public override bool IsVisible => this.entries[0].IsVisible;

    public override void Load()
    {
        foreach (var entry in this.entries)
        {
            entry.Load();
        }
    }

    public override void Save()
    {
        foreach (var entry in this.entries)
        {
            entry.Save();
        }
    }

    public override void Draw()
    {
        foreach (var entry in this.entries)
        {
            if (!entry.IsVisible)
            {
                continue;
            }

            entry.Draw();
        }
    }

    public override void PostDraw()
    {
        foreach (var entry in this.entries)
        {
            entry.PostDraw();
        }
    }

    public override void OnOpen()
    {
        foreach (var entry in this.entries)
        {
            entry.OnOpen();
        }
    }

    public override void OnClose()
    {
        foreach (var entry in this.entries)
        {
            entry.OnClose();
        }
    }

    public override bool MatchesSearch(string search)
    {
        return this.entries.Any(x => x.MatchesSearch(search));
    }
}
