using System;
using System.IO;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Lumina.Data.Files;
using SharpDX;
using SharpDX.Direct3D11;
using Vector2 = System.Numerics.Vector2;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// Class responsible for drawing the main plugin window.
    /// </summary>
    internal class PluginWindow : Window, IDisposable
    {
        private readonly UiBuilder uiBuilder;
        private readonly ITextureProvider textureProvider;
        private readonly IDalamudTextureWrap tex;
        private readonly IntPtr callbackId;
        private readonly Device device;
        private readonly DeviceContext deviceContext;
        private readonly PixelShader pixelShader;
        private readonly SamplerState fontSampler;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        public PluginWindow(UiBuilder uiBuilder, IDataManager dataManager, ITextureProvider textureProvider)
            : base("CorePlugin")
        {
            this.uiBuilder = uiBuilder;
            this.textureProvider = textureProvider;
            this.IsOpen = true;

            this.Size = new Vector2(810, 520);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.tex = this.textureProvider.GetTexture(dataManager.GetFile<TexFile>("chara/monster/m0361/obj/body/b0001/texture/v01_m0361b0001_n.tex")!);
            this.callbackId = this.uiBuilder.AddImGuiDrawCmdUserCallback(this.DrawCmdUserCallback);
            this.device = CppObject.FromPointer<Device>(uiBuilder.DeviceNativePointer);
            this.deviceContext = CppObject.FromPointer<DeviceContext>(uiBuilder.DeviceContextNativePointer);
            this.pixelShader = new PixelShader(this.device, File.ReadAllBytes(@"Z:\test.fxc"));
            this.fontSampler = new SamplerState(this.device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLodBias = 0,
                ComparisonFunction = Comparison.Always,
                MinimumLod = 0,
                MaximumLod = 0,
            });
        }

        private void DrawCmdUserCallback(ImDrawDataPtr drawData, ImDrawCmdPtr drawCmd)
        {
            this.deviceContext.PixelShader.Set(this.pixelShader);
            this.deviceContext.PixelShader.SetSampler(0, this.fontSampler);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.uiBuilder.RemoveImGuiDrawCmdUserCallback(this.DrawCmdUserCallback);
            this.tex.Dispose();
            this.pixelShader.Dispose();
            this.fontSampler.Dispose();
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddCallback(this.callbackId, nint.Zero);
            ImGui.Image(this.tex.ImGuiHandle, new(512, 512), new(1, 0), new(2, 1));
            ImGui.SameLine();
            ImGui.Image(this.tex.ImGuiHandle, new(512, 512), new(2, 0), new(3, 1));
            ImGui.Image(this.tex.ImGuiHandle, new(512, 512), new(3, 0), new(4, 1));
            ImGui.SameLine();
            ImGui.Image(this.tex.ImGuiHandle, new(512, 512), new(4, 0), new(5, 1));
            drawList.AddCallback(this.uiBuilder.ImGuiResetDrawCmdUserCallback, nint.Zero);
        }
    }
}
