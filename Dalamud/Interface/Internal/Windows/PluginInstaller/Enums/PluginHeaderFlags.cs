namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;

[Flags]
internal enum PluginHeaderFlags
{
    None = 0,
    IsThirdParty = 1 << 0,
    HasTrouble = 1 << 1,
    UpdateAvailable = 1 << 2,
    MainRepoCrossUpdate = 1 << 3,
    IsNew = 1 << 4,
    IsInstallableOutdated = 1 << 5,
    IsOrphan = 1 << 6,
    IsTesting = 1 << 7,
    IsIncompatible = 1 << 8,
}
