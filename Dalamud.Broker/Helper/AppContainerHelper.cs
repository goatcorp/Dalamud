using System.Runtime.Versioning;
using Dalamud.Broker.Win32;

namespace Dalamud.Broker.Helper;

internal static class AppContainerHelper
{
    public const string ContainerName = "Dalamud.Container";
    public const string ContainerDisplayName = "Dalamud Container";
    public const string ContainerDescription = "AppContainer sandbox for Dalamud and FINAL FANTASY XIV";

    public static AppContainer GetContainer()
    {
        return AppContainer.GetOrCreate(ContainerName, ContainerDisplayName, ContainerDescription);
    }
}
