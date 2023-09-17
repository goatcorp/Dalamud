using System.IO;
using System.Threading.Tasks;
using Dalamud.Configuration.Internal;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using Serilog;

namespace Dalamud.Game.Network.Internal;

/// <summary>
/// This class handles network notifications and uploading market board data.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class NetworkHandlers : IDisposable, IServiceType
{
    private delegate nint CfPopDelegate(nint packetData);

    private readonly NetworkHandlersAddressResolver addressResolver;

    private readonly Hook<CfPopDelegate> cfPopHook;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private bool disposing;

    [ServiceManager.ServiceConstructor]
    private NetworkHandlers(GameNetwork gameNetwork, SigScanner sigScanner)
    {
        this.addressResolver = new NetworkHandlersAddressResolver();
        this.addressResolver.Setup(sigScanner);

        this.CfPop = (_, _) => { };

        this.cfPopHook = Hook<CfPopDelegate>.FromAddress(this.addressResolver.CfPopPacketHandler, this.CfPopDetour);
        this.cfPopHook.Enable();
    }

    /// <summary>
    /// Event which gets fired when a duty is ready.
    /// </summary>
    public event EventHandler<ContentFinderCondition> CfPop;

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.disposing = true;
        this.Dispose(this.disposing);
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    /// <param name="shouldDispose">Whether or not to execute the disposal.</param>
    protected void Dispose(bool shouldDispose)
    {
        if (!shouldDispose)
            return;

        this.cfPopHook.Dispose();
    }

    private unsafe nint CfPopDetour(nint packetData)
    {
        using var stream = new UnmanagedMemoryStream((byte*)packetData, 64);
        using var reader = new BinaryReader(stream);

        var notifyType = reader.ReadByte();
        stream.Position += 0x1B;
        var conditionId = reader.ReadUInt16();

        if (notifyType != 3)
            goto ORIGINAL;

        if (this.configuration.DutyFinderTaskbarFlash)
            Util.FlashWindow();

        var cfConditionSheet = Service<DataManager>.Get().GetExcelSheet<ContentFinderCondition>()!;
        var cfCondition = cfConditionSheet.GetRow(conditionId);

        if (cfCondition == null)
        {
            Log.Error("CFC key {ConditionId} not in Lumina data", conditionId);
            goto ORIGINAL;
        }

        var cfcName = cfCondition.Name.ToString();
        if (cfcName.IsNullOrEmpty())
        {
            cfcName = "Duty Roulette";
            cfCondition.Image = 112324;
        }

        Task.Run(() =>
        {
            if (this.configuration.DutyFinderChatMessage)
            {
                Service<ChatGui>.GetNullable()?.Print($"Duty pop: {cfcName}");
            }

            this.CfPop.InvokeSafely(this, cfCondition);
        }).ContinueWith(
            task => Log.Error(task.Exception, "CfPop.Invoke failed"),
            TaskContinuationOptions.OnlyOnFaulted);

        ORIGINAL:
        return this.cfPopHook.OriginalDisposeSafe(packetData);
    }

    private bool ShouldUpload<T>(T any)
    {
        return this.configuration.IsMbCollect;
    }
}
