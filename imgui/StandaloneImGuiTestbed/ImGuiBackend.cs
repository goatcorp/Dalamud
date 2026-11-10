using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;

using Veldrid;
using Veldrid.Sdl2;

namespace StandaloneImGuiTestbed;

public class ImGuiBackend : IDisposable
{
 private GraphicsDevice gd;
    private bool frameBegun;

    // Veldrid objects
    private DeviceBuffer? vertexBuffer;
    private DeviceBuffer? indexBuffer;
    private DeviceBuffer? projMatrixBuffer;
    private Texture? fontTexture;
    private TextureView? fontTextureView;
    private Shader? vertexShader;
    private Shader? fragmentShader;
    private ResourceLayout? layout;
    private ResourceLayout? textureLayout;
    private Pipeline? pipeline;
    private ResourceSet? mainResourceSet;
    private ResourceSet? fontTextureResourceSet;

    private readonly IntPtr fontAtlasId = (IntPtr)1;
    private bool controlDown;
    private bool shiftDown;
    private bool altDown;
    private bool winKeyDown;

    private IntPtr iniPathPtr;

    private int windowWidth;
    private int windowHeight;
    private Vector2 scaleFactor = Vector2.One;

    // Image trackers
    private readonly Dictionary<TextureView, ResourceSetInfo> setsByView = new();

    private readonly Dictionary<Texture, TextureView> autoViewsByTexture = new();

    private readonly Dictionary<IntPtr, ResourceSetInfo> viewsById = new Dictionary<IntPtr, ResourceSetInfo>();
    private readonly List<IDisposable> ownedResources = new List<IDisposable>();
    private int lastAssignedID = 100;

    private delegate void SetClipboardTextDelegate(IntPtr userData, string text);

    private delegate string GetClipboardTextDelegate();

    // variables because they need to exist for the program duration without being gc'd
    private SetClipboardTextDelegate setText;
    private GetClipboardTextDelegate getText;

    /// <summary>
    /// Constructs a new ImGuiBackend.
    /// </summary>
    public unsafe ImGuiBackend(GraphicsDevice gd, OutputDescription outputDescription, int width, int height, FileInfo iniPath, float fontPxSize)
    {
        this.gd = gd;
        windowWidth = width;
        windowHeight = height;

        ImGui.CreateContext();

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.HasGamepad;

        SetIniPath(iniPath.FullName);

        setText = SetClipboardText;
        getText = GetClipboardText;

        var io = ImGui.GetIO();
        io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(setText).ToPointer();
        io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(getText).ToPointer();
        io.ClipboardUserData = null;

        CreateDeviceResources(gd, outputDescription, fontPxSize);
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
        frameBegun = true;
    }

    private void SetIniPath(string iniPath)
    {
        if (iniPathPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(iniPathPtr);
        }

        iniPathPtr = Marshal.StringToHGlobalAnsi(iniPath);

        unsafe
        {
            var io = ImGui.GetIO();
            io.IniFilename = (byte*)iniPathPtr.ToPointer();
        }
    }

    private static void SetClipboardText(IntPtr userData, string text)
    {
        // text always seems to have an extra newline, but I'll leave it for now
        Sdl2Native.SDL_SetClipboardText(text);
    }

    private static string GetClipboardText()
    {
        return Sdl2Native.SDL_GetClipboardText();
    }

    public void WindowResized(int width, int height)
    {
        windowWidth = width;
        windowHeight = height;
    }

    public void DestroyDeviceObjects()
    {
        Dispose();
    }

    public void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription, float fontPxSize)
    {
        this.gd = gd;
        var factory = gd.ResourceFactory;
        vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        vertexBuffer.Name = "ImGui.NET Vertex Buffer";
        indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        indexBuffer.Name = "ImGui.NET Index Buffer";

        var ioFonts = ImGui.GetIO().Fonts;

        ImGui.GetIO().Fonts.Clear();
        ImGui.GetIO().Fonts.AddFontDefault();
        ImGui.GetIO().Fonts.Build();

        RecreateFontDeviceTexture(gd);

        projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

        var vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex);
        var fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag", ShaderStages.Fragment);
        vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
        fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));

        var vertexLayouts = new VertexLayoutDescription[]
        {
            new VertexLayoutDescription(
                new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
        };

        layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

        var pd = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(vertexLayouts, new[] { vertexShader, fragmentShader }),
            new ResourceLayout[] { layout, textureLayout },
            outputDescription,
            ResourceBindingModel.Default);
        pipeline = factory.CreateGraphicsPipeline(ref pd);

        mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(layout,
            projMatrixBuffer,
            gd.PointSampler));

        fontTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, fontTextureView));
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
    {
        if (!setsByView.TryGetValue(textureView, out var rsi))
        {
            var resourceSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, textureView));
            rsi = new ResourceSetInfo(this.GetNextImGuiBindingId(), resourceSet);

            setsByView.Add(textureView, rsi);
            viewsById.Add(rsi.ImGuiBinding, rsi);
            ownedResources.Add(resourceSet);
        }

        return rsi.ImGuiBinding;
    }

    private IntPtr GetNextImGuiBindingId()
    {
        var newId = lastAssignedID++;
        return (IntPtr)newId;
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
        if (!autoViewsByTexture.TryGetValue(texture, out var textureView))
        {
            textureView = factory.CreateTextureView(texture);
            autoViewsByTexture.Add(texture, textureView);
            ownedResources.Add(textureView);
        }

        return GetOrCreateImGuiBinding(factory, textureView);
    }

    /// <summary>
    /// Retrieves the shader texture binding for the given helper handle.
    /// </summary>
    public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
    {
        if (!viewsById.TryGetValue(imGuiBinding, out var tvi))
        {
            throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
        }

        return tvi.ResourceSet;
    }

    public void ClearCachedImageResources()
    {
        foreach (var resource in ownedResources)
        {
            resource.Dispose();
        }

        ownedResources.Clear();
        setsByView.Clear();
        viewsById.Clear();
        autoViewsByTexture.Clear();
        lastAssignedID = 100;
    }

    public static byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        var assembly = typeof(ImGuiBackend).Assembly;

        using var s = assembly.GetManifestResourceStream(resourceName);
        if (s == null)
            throw new ArgumentException($"Resource {resourceName} not found", nameof(resourceName));

        var ret = new byte[s.Length];
        s.ReadExactly(ret, 0, (int)s.Length);
        return ret;
    }

    private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
    {
        switch (factory.BackendType)
        {
            case GraphicsBackend.Direct3D11:
                {
                    var resourceName = name + ".hlsl.bytes";
                    return GetEmbeddedResourceBytes(resourceName);
                }

            case GraphicsBackend.OpenGL:
                {
                    var resourceName = name + ".glsl";
                    return GetEmbeddedResourceBytes(resourceName);
                }

            case GraphicsBackend.Vulkan:
                {
                    var resourceName = name + ".spv";
                    return GetEmbeddedResourceBytes(resourceName);
                }

            case GraphicsBackend.Metal:
                {
                    var resourceName = name + ".metallib";
                    return GetEmbeddedResourceBytes(resourceName);
                }

            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public unsafe void RecreateFontDeviceTexture(GraphicsDevice gd)
    {
        var io = ImGui.GetIO();
        // Build
        byte* pixels = null;
        int width, height, bytesPerPixel;
        io.Fonts.GetTexDataAsRGBA32(0, &pixels, &width, &height, &bytesPerPixel);
        // Store our identifier
        io.Fonts.SetTexID(0, new ImTextureID(this.fontAtlasId));

        fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
        fontTexture.Name = "ImGui.NET Font Texture";
        gd.UpdateTexture(
            fontTexture,
            new IntPtr(pixels),
            (uint)(bytesPerPixel * width * height),
            0,
            0,
            0,
            (uint)width,
            (uint)height,
            1,
            0,
            0);
        fontTextureView = gd.ResourceFactory.CreateTextureView(fontTexture);

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
    /// or index data has increased beyond the capacity of the existing buffers.
    /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
    /// </summary>
    public void Render(GraphicsDevice gd, CommandList cl)
    {
        if (frameBegun)
        {
            frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData(), gd, cl);
        }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds, InputSnapshot snapshot)
    {
        if (frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput(snapshot);

        frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(
            windowWidth / scaleFactor.X,
            windowHeight / scaleFactor.Y);
        io.DisplayFramebufferScale = scaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    private void UpdateImGuiInput(InputSnapshot snapshot)
    {
        var io = ImGui.GetIO();

        var mousePosition = snapshot.MousePosition;

        // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
        var leftPressed = false;
        var middlePressed = false;
        var rightPressed = false;

        foreach (var me in snapshot.MouseEvents)
        {
            if (me.Down)
            {
                switch (me.MouseButton)
                {
                    case MouseButton.Left:
                        leftPressed = true;
                        break;

                    case MouseButton.Middle:
                        middlePressed = true;
                        break;

                    case MouseButton.Right:
                        rightPressed = true;
                        break;
                }
            }
        }

        io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
        io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
        io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
        io.MousePos = mousePosition;
        io.MouseWheel = snapshot.WheelDelta;

        var keyCharPresses = snapshot.KeyCharPresses;

        for (var i = 0; i < keyCharPresses.Count; i++)
        {
            var c = keyCharPresses[i];
            io.AddInputCharacter(c);
        }

        var keyEvents = snapshot.KeyEvents;

        for (var i = 0; i < keyEvents.Count; i++)
        {
            var keyEvent = keyEvents[i];
            io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;

            if (keyEvent.Key == Key.ControlLeft)
            {
                controlDown = keyEvent.Down;
            }

            if (keyEvent.Key == Key.ShiftLeft)
            {
                shiftDown = keyEvent.Down;
            }

            if (keyEvent.Key == Key.AltLeft)
            {
                altDown = keyEvent.Down;
            }

            if (keyEvent.Key == Key.WinLeft)
            {
                winKeyDown = keyEvent.Down;
            }
        }

        io.KeyCtrl = controlDown;
        io.KeyAlt = altDown;
        io.KeyShift = shiftDown;
        io.KeySuper = winKeyDown;
    }

    private static void SetKeyMappings()
    {
        var io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
        io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
        io.KeyMap[(int)ImGuiKey.KeypadEnter] = (int)Key.KeypadEnter;
        io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr drawData, GraphicsDevice gd, CommandList cl)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (drawData.CmdListsCount == 0)
        {
            return;
        }

        var totalVbSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());

        if (totalVbSize > vertexBuffer!.SizeInBytes)
        {
            gd.DisposeWhenIdle(vertexBuffer);
            vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVbSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        var totalIbSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));

        if (totalIbSize > indexBuffer!.SizeInBytes)
        {
            gd.DisposeWhenIdle(indexBuffer);
            indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIbSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[i];

            cl.UpdateBuffer(
                vertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                new IntPtr(cmdList.VtxBuffer.Data),
                (uint)(cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            cl.UpdateBuffer(
                indexBuffer,
                indexOffsetInElements * sizeof(ushort),
                new IntPtr(cmdList.IdxBuffer.Data),
                (uint)(cmdList.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmdList.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmdList.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        var io = ImGui.GetIO();
        var mvp = Matrix4x4.CreateOrthographicOffCenter(
            0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        this.gd.UpdateBuffer(this.projMatrixBuffer, 0, ref mvp);

        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(pipeline);
        cl.SetGraphicsResourceSet(0, mainResourceSet);

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        // Render command lists
        var globalIdxOffset = 0;
        var globalVtxOffset = 0;

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (var cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                var pcmd = cmdList.CmdBuffer[cmdI];

                if (pcmd.UserCallback != null)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (pcmd.TextureId != IntPtr.Zero)
                    {
                        if (pcmd.TextureId == this.fontAtlasId)
                        {
                            cl.SetGraphicsResourceSet(1, fontTextureResourceSet);
                        }
                        else
                        {
                            cl.SetGraphicsResourceSet(1, GetImageResourceSet((nint)pcmd.TextureId.Handle));
                        }
                    }

                    cl.SetScissorRect(
                        0,
                        (uint)pcmd.ClipRect.X,
                        (uint)pcmd.ClipRect.Y,
                        (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                    cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)globalIdxOffset, (int)pcmd.VtxOffset + globalVtxOffset, 0);
                }
            }

            globalIdxOffset += cmdList.IdxBuffer.Size;
            globalVtxOffset += cmdList.VtxBuffer.Size;
        }
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        projMatrixBuffer?.Dispose();
        fontTexture?.Dispose();
        fontTextureView?.Dispose();
        vertexShader?.Dispose();
        fragmentShader?.Dispose();
        layout?.Dispose();
        textureLayout?.Dispose();
        pipeline?.Dispose();
        mainResourceSet?.Dispose();

        foreach (var resource in ownedResources)
        {
            resource.Dispose();
        }
    }

    private struct ResourceSetInfo
    {
        public readonly IntPtr ImGuiBinding;
        public readonly ResourceSet ResourceSet;

        public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
        {
            ImGuiBinding = imGuiBinding;
            ResourceSet = resourceSet;
        }
    }
}
