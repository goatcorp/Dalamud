using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Internal.Windows;
using Dalamud.Networking.Http;

using Serilog;

namespace Dalamud.Interface;

[ServiceManager.EarlyLoadedService]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "One-off")]
internal class Fools24 : IServiceType
{
    private readonly HappyHttpClient httpClient;

    private CancellationTokenSource? cancellation;
    private Task? horseIconTask = null;

    private string[]? applicableIcons = null;

    [ServiceManager.ServiceConstructor]
    public Fools24(HappyHttpClient httpClient)
    {
        this.httpClient = httpClient;
    }
    
    public bool IsWaitingForIconList => this.horseIconTask?.IsCompleted == false;

    public bool Failed { get; private set; }
    
    public static bool IsDayApplicable()
    {
        var utcNow = DateTime.UtcNow;

        var dateAhead = utcNow.AddHours(14);
        var dateBehind = utcNow.AddHours(-12);

        return dateAhead is { Day: 1, Month: 4 } || dateBehind is { Day: 1, Month: 4 } || DateTime.Now is { Day: 1, Month: 4 };
    }
    
    public string? GetHorseIconLink(string internalName)
    {
        if (this.applicableIcons == null || this.applicableIcons.All(x => x != $"{internalName}.png"))
            return null;
        
        return $"https://raw.githubusercontent.com/goaaats/horse-icons/main/icons/{internalName}.png";
    }

    public void NotifyInstallerWindowOpened()
    {
        if (!IsDayApplicable())
            return;
        
        Service<PluginImageCache>.Get().ClearIconCache();

        if (this.horseIconTask?.IsCompleted == false)
            return;
        
        this.Failed = false;
        try
        {
            this.cancellation = new CancellationTokenSource();
            this.horseIconTask = this.httpClient.SharedHttpClient.GetStringAsync("https://raw.githubusercontent.com/goaaats/horse-icons/main/iconlist.txt", this.cancellation.Token)
                .ContinueWith(
                    f =>
                    {
                        if (!f.IsCompletedSuccessfully)
                        {
                            this.Failed = true;
                            this.applicableIcons = null;
                            return;
                        }

                        if (f.Result is not { Length: > 0 })
                        {
                            this.Failed = true;
                            this.applicableIcons = null;
                            return;
                        }

                        this.applicableIcons = f.Result.Split(
                            '\n',
                            StringSplitOptions.RemoveEmptyEntries);
                    });
            this.cancellation.CancelAfter(10000);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch horse icons");
            this.Failed = true;
        }
    }
}
