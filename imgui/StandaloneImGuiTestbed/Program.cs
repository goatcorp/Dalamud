using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace StandaloneImGuiTestbed;

class Program
{
    static void Main(string[] args)
    {
        Sdl2Window window;
        GraphicsDevice gd;

        ImGuiBackend? backend = null;

        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(50, 50, 1280, 800, WindowState.Normal, "Dalamud Standalone ImGui Testbed"),
            new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
            out window,
            out gd);

        window.Resized += () =>
        {
            gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
            backend!.WindowResized(window.Width, window.Height);
        };

        var cl = gd.ResourceFactory.CreateCommandList();
        backend = new ImGuiBackend(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height, new FileInfo("imgui.ini"), 21.0f);

        var testbed = new Testbed();

        while (window.Exists)
        {
            Thread.Sleep(50);

            var snapshot = window.PumpEvents();

            if (!window.Exists)
                break;

            backend.Update(1f / 60f, snapshot);

            testbed.Draw();

            cl.Begin();
            cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, new RgbaFloat(0, 0, 0, 1f));
            backend.Render(gd, cl);
            cl.End();
            gd.SubmitCommands(cl);
            gd.SwapBuffers(gd.MainSwapchain);
        }

        // Clean up Veldrid resources
        gd.WaitForIdle();
        backend.Dispose();
        cl.Dispose();
        gd.Dispose();
    }
}
