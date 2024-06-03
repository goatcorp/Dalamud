using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Services;

using Serilog;

namespace Dalamud.Game.Gui.FlyText;

/// <summary>
/// This class facilitates interacting with and creating native in-game "fly text".
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed class FlyTextGui : IInternalDisposableService, IFlyTextGui
{
    /// <summary>
    /// The native function responsible for adding fly text to the UI. See <see cref="FlyTextGuiAddressResolver.AddFlyText"/>.
    /// </summary>
    private readonly AddFlyTextDelegate addFlyTextNative;

    /// <summary>
    /// The hook that fires when the game creates a fly text element. See <see cref="FlyTextGuiAddressResolver.CreateFlyText"/>.
    /// </summary>
    private readonly Hook<CreateFlyTextDelegate> createFlyTextHook;

    [ServiceManager.ServiceConstructor]
    private FlyTextGui(TargetSigScanner sigScanner)
    {
        this.Address = new FlyTextGuiAddressResolver();
        this.Address.Setup(sigScanner);

        this.addFlyTextNative = Marshal.GetDelegateForFunctionPointer<AddFlyTextDelegate>(this.Address.AddFlyText);
        this.createFlyTextHook = Hook<CreateFlyTextDelegate>.FromAddress(this.Address.CreateFlyText, this.CreateFlyTextDetour);

        this.createFlyTextHook.Enable();
    }

    /// <summary>
    /// Private delegate for the native CreateFlyText function's hook.
    /// </summary>
    private delegate IntPtr CreateFlyTextDelegate(
        IntPtr addonFlyText,
        FlyTextKind kind,
        int val1,
        int val2,
        IntPtr text2,
        uint color,
        uint icon,
        uint damageTypeIcon,
        IntPtr text1,
        float yOffset);

    /// <summary>
    /// Private delegate for the native AddFlyText function pointer.
    /// </summary>
    private delegate void AddFlyTextDelegate(
        IntPtr addonFlyText,
        uint actorIndex,
        uint messageMax,
        IntPtr numbers,
        uint offsetNum,
        uint offsetNumMax,
        IntPtr strings,
        uint offsetStr,
        uint offsetStrMax,
        int unknown);

    /// <inheritdoc/>
    public event IFlyTextGui.OnFlyTextCreatedDelegate? FlyTextCreated;

    private FlyTextGuiAddressResolver Address { get; }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.createFlyTextHook.Dispose();
    }

    /// <inheritdoc/>
    public unsafe void AddFlyText(FlyTextKind kind, uint actorIndex, uint val1, uint val2, SeString text1, SeString text2, uint color, uint icon, uint damageTypeIcon)
    {
        // Known valid flytext region within the atk arrays
        var numIndex = 30;
        var strIndex = 27;
        var numOffset = 161u;
        var strOffset = 28u;

        // Get the UI module and flytext addon pointers
        var gameGui = Service<GameGui>.GetNullable();
        if (gameGui == null)
            return;

        var ui = (FFXIVClientStructs.FFXIV.Client.UI.UIModule*)gameGui.GetUIModule();
        var flytext = gameGui.GetAddonByName("_FlyText");

        if (ui == null || flytext == IntPtr.Zero)
            return;

        // Get the number and string arrays we need
        var atkArrayDataHolder = ui->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
        var numArray = atkArrayDataHolder._NumberArrays[numIndex];
        var strArray = atkArrayDataHolder._StringArrays[strIndex];

        // Write the values to the arrays using a known valid flytext region
        numArray->IntArray[numOffset + 0] = 1; // Some kind of "Enabled" flag for this section
        numArray->IntArray[numOffset + 1] = (int)kind;
        numArray->IntArray[numOffset + 2] = unchecked((int)val1);
        numArray->IntArray[numOffset + 3] = unchecked((int)val2);
        numArray->IntArray[numOffset + 4] = unchecked((int)damageTypeIcon); // Icons for damage type
        numArray->IntArray[numOffset + 5] = 5;                              // Unknown
        numArray->IntArray[numOffset + 6] = unchecked((int)color);
        numArray->IntArray[numOffset + 7] = unchecked((int)icon);
        numArray->IntArray[numOffset + 8] = 0; // Unknown
        numArray->IntArray[numOffset + 9] = 0; // Unknown, has something to do with yOffset

        strArray->SetValue((int)strOffset + 0, text1.Encode(), false, true, false);
        strArray->SetValue((int)strOffset + 1, text2.Encode(), false, true, false);

        this.addFlyTextNative(
            flytext,
            actorIndex,
            1,
            (IntPtr)numArray,
            numOffset,
            10,
            (IntPtr)strArray,
            strOffset,
            2,
            0);
    }

    private static byte[] Terminate(byte[] source)
    {
        var terminated = new byte[source.Length + 1];
        Array.Copy(source, 0, terminated, 0, source.Length);
        terminated[^1] = 0;

        return terminated;
    }

    private IntPtr CreateFlyTextDetour(
        IntPtr addonFlyText,
        FlyTextKind kind,
        int val1,
        int val2,
        IntPtr text2,
        uint color,
        uint icon,
        uint damageTypeIcon,
        IntPtr text1,
        float yOffset)
    {
        var retVal = IntPtr.Zero;
        try
        {
            Log.Verbose("[FlyText] Enter CreateFlyText detour!");

            var handled = false;

            var tmpKind = kind;
            var tmpVal1 = val1;
            var tmpVal2 = val2;
            var tmpText1 = text1 == IntPtr.Zero ? string.Empty : MemoryHelper.ReadSeStringNullTerminated(text1);
            var tmpText2 = text2 == IntPtr.Zero ? string.Empty : MemoryHelper.ReadSeStringNullTerminated(text2);
            var tmpColor = color;
            var tmpIcon = icon;
            var tmpDamageTypeIcon = damageTypeIcon;
            var tmpYOffset = yOffset;

            var cmpText1 = tmpText1.ToString();
            var cmpText2 = tmpText2.ToString();

            Log.Verbose($"[FlyText] Called with addonFlyText({addonFlyText.ToInt64():X}) " +
                        $"kind({kind}) val1({val1}) val2({val2}) damageTypeIcon({damageTypeIcon}) " +
                        $"text1({text1.ToInt64():X}, \"{tmpText1}\") text2({text2.ToInt64():X}, \"{tmpText2}\") " +
                        $"color({color:X}) icon({icon}) yOffset({yOffset})");
            Log.Verbose("[FlyText] Calling flytext events!");
            this.FlyTextCreated?.Invoke(
                ref tmpKind,
                ref tmpVal1,
                ref tmpVal2,
                ref tmpText1,
                ref tmpText2,
                ref tmpColor,
                ref tmpIcon,
                ref tmpDamageTypeIcon,
                ref tmpYOffset,
                ref handled);

            // If handled, ignore the original call
            if (handled)
            {
                Log.Verbose("[FlyText] FlyText was handled.");

                // Returning null to AddFlyText from CreateFlyText will result
                // in the operation being dropped entirely.
                return IntPtr.Zero;
            }

            // Check if any values have changed
            var dirty = tmpKind != kind ||
                        tmpVal1 != val1 ||
                        tmpVal2 != val2 ||
                        tmpText1.ToString() != cmpText1 ||
                        tmpText2.ToString() != cmpText2 ||
                        tmpDamageTypeIcon != damageTypeIcon ||
                        tmpColor != color ||
                        tmpIcon != icon ||
                        Math.Abs(tmpYOffset - yOffset) > float.Epsilon;

            // If not dirty, make the original call
            if (!dirty)
            {
                Log.Verbose("[FlyText] Calling flytext with original args.");
                return this.createFlyTextHook.Original(addonFlyText, kind, val1, val2, text2, color, icon,
                                                       damageTypeIcon, text1, yOffset);
            }

            var terminated1 = Terminate(tmpText1.Encode());
            var terminated2 = Terminate(tmpText2.Encode());
            var pText1 = Marshal.AllocHGlobal(terminated1.Length);
            var pText2 = Marshal.AllocHGlobal(terminated2.Length);
            Marshal.Copy(terminated1, 0, pText1, terminated1.Length);
            Marshal.Copy(terminated2, 0, pText2, terminated2.Length);
            Log.Verbose("[FlyText] Allocated and set strings.");

            retVal = this.createFlyTextHook.Original(
                addonFlyText,
                tmpKind,
                tmpVal1,
                tmpVal2,
                pText2,
                tmpColor,
                tmpIcon,
                tmpDamageTypeIcon,
                pText1,
                tmpYOffset);

            Log.Verbose("[FlyText] Returned from original. Delaying free task.");

            Task.Delay(2000).ContinueWith(_ =>
            {
                try
                {
                    Marshal.FreeHGlobal(pText1);
                    Marshal.FreeHGlobal(pText2);
                    Log.Verbose("[FlyText] Freed strings.");
                }
                catch (Exception e)
                {
                    Log.Verbose(e, "[FlyText] Exception occurred freeing strings in task.");
                }
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception occurred in CreateFlyTextDetour!");
        }

        return retVal;
    }
}

/// <summary>
/// Plugin scoped version of FlyTextGui.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IFlyTextGui>]
#pragma warning restore SA1015
internal class FlyTextGuiPluginScoped : IInternalDisposableService, IFlyTextGui
{
    [ServiceManager.ServiceDependency]
    private readonly FlyTextGui flyTextGuiService = Service<FlyTextGui>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="FlyTextGuiPluginScoped"/> class.
    /// </summary>
    internal FlyTextGuiPluginScoped()
    {
        this.flyTextGuiService.FlyTextCreated += this.FlyTextCreatedForward;
    }

    /// <inheritdoc/>
    public event IFlyTextGui.OnFlyTextCreatedDelegate? FlyTextCreated;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.flyTextGuiService.FlyTextCreated -= this.FlyTextCreatedForward;

        this.FlyTextCreated = null;
    }

    /// <inheritdoc/>
    public void AddFlyText(FlyTextKind kind, uint actorIndex, uint val1, uint val2, SeString text1, SeString text2, uint color, uint icon, uint damageTypeIcon)
    {
        this.flyTextGuiService.AddFlyText(kind, actorIndex, val1, val2, text1, text2, color, icon, damageTypeIcon);
    }

    private void FlyTextCreatedForward(ref FlyTextKind kind, ref int val1, ref int val2, ref SeString text1, ref SeString text2, ref uint color, ref uint icon, ref uint damageTypeIcon, ref float yOffset, ref bool handled)
        => this.FlyTextCreated?.Invoke(ref kind, ref val1, ref val2, ref text1, ref text2, ref color, ref icon, ref damageTypeIcon, ref yOffset, ref handled);
}
