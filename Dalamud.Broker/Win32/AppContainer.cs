using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace Dalamud.Broker.Win32;

internal sealed class AppContainer : IDisposable
{
    public PSID Psid { get; }

    private AppContainer(PSID psid)
    {
        this.Psid = psid;
    }

    ~AppContainer()
    {
        this.DisposeUnmanaged();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.DisposeUnmanaged();
    }

    private unsafe void DisposeUnmanaged()
    {
        PInvoke.FreeSid(this.Psid);
    }

    public static AppContainer Get(string containerName)
    {
        var hresult = PInvoke.DeriveAppContainerSidFromAppContainerName(containerName, out var psid);
        hresult.ThrowOnFailure();

        return new AppContainer(psid);
    }

    public static AppContainer GetOrCreate(string containerName, string displayName, string description)
    {
        var hresult = PInvoke.CreateAppContainerProfile(
            containerName,
            displayName,
            description,
            Span<SID_AND_ATTRIBUTES>.Empty,
            out var psid
        );
        if (hresult.Value == PInvoke.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_ALREADY_EXISTS))
        {
            return Get(containerName);
        }

        hresult.ThrowOnFailure();

        return new AppContainer(psid);
    }
}
