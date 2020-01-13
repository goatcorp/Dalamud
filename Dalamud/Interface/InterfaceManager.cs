using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.Internal.DXGI;
using Dalamud.Hooking;
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

        private SwapChainAddressResolver Address { get; }

        private RawDX11Scene scene;

        /// <summary>
        /// This event gets called by a plugin UiBuilder when read
        /// </summary>
        public event RawDX11Scene.BuildUIDelegate OnDraw;

        public InterfaceManager(SigScanner scanner)
        {
            Address = new SwapChainAddressResolver();
            Address.Setup(scanner);

            Log.Verbose("===== S W A P C H A I N =====");
            Log.Verbose("Present address {Present}", Address.Present);

            this.presentHook =
                new Hook<PresentDelegate>(Address.Present,
                                                    new PresentDelegate(PresentDetour),
                                                    this);
        }

        public void Enable()
        {
            this.presentHook.Enable();

            if (this.scene != null)
            {
                this.scene.Enable();
            }
        }

        public void Disable()
        {
            this.presentHook.Disable();

            if (this.scene != null)
            {
                this.scene.Disable();
            }
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

        private void Display()
        {
            // this is more or less part of what reshade/etc do to avoid having to manually
            // set the cursor inside the ui
            // This will just tell ImGui to draw its own software cursor instead of using the hardware cursor
            // The scene internally will handle hiding and showing the hardware (game) cursor
            // If the player has the game software cursor enabled, we can't really do anything about that and
            // they will see both cursors.
            // Doing this here because it's somewhat application-specific behavior
            ImGui.GetIO().MouseDrawCursor = ImGui.GetIO().WantCaptureMouse;

            OnDraw?.Invoke();
        }
    }
}
