using ImGuiNET;
using PInvoke;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using StbiSharp;

using System.IO;

using Dalamud.Interface.Textures.TextureWraps;

using ImGuiScene.ImGui_Impl;
using ImGuizmoNET;
using ImPlotNET;
using Device = SharpDX.Direct3D11.Device;

namespace ImGuiScene
{
    // This class will likely eventually be unified a bit more with other scenes, but for
    // now it should be directly useable
    public sealed class RawDX11Scene : IDisposable
    {
        public Device Device { get; private set; }
        public IntPtr WindowHandlePtr { get; private set; }
        public SwapChain SwapChain { get; private set; }

        public bool UpdateCursor
        {
            get => this.imguiInput.UpdateCursor;
            set => this.imguiInput.UpdateCursor = value;
        }

        private DeviceContext deviceContext;
        private RenderTargetView rtv;

        private int targetWidth;
        private int targetHeight;

        private ImGui_Impl_DX11 imguiRenderer;
        private ImGui_Input_Impl_Direct imguiInput;

        public delegate void BuildUIDelegate();
        public delegate void NewInputFrameDelegate();
        public delegate void NewRenderFrameDelegate();

        /// <summary>
        /// User methods invoked every ImGui frame to construct custom UIs.
        /// </summary>
        public BuildUIDelegate OnBuildUI;

        public NewInputFrameDelegate OnNewInputFrame;
        public NewRenderFrameDelegate OnNewRenderFrame;

        private string imguiIniPath = null;
        public string ImGuiIniPath
        {
            get { return imguiIniPath; }
            set
            {
                imguiIniPath = value;
                imguiInput.SetIniPath(imguiIniPath);
            }
        }

        public RawDX11Scene(IntPtr nativeSwapChain)
        {
            this.SwapChain = new SwapChain(nativeSwapChain);
            this.Device = SwapChain.GetDevice<Device>();

            Initialize();
        }

        // This ctor will work fine, but it's only usefulness over using just the swapchain version
        // is that this one will allow you to pass a different device than the swapchain.GetDevice() would
        // return.  This is mostly useful for render debugging, where the real d3ddevice is hooked and
        // where we would like all our work to be done on that hooked device.
        // Because we generally will get the swapchain from the internal present() call, we are getting
        // the real d3d swapchain and not a hooked version, so GetDevice() will correspondingly return
        // the read device and not a hooked verison.
        // By passing in the hooked version explicitly here, we can mostly play nice with debug tools
        public RawDX11Scene(IntPtr nativeDevice, IntPtr nativeSwapChain)
        {
            this.Device = new Device(nativeDevice);
            this.SwapChain = new SwapChain(nativeSwapChain);

            Initialize();
        }

        private void Initialize()
        {
            this.deviceContext = this.Device.ImmediateContext;

            using (var backbuffer = this.SwapChain.GetBackBuffer<Texture2D>(0))
            {
                this.rtv = new RenderTargetView(this.Device, backbuffer);
            }

            // could also do things with GetClientRect() for WindowHandlePtr, not sure if that is necessary
            this.targetWidth = this.SwapChain.Description.ModeDescription.Width;
            this.targetHeight = this.SwapChain.Description.ModeDescription.Height;

            this.WindowHandlePtr = this.SwapChain.Description.OutputHandle;

            InitializeImGui();
        }

        private void InitializeImGui()
        {
            this.imguiRenderer = new ImGui_Impl_DX11();

            var ctx = ImGui.CreateContext();
            ImGuizmo.SetImGuiContext(ctx);
            ImPlot.SetImGuiContext(ctx);
            ImPlot.CreateContext();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable | ImGuiConfigFlags.ViewportsEnable;

            this.imguiRenderer.Init(this.Device, this.deviceContext);
            this.imguiInput = new ImGui_Input_Impl_Direct(WindowHandlePtr);
        }

        /// <summary>
        /// Processes window messages.
        /// </summary>
        /// <param name="hWnd">Handle of the window.</param>
        /// <param name="msg">Type of window message.</param>
        /// <param name="wParam">wParam.</param>
        /// <param name="lParam">lParam.</param>
        /// <returns>Return value.</returns>
        public unsafe IntPtr? ProcessWndProcW(IntPtr hWnd, User32.WindowMessage msg, void* wParam, void* lParam) {
            return this.imguiInput.ProcessWndProcW(hWnd, msg, wParam, lParam);
        }

        public void Render()
        {
            this.deviceContext.OutputMerger.SetRenderTargets(this.rtv);

            this.imguiRenderer.NewFrame();
            this.OnNewRenderFrame?.Invoke();
            this.imguiInput.NewFrame(targetWidth, targetHeight);
            this.OnNewInputFrame?.Invoke();

            ImGui.NewFrame();
            ImGuizmo.BeginFrame();

            OnBuildUI?.Invoke();

            ImGui.Render();

            this.imguiRenderer.RenderDrawData(ImGui.GetDrawData());
            this.deviceContext.OutputMerger.SetRenderTargets((RenderTargetView)null);
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }

        public void OnPreResize()
        {
            this.deviceContext.OutputMerger.SetRenderTargets((RenderTargetView)null);

            this.rtv?.Dispose();
            this.rtv = null;
        }

        public void OnPostResize(int newWidth, int newHeight)
        {
            using (var backbuffer = this.SwapChain.GetBackBuffer<Texture2D>(0))
            {
                this.rtv = new RenderTargetView(this.Device, backbuffer);
            }

            this.targetWidth = newWidth;
            this.targetHeight = newHeight;
        }

        // It is pretty much required that this is called from a handler attached
        // to OnNewRenderFrame
        public void InvalidateFonts()
        {
            this.imguiRenderer.RebuildFontTexture();
        }
        
        // It is pretty much required that this is called from a handler attached
        // to OnNewRenderFrame
        public void ClearStacksOnContext() {
            Custom.igCustom_ClearStacks();
        }
        
        public bool IsImGuiCursor(IntPtr hCursor)
        {
            return this.imguiInput.IsImGuiCursor(hCursor);
        }

        public IDalamudTextureWrap LoadImage(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                fs.CopyTo(ms);
                var image = Stbi.LoadFromMemory(ms, 4);
                return LoadImage_Internal(image);
            }
        }

        public IDalamudTextureWrap LoadImage(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length, false, true))
            {
                var image = Stbi.LoadFromMemory(ms, 4);
                return LoadImage_Internal(image);
            }
        }

        public unsafe IDalamudTextureWrap LoadImageRaw(byte[] imageData, int width, int height, int numChannels = 4)
        {
            // StbiSharp doesn't expose a constructor, even just to wrap existing data, which means
            // short of something awful like below, or creating another wrapper layer, we can't avoid
            // adding divergent code paths into CreateTexture
            //var mock = new { Width = width, Height = height, NumChannels = numChannels, Data = imageData };
            //var image = Unsafe.As<StbiImage>(mock);
            //return LoadImage_Internal(image);

            fixed (void* pixelData = imageData)
            {
                return CreateTexture(pixelData, width, height, numChannels);
            }
        }

        private unsafe IDalamudTextureWrap LoadImage_Internal(StbiImage image)
        {
            fixed (void* pixelData = image.Data)
            {
                return CreateTexture(pixelData, image.Width, image.Height, image.NumChannels);
            }
        }

        private unsafe IDalamudTextureWrap CreateTexture(void* pixelData, int width, int height, int bytesPerPixel)
        {
            ShaderResourceView resView = null;

            var texDesc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            using (var texture = new Texture2D(this.Device, texDesc, new DataRectangle(new IntPtr(pixelData), width * bytesPerPixel)))
            {
                resView = new ShaderResourceView(this.Device, texture, new ShaderResourceViewDescription
                {
                    Format = texDesc.Format,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = { MipLevels = texDesc.MipLevels }
                });
            }

            // no sampler for now because the ImGui implementation we copied doesn't allow for changing it

            return new D3DTextureWrap(resView, width, height);
        }

        public byte[] CaptureScreenshot()
        {
            using (var backBuffer = this.SwapChain.GetBackBuffer<Texture2D>(0))
            {
                Texture2DDescription desc = backBuffer.Description;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;
                desc.OptionFlags = ResourceOptionFlags.None;
                desc.BindFlags = BindFlags.None;

                using (var tex = new Texture2D(this.Device, desc))
                {
                    this.deviceContext.CopyResource(backBuffer, tex);
                    using (var surf = tex.QueryInterface<Surface>())
                    {
                        var map = surf.Map(SharpDX.DXGI.MapFlags.Read, out DataStream dataStream);
                        var pixelData = new byte[surf.Description.Width * surf.Description.Height * surf.Description.Format.SizeOfInBytes()];
                        var dataCounter = 0;

                        while (dataCounter < pixelData.Length)
                        {
                            //var curPixel = dataStream.Read<uint>();
                            var x = dataStream.Read<byte>();
                            var y = dataStream.Read<byte>();
                            var z = dataStream.Read<byte>();
                            var w = dataStream.Read<byte>();

                            pixelData[dataCounter++] = z;
                            pixelData[dataCounter++] = y;
                            pixelData[dataCounter++] = x;
                            pixelData[dataCounter++] = w;
                        }

                        // TODO: test this on a thread
                        //var gch = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
                        //using (var bitmap = new Bitmap(surf.Description.Width, surf.Description.Height, map.Pitch, PixelFormat.Format32bppRgb, gch.AddrOfPinnedObject()))
                        //{
                        //    bitmap.Save(path);
                        //}
                        //gch.Free();

                        surf.Unmap();
                        dataStream.Dispose();

                        return pixelData;
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                this.imguiRenderer?.Shutdown();
                this.imguiInput?.Dispose();

                ImGui.DestroyContext();

                this.rtv.Dispose();

                // Not actually sure how sharpdx does ref management, but hopefully they
                // addref when we create our wrappers, so this should just release that count

                // Originally it was thought these lines were needed because it was assumed that SharpDX does
                // proper refcounting to handle disposing, but disposing these would cause the game to crash
                // on resizing after unloading Dalamud
                // this.SwapChain?.Dispose();
                // this.deviceContext?.Dispose();
                // this.Device?.Dispose();

                disposedValue = true;
            }
        }

        ~RawDX11Scene()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
