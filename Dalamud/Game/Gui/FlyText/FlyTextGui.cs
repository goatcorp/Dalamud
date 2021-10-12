using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Memory;
using Serilog;

namespace Dalamud.Game.Gui.FlyText
{
    /// <summary>
    /// This class facilitates interacting with and creating native in-game "fly text".
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class FlyTextGui : IDisposable
    {
        /// <summary>
        /// The native function responsible for adding fly text to the UI. See <see cref="FlyTextGuiAddressResolver.AddFlyText"/>.
        /// </summary>
        private readonly AddFlyTextDelegate addFlyTextNative;

        /// <summary>
        /// The hook that fires when the game creates a fly text element. See <see cref="FlyTextGuiAddressResolver.CreateFlyText"/>.
        /// </summary>
        private readonly Hook<CreateFlyTextDelegate> createFlyTextHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyTextGui"/> class.
        /// </summary>
        internal FlyTextGui()
        {
            this.Address = new FlyTextGuiAddressResolver();
            this.Address.Setup();

            this.addFlyTextNative = Marshal.GetDelegateForFunctionPointer<AddFlyTextDelegate>(this.Address.AddFlyText);
            this.createFlyTextHook = new Hook<CreateFlyTextDelegate>(this.Address.CreateFlyText, this.CreateFlyTextDetour);
        }

        /// <summary>
        /// The delegate defining the type for the FlyText event.
        /// </summary>
        /// <param name="kind">The FlyTextKind. See <see cref="FlyTextKind"/>.</param>
        /// <param name="val1">Value1 passed to the native flytext function.</param>
        /// <param name="val2">Value2 passed to the native flytext function. Seems unused.</param>
        /// <param name="text1">Text1 passed to the native flytext function.</param>
        /// <param name="text2">Text2 passed to the native flytext function.</param>
        /// <param name="color">Color passed to the native flytext function. Changes flytext color.</param>
        /// <param name="icon">Icon ID passed to the native flytext function. Only displays with select FlyTextKind.</param>
        /// <param name="yOffset">The vertical offset to place the flytext at. 0 is default. Negative values result
        /// in text appearing higher on the screen. This does not change where the element begins to fade.</param>
        /// <param name="handled">Whether this flytext has been handled. If a subscriber sets this to true, the FlyText will not appear.</param>
        public delegate void OnFlyTextCreatedDelegate(
            ref FlyTextKind kind,
            ref int val1,
            ref int val2,
            ref SeString text1,
            ref SeString text2,
            ref uint color,
            ref uint icon,
            ref float yOffset,
            ref bool handled);

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

        /// <summary>
        /// The FlyText event that can be subscribed to.
        /// </summary>
        public event OnFlyTextCreatedDelegate? FlyTextCreated;

        private Dalamud Dalamud { get; }

        private FlyTextGuiAddressResolver Address { get; }

        /// <summary>
        /// Disposes of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.createFlyTextHook.Dispose();
        }

        /// <summary>
        /// Displays a fly text in-game on the local player.
        /// </summary>
        /// <param name="kind">The FlyTextKind. See <see cref="FlyTextKind"/>.</param>
        /// <param name="actorIndex">The index of the actor to place flytext on. Indexing unknown. 1 places flytext on local player.</param>
        /// <param name="val1">Value1 passed to the native flytext function.</param>
        /// <param name="val2">Value2 passed to the native flytext function. Seems unused.</param>
        /// <param name="text1">Text1 passed to the native flytext function.</param>
        /// <param name="text2">Text2 passed to the native flytext function.</param>
        /// <param name="color">Color passed to the native flytext function. Changes flytext color.</param>
        /// <param name="icon">Icon ID passed to the native flytext function. Only displays with select FlyTextKind.</param>
        public unsafe void AddFlyText(FlyTextKind kind, uint actorIndex, uint val1, uint val2, SeString text1, SeString text2, uint color, uint icon)
        {
            // Known valid flytext region within the atk arrays
            var numIndex = 28;
            var strIndex = 25;
            var numOffset = 147u;
            var strOffset = 28u;

            // Get the UI module and flytext addon pointers
            var gameGui = Service<GameGui>.Get();
            var ui = (FFXIVClientStructs.FFXIV.Client.UI.UIModule*)gameGui.GetUIModule();
            var flytext = gameGui.GetAddonByName("_FlyText", 1);

            if (ui == null || flytext == IntPtr.Zero)
                return;

            // Get the number and string arrays we need
            var atkArrayDataHolder = ui->RaptureAtkModule.AtkModule.AtkArrayDataHolder;
            var numArray = atkArrayDataHolder._NumberArrays[numIndex];
            var strArray = atkArrayDataHolder._StringArrays[strIndex];

            // Write the values to the arrays using a known valid flytext region
            numArray->IntArray[numOffset + 0] = 1;                      // Some kind of "Enabled" flag for this section
            numArray->IntArray[numOffset + 1] = (int)kind;
            numArray->IntArray[numOffset + 2] = unchecked((int)val1);
            numArray->IntArray[numOffset + 3] = unchecked((int)val2);
            numArray->IntArray[numOffset + 4] = 5;                      // Unknown
            numArray->IntArray[numOffset + 5] = unchecked((int)color);
            numArray->IntArray[numOffset + 6] = unchecked((int)icon);
            numArray->IntArray[numOffset + 7] = 0;                      // Unknown
            numArray->IntArray[numOffset + 8] = 0;                      // Unknown, has something to do with yOffset

            fixed (byte* pText1 = text1.Encode())
            {
                fixed (byte* pText2 = text2.Encode())
                {
                    strArray->StringArray[strOffset + 0] = pText1;
                    strArray->StringArray[strOffset + 1] = pText2;

                    this.addFlyTextNative(
                        flytext,
                        actorIndex,
                        1,
                        (IntPtr)numArray,
                        numOffset,
                        9,
                        (IntPtr)strArray,
                        strOffset,
                        2,
                        0);
                }
            }
        }

        /// <summary>
        /// Enables this module.
        /// </summary>
        internal void Enable()
        {
            this.createFlyTextHook.Enable();
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
                var tmpYOffset = yOffset;

                var cmpText1 = tmpText1.ToString();
                var cmpText2 = tmpText2.ToString();

                Log.Verbose($"[FlyText] Called with addonFlyText({addonFlyText.ToInt64():X}) " +
                                         $"kind({kind}) val1({val1}) val2({val2}) " +
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
                            tmpColor != color ||
                            tmpIcon != icon ||
                            Math.Abs(tmpYOffset - yOffset) > float.Epsilon;

                // If not dirty, make the original call
                if (!dirty)
                {
                    Log.Verbose("[FlyText] Calling flytext with original args.");
                    return this.createFlyTextHook.Original(addonFlyText, kind, val1, val2, text2, color, icon, text1, yOffset);
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
}
