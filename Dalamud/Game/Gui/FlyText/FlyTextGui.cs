using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Serilog;

namespace Dalamud.Game.Gui.FlyText;

/// <summary>
/// This class facilitates interacting with and creating native in-game "fly text".
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class FlyTextGui : IInternalDisposableService, IFlyTextGui
{
    /// <summary>
    /// The hook that fires when the game creates a fly text element.
    /// </summary>
    private readonly Hook<AddonFlyText.Delegates.CreateFlyText> createFlyTextHook;

    [ServiceManager.ServiceConstructor]
    private unsafe FlyTextGui(TargetSigScanner sigScanner)
    {
        this.createFlyTextHook = Hook<AddonFlyText.Delegates.CreateFlyText>.FromAddress(AddonFlyText.Addresses.CreateFlyText.Value, this.CreateFlyTextDetour);

        this.createFlyTextHook.Enable();
    }

    /// <inheritdoc/>
    public event IFlyTextGui.OnFlyTextCreatedDelegate? FlyTextCreated;

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
        var numOffset = 161u;
        var strOffset = 28u;

        var flytext = (AddonFlyText*)RaptureAtkUnitManager.Instance()->GetAddonByName("_FlyText");
        if (flytext == null)
            return;

        // Get the number and string arrays we need
        var numArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.FlyText);
        var strArray = AtkStage.Instance()->GetStringArrayData(StringArrayType.FlyText);

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

        strArray->SetValue((int)strOffset + 0, text1.EncodeWithNullTerminator(), false, true, false);
        strArray->SetValue((int)strOffset + 1, text2.EncodeWithNullTerminator(), false, true, false);
        
        flytext->AddFlyText(actorIndex, 1, numArray, numOffset, 10, strArray, strOffset, 2, 0);
    }

    private unsafe nint CreateFlyTextDetour(
        AddonFlyText* thisPtr,
        int kind,
        int val1,
        int val2,
        byte* text2,
        uint color,
        uint icon,
        uint damageTypeIcon,
        byte* text1,
        float yOffset)
    {
        var retVal = nint.Zero;
        try
        {
            Log.Verbose("[FlyText] Enter CreateFlyText detour!");

            var handled = false;

            var tmpKind = (FlyTextKind)kind;
            var tmpVal1 = val1;
            var tmpVal2 = val2;
            var tmpText1 = text1 == null ? string.Empty : MemoryHelper.ReadSeStringNullTerminated((nint)text1);
            var tmpText2 = text2 == null ? string.Empty : MemoryHelper.ReadSeStringNullTerminated((nint)text2);
            var tmpColor = color;
            var tmpIcon = icon;
            var tmpDamageTypeIcon = damageTypeIcon;
            var tmpYOffset = yOffset;

            var originalText1 = tmpText1.EncodeWithNullTerminator();
            var originalText2 = tmpText2.EncodeWithNullTerminator();

            Log.Verbose($"[FlyText] Called with addonFlyText({(nint)thisPtr:X}) " +
                        $"kind({kind}) val1({val1}) val2({val2}) damageTypeIcon({damageTypeIcon}) " +
                        $"text1({(nint)text1:X}, \"{tmpText1}\") text2({(nint)text2:X}, \"{tmpText2}\") " +
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

            var maybeModifiedText1 = tmpText1.EncodeWithNullTerminator();
            var maybeModifiedText2 = tmpText2.EncodeWithNullTerminator();

            // Check if any values have changed
            var dirty = (int)tmpKind != kind ||
                        tmpVal1 != val1 ||
                        tmpVal2 != val2 ||
                        !maybeModifiedText1.SequenceEqual(originalText1) ||
                        !maybeModifiedText2.SequenceEqual(originalText2) ||
                        tmpDamageTypeIcon != damageTypeIcon ||
                        tmpColor != color ||
                        tmpIcon != icon ||
                        Math.Abs(tmpYOffset - yOffset) > float.Epsilon;

            // If not dirty, make the original call
            if (!dirty)
            {
                Log.Verbose("[FlyText] Calling flytext with original args.");
                return this.createFlyTextHook.Original(thisPtr, kind, val1, val2, text2, color, icon,
                                                       damageTypeIcon, text1, yOffset);
            }

            var pText1 = Marshal.AllocHGlobal(maybeModifiedText1.Length);
            var pText2 = Marshal.AllocHGlobal(maybeModifiedText2.Length);
            Marshal.Copy(maybeModifiedText1, 0, pText1, maybeModifiedText1.Length);
            Marshal.Copy(maybeModifiedText2, 0, pText2, maybeModifiedText2.Length);
            Log.Verbose("[FlyText] Allocated and set strings.");

            retVal = this.createFlyTextHook.Original(
                thisPtr,
                (int)tmpKind,
                tmpVal1,
                tmpVal2,
                (byte*)pText2,
                tmpColor,
                tmpIcon,
                tmpDamageTypeIcon,
                (byte*)pText1,
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
