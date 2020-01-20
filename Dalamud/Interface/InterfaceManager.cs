using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
using EasyHook;
using ImGuiNET;
using ImGuiScene;
using Serilog;

// general dev notes, here because it's easiest
/*
 * - Hooking ResizeBuffers seemed to be unnecessary, though I'm not sure why.  Left out for now since it seems to work without it.
 * - We may want to build our ImGui command list in a thread to keep it divorced from present.  We'd still have to block in present to
 *   synchronize on the list and render it, but ideally the overall delay we add to present would then be shorter.  This may cause minor
 *   timing issues with anything animated inside ImGui, but that is probably rare and may not even be noticeable.
 * - Our hook is too low level to really work well with debugging, as we only have access to the 'real' dx objects and not any
 *   that have been hooked/wrapped by tools.
 * - Might eventually want to render to a separate target and composite, especially with reshade etc in the mix.
 */

namespace Dalamud.Interface
{
    public class InterfaceManager : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PresentDelegate(IntPtr swapChain, uint syncInterval, uint presentFlags);

        private readonly Hook<PresentDelegate> presentHook;

        private readonly Hook<SetCursorDelegate> setCursorHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetCursorDelegate(IntPtr hCursor);

        private ISwapChainAddressResolver Address { get; }

        private RawDX11Scene scene;

        /// <summary>
        /// This event gets called by a plugin UiBuilder when read
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnDraw;

        public InterfaceManager(SigScanner scanner)
        {
            try {
                var sigResolver = new SwapChainSigResolver();
                sigResolver.Setup(scanner);

                Log.Verbose("Found SwapChain via signatures.");

                Address = sigResolver;
            } catch (Exception ex) {
                // The SigScanner method fails on wine/proton since DXGI is not a real DLL. We fall back to vtable to detect our Present function address.
                Log.Error(ex, "Could not get SwapChain address via sig method, falling back to vtable...");

                var vtableResolver = new SwapChainVtableResolver();
                vtableResolver.Setup(scanner);

                Log.Verbose("Found SwapChain via vtable.");

                Address = vtableResolver;
            }

            var setCursorAddr = LocalHook.GetProcAddress("user32.dll", "SetCursor");

            Log.Verbose("===== S W A P C H A I N =====");
            Log.Verbose("SetCursor address {SetCursor}", setCursorAddr);
            Log.Verbose("Present address {Present}", Address.Present);

            this.setCursorHook = new Hook<SetCursorDelegate>(setCursorAddr, new SetCursorDelegate(SetCursorDetour), this);

            this.presentHook =
                new Hook<PresentDelegate>(Address.Present, 
                    new PresentDelegate(PresentDetour),
                    this);
        }

        public void Enable()
        {
            this.setCursorHook.Enable();
            this.presentHook.Enable();
        }

        private void Disable()
        {
            this.setCursorHook.Disable();
            this.presentHook.Disable();
        }

        public void Dispose()
        {
            // HACK: this is usually called on a separate thread from PresentDetour (likely on a dedicated render thread)
            // and if we aren't already disabled, disposing of the scene and hook can frequently crash due to the hook
            // being disposed of in this thread while it is actively in use in the render thread.
            // This is a terrible way to prevent issues, but should basically always work to ensure that all outstanding
            // calls to PresentDetour have finished (and Disable means no new ones will start), before we try to cleanup
            // So... not great, but much better than constantly crashing on unload
            this.Disable();
            System.Threading.Thread.Sleep(100);

            this.scene.Dispose();
            this.presentHook.Dispose();
        }

        private IntPtr PresentDetour(IntPtr swapChain, uint syncInterval, uint presentFlags)
        {
            if (this.scene == null)
            {
                this.scene = new RawDX11Scene(swapChain);
                this.scene.OnBuildUI += Display;
            }

            this.scene.Render();

            return this.presentHook.Original(swapChain, syncInterval, presentFlags);
        }

        // can't access imgui IO before first present call
        private bool lastWantCapture = false;

        private IntPtr SetCursorDetour(IntPtr hCursor) {
            Log.Debug($"hCursor: {hCursor.ToInt64():X} WantCapture: {this.lastWantCapture}");

            if (this.lastWantCapture == true && (!scene?.IsImGuiCursor(hCursor) ?? false))
                return IntPtr.Zero;

            return this.setCursorHook.Original(hCursor);
        }

        private void Display()
        {
            // this is more or less part of what reshade/etc do to avoid having to manually
            // set the cursor inside the ui
            // This will just tell ImGui to draw its own software cursor instead of using the hardware cursor
            // The scene internally will handle hiding and showing the hardware (game) cursor
            // If the player has the game software cursor enabled, we can't really do anything about that and
            // they will see both cursors.
            // Doing this here because it's somewhat application-specific behavior
            //ImGui.GetIO().MouseDrawCursor = ImGui.GetIO().WantCaptureMouse;
            this.lastWantCapture = ImGui.GetIO().WantCaptureMouse;

            OnDraw?.Invoke();
        }
    }
}
