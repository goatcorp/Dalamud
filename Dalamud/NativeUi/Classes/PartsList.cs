using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.NativeUi.Extensions;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Managed AtkUldPartsList. For storing and managing multiple AtkUldParts'.
/// </summary>
internal unsafe class PartsList : IDisposable
{
    private bool isDisposed;
    private uint partCapacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartsList"/> class.
    /// </summary>
    public PartsList()
    {
        this.InternalPartsList = (AtkUldPartsList*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPartsList), 8);

        this.InternalPartsList->Parts = null;
        this.InternalPartsList->PartCount = 0;
        this.InternalPartsList->Id = 0;
    }

    /// <summary>
    /// Gets internally exposed pointer to the contained PartsList.
    /// </summary>
    internal AtkUldPartsList* InternalPartsList { get; private set; }

    private uint PartCount
    {
        get => this.InternalPartsList->PartCount;
        set => this.InternalPartsList->PartCount = value;
    }

    /// <summary>
    /// Gets or sets an individual part by index.
    /// </summary>
    /// <param name="index">Index to extract part from.</param>
    public AtkUldPart* this[int index]
    {
        get
        {
            if (this.InternalPartsList is null) return null;
            if (this.PartCount <= index) return null;

            return &this.InternalPartsList->Parts[index];
        }
    }

    /// <summary>
    /// Add multiple parts to this PartsList.
    /// </summary>
    /// <param name="items">The parts to add.</param>
    public void Add(params Part[] items)
    {
        this.EnsureCapacity(this.PartCount + (uint)items.Length);

        foreach (var part in items)
        {
            this.AddPart(part);
        }
    }

    /// <summary>
    /// Add a single part to this PartsList.
    /// </summary>
    /// <param name="item">Item to add.</param>
    public void Add(Part item)
    {
        this.EnsureCapacity(this.PartCount + 1);

        this.AddPart(item);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this.isDisposed)
        {
            foreach (var partIndex in Enumerable.Range(0, (int)this.PartCount))
            {
                ref var part = ref this.InternalPartsList->Parts[partIndex];

                if (part.UldAsset is not null && part.UldAsset->AtkTexture.IsTextureReady())
                {
                    part.UldAsset->AtkTexture.ReleaseTexture();
                    part.UldAsset->AtkTexture.KernelTexture = null;
                    part.UldAsset->AtkTexture.TextureType = 0;
                }

                IMemorySpace.Free(part.UldAsset);
                part.UldAsset = null;
            }

            if (this.InternalPartsList->Parts is not null)
            {
                IMemorySpace.Free(this.InternalPartsList->Parts, (ulong)sizeof(AtkUldPart) * this.partCapacity);
                this.InternalPartsList->Parts = null;
            }

            IMemorySpace.Free(this.InternalPartsList);
            this.InternalPartsList = null;
            this.partCapacity = 0;
        }

        this.isDisposed = true;
    }

    private void EnsureCapacity(uint capacity)
    {
        if (this.partCapacity >= capacity) return;

        var newCapacity = this.partCapacity is 0 ? 4U : this.partCapacity;

        while (newCapacity < capacity)
        {
            if (newCapacity > uint.MaxValue / 2)
            {
                newCapacity = capacity;
                break;
            }

            newCapacity *= 2;
        }

        var newBuffer = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart) * newCapacity, 8);

        if (this.InternalPartsList->Parts is not null)
        {
            NativeMemory.Copy(this.InternalPartsList->Parts, newBuffer, (nuint)(sizeof(AtkUldPart) * this.PartCount));
            IMemorySpace.Free(this.InternalPartsList->Parts, (ulong)sizeof(AtkUldPart) * this.partCapacity);
        }

        this.InternalPartsList->Parts = newBuffer;
        this.partCapacity = newCapacity;
    }

    private void AddPart(Part item)
    {
        ref var newPart = ref this.InternalPartsList->Parts[this.PartCount];

        newPart.Width = (ushort)item.Width;
        newPart.Height = (ushort)item.Height;
        newPart.U = (ushort)item.U;
        newPart.V = (ushort)item.V;

        newPart.UldAsset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldAsset), 8);
        newPart.UldAsset->Id = item.Id;
        newPart.UldAsset->AtkTexture.Ctor();
        newPart.LoadTexture(item.TexturePath);

        this.PartCount++;
    }
}
