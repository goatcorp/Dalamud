namespace Dalamud.Plugin.Internal.Profiles;

internal class ProfilePluginEntry
{
    public ProfilePluginEntry(string internalName, bool state)
    {
        this.InternalName = internalName;
        this.IsEnabled = state;
    }

    public string InternalName { get; }

    public bool IsEnabled { get; }
}
