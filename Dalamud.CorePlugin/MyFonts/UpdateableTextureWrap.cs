#nullable enable
using System;
using System.Linq;

using Dalamud.Interface.Internal;
using Dalamud.Utility;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Dalamud.CorePlugin.MyFonts;

internal sealed unsafe class UpdateableTextureWrap : IDalamudTextureWrap, InterfaceManager.IDeferredDisposable
{
    private readonly DisposeStack waste = new();

    public UpdateableTextureWrap(Device device, nint pixels, int width, int height)
    {
        this.Width = width;
        this.Height = height;
        try
        {
            this.Data = new byte[width * height * 4];
            if (pixels != 0)
                new Span<byte>((void*)pixels, width * height * 4).CopyTo(this.Data);

            this.Device = this.waste.Add(device.QueryInterface<Device>());

            fixed (void* d = this.Data)
            {
                this.Texture = this.waste.Add(
                    new Texture2D(
                        device,
                        new()
                        {
                            Width = width,
                            Height = height,
                            MipLevels = 1,
                            ArraySize = 1,
                            Format = Format.B8G8R8A8_UNorm,
                            SampleDescription = new(1, 0),
                            Usage = ResourceUsage.Dynamic,
                            BindFlags = BindFlags.ShaderResource,
                            CpuAccessFlags = CpuAccessFlags.Write,
                            OptionFlags = ResourceOptionFlags.None,
                        },
                        new DataRectangle((nint)d, width * 4)));
            }

            this.View = this.waste.Add(
                new ShaderResourceView(
                    device,
                    this.Texture,
                    new()
                    {
                        Format = Format.B8G8R8A8_UNorm,
                        Dimension = ShaderResourceViewDimension.Texture2D,
                        Texture2D = { MipLevels = 1 },
                    }));

            this.Packers = new[]
            {
                new Packer(width - 1, height - 1),
                new Packer(width - 1, height - 1),
                new Packer(width - 1, height - 1),
                new Packer(width - 1, height - 1),
            };
        }
        catch (Exception)
        {
            this.waste.Dispose();
            throw;
        }
    }

    public bool Immutable => !this.Data.Any();

    public byte[] Data { get; }

    public ShaderResourceView View { get; }

    public Texture2D Texture { get; }

    public Packer[] Packers { get; }

    /// <inheritdoc/>
    public IntPtr ImGuiHandle => this.View.NativePointer;

    /// <inheritdoc/>
    public int Width { get; }

    /// <inheritdoc/>
    public int Height { get; }

    private Device Device { get; }

    public void ApplyChanges()
    {
        var box = this.Device.ImmediateContext.MapSubresource(
            this.Texture,
            0,
            MapMode.WriteDiscard,
            MapFlags.None);
        this.Data.AsSpan().CopyTo(new((void*)box.DataPointer, this.Data.Length));
        this.Device.ImmediateContext.UnmapSubresource(this.Texture, 0);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Service<InterfaceManager>.GetNullable() is { } im)
            im.EnqueueDeferredDispose(this);
        else
            this.RealDispose();
    }

    /// <inheritdoc/>
    public void RealDispose() => this.waste.Dispose();
}
