namespace Dalamud.Broker.Win32;

internal static class AppContainerHelper
{
    private const string ContainerName = "Dalamud.Container";
    private const string ContainerDisplayName = "Dalamud";
    private const string ContainerDescription = "A sandbox environment for FINAL FANTASY XIV and Dalamud";

    public static AppContainer CreateContainer()
    {
        return AppContainer.GetOrCreate(ContainerName, ContainerDisplayName, ContainerDescription); 
    }
}
