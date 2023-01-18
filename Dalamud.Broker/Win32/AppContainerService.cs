namespace Dalamud.Broker.Win32;

internal sealed class AppContainerService : IDisposable
{
    public AppContainer Container { get; }

    public AppContainerService()
    {
        this.Container = AppContainerHelper.CreateContainer();
    }

    public void Dispose()
    {
        this.Container.Dispose();
    }
}
