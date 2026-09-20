using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Nodes;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Addon;

/// <summary>
/// Implementation of a custom native addon (AtkUnitBase).
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "Suppress warning on partials, as documentation duplicates on tooltip.")]
internal unsafe partial class NativeAddon
{
    private PinnedGCHandle<NativeAddon>? disposeHandle;

    /// <summary>
    /// Gets or inits a function to be called for creating the window with a custom window node.
    /// </summary>
    public Func<WindowNodeBase>? CreateWindowNode { get; init; }

    /// <summary>
    /// Gets the root node for this NativeAddon.
    /// </summary>
    public ResNode RootNode { get; private set; } = null!;

    /// <summary>
    /// Gets a pointer to the contained addon. This is also accessible via <see cref="op_Implicit"/>.
    /// </summary>
    protected internal AtkUnitBase* InternalAddon { get; private set; }

    /// <summary>
    /// Gets the Window node for this NativeAddon. May be null if <see cref="CreateWindowNode"/> returns a null windowNode.
    /// </summary>
    protected WindowNodeBase? WindowNode { get; private set; }

    /// <summary>
    /// Converts this instance to a AtkUnitBase for seamless game interop.
    /// </summary>
    /// <param name="addon">Addon to convert.</param>
    public static implicit operator AtkUnitBase*(NativeAddon addon) => addon.InternalAddon;

    /// <summary>
    /// Triggers a window update via AtkUnitBase.SetSize(...), to have the game recalculate NodeFlags.Anchor{Direction}.
    /// </summary>
    protected void UpdateAnchoringLayout()
        => this.SetWindowSize(this.Size);

    private void AllocateAddon(uint atkValueCount = 0, AtkValue* atkValues = null)
    {
        if (this.InternalAddon is not null)
        {
            this.Log.Warning("Tried to allocate addon that was already allocated.");
            return;
        }

        var currentAddonCount = RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Count;
        if (currentAddonCount >= 200)
        {
            this.Log.Warning("WARNING: Current Addon Count is approaching hard limits ({CurrentAddonCount}/250).", currentAddonCount);
        }

        if (currentAddonCount >= 225)
        {
            this.Log.Error("ERROR: Current Addon Count is too high. Aborting allocation ({CurrentAddonCount}/250).", currentAddonCount);
            return;
        }

        if (this.InternalName.Length is 0)
        {
            throw new NullReferenceException("InternalName is empty, this is not allowed.");
        }

        this.InternalAddon = IMemorySpace.GetUISpace()->Create<AtkUnitBase>();

        this.RegisterVirtualTable();

        this.RootNode = new ResNode
        {
            NodeId = 1,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Fill,
            IsAddonRootNode = true,
        };

        if (!this.IsOverlayAddon)
        {
            this.WindowNode = this.CreateWindowNode?.Invoke() ?? new WindowNode { NodeId = 2 };
        }

        this.InternalAddon->NameString = this.InternalName;

        this.InternalAddon->ShowSoundEffectId = (short)this.OpenWindowSoundEffectId;

        this.UpdateFlags();

        this.disposeHandle = new PinnedGCHandle<NativeAddon>(this);

        var localRef = this.InternalAddon;
        using var nameString = new Utf8String(this.InternalName);

        AtkStage.Instance()->RaptureAtkUnitManager->InitializeAddon(&localRef, nameString.StringPtr, atkValueCount, atkValues);

        if (localRef is null)
        {
            this.Dispose();
            throw new Exception("Failed to initialize addon!");
        }
    }

    // Note: Commented code is regarding saving and loading addon position data.
    private void SetInitialState()
    {
        this.WindowNode?.SetTitle(this.Title.ToString(), this.Subtitle?.ToString() ?? string.Empty);

        this.InternalAddon->ShowSoundEffectId = (short)this.OpenWindowSoundEffectId;

        var dalamudConfig = Service<DalamudConfiguration>.Get();

        var hasConfigEntry = dalamudConfig.AddonConfigEntries.TryGetValue(this.InternalName, out var configEntry);
        Vector2 openPosition;

        if (hasConfigEntry && configEntry.Position != Vector2.Zero && this.RememberClosePosition)
        {
            openPosition = configEntry.Position;
        }
        else
        {
            var stage = AtkStage.Instance();
            var screenSize = new Vector2(stage->ScreenSize.Width, stage->ScreenSize.Height);
            openPosition = (screenSize - this.Size) / 2.0f;
        }

        this.InternalAddon->SetPosition((short)openPosition.X, (short)openPosition.Y);

        if (hasConfigEntry && configEntry.Scale is not 1.0f)
        {
            this.InternalAddon->SetScale(Math.Clamp(configEntry.Scale, 0.25f, 6.0f), true);
        }

        this.SetWindowSize(this.Size);

        if (this.LastClosePosition != Vector2.Zero && this.RememberClosePosition)
        {
            var clampedPosition = this.GetScreenClampedPosition(this.LastClosePosition);
            InternalAddon->SetPosition((short)clampedPosition.X, (short)clampedPosition.Y);
        }
    }
}
