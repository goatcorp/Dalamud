using CheapLoc;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Internal;

/// <summary>
/// This class implements in-game Dalamud options in the in-game System menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class TitleScreenVersionInfo : IInternalDisposableService
{
    private static readonly ModuleLog Log = ModuleLog.Create<TitleScreenVersionInfo>();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    private readonly AddonLifecycleEventListener versionStringListener;
    private int lastLoadedPluginCount = -1;

    private bool disposed = false;

    [ServiceManager.ServiceConstructor]
    private TitleScreenVersionInfo()
    {
        this.versionStringListener = new AddonLifecycleEventListener(AddonEvent.PreDraw, "_TitleRevision", this.OnVersionStringDraw);

        this.addonLifecycle.RegisterListener(this.versionStringListener);
    }

    /// <summary>Finalizes an instance of the <see cref="TitleScreenVersionInfo"/> class.</summary>
    ~TitleScreenVersionInfo() => this.Dispose(false);

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.Dispose(true);

    private void Dispose(bool disposing)
    {
        if (this.disposed)
            return;

        if (disposing)
        {
            this.addonLifecycle.UnregisterListener(this.versionStringListener);

            var addonPtr = this.gameGui.GetAddonByName("_TitleRevision");
            if (!addonPtr.IsNull && addonPtr.IsReady)
            {
                var addon = addonPtr.Struct;
                var textNode = addon->GetTextNodeById(3);

                var containsDalamudVersionString = textNode->OriginalTextPointer.Value == textNode->NodeText.StringPtr.Value;
                if (containsDalamudVersionString)
                    textNode->SetText(addon->AtkValues[1].String);

                this.lastLoadedPluginCount = -1;
            }
        }

        this.disposed = true;
    }

    private void OnVersionStringDraw(AddonEvent ev, AddonArgs args)
    {
        if (ev is not (AddonEvent.PostDraw or AddonEvent.PreDraw)) return;

        var addon = args.Addon.Struct;
        var textNode = addon->GetTextNodeById(3);

        // look and feel init. should be harmless to set.
        textNode->TextFlags |= TextFlags.MultiLine;
        textNode->AlignmentType = AlignmentType.TopLeft;

        var containsDalamudVersionString = textNode->OriginalTextPointer.Value == textNode->NodeText.StringPtr.Value;
        if (!this.configuration.ShowTsm)
        {
            if (containsDalamudVersionString)
                textNode->SetText(addon->AtkValues[1].String);
            this.lastLoadedPluginCount = -1;
            return;
        }

        var pm = Service<PluginManager>.GetNullable();
        var count = pm?.LoadedPluginCount ?? 0;

        // Avoid rebuilding the string every frame.
        if (containsDalamudVersionString && count == this.lastLoadedPluginCount)
            return;
        this.lastLoadedPluginCount = count;

        using var rssb = new RentedSeStringBuilder();

        rssb.Builder
            .Append(new ReadOnlySeStringSpan(addon->AtkValues[1].String.Value))
            .Append("\n\n")
            .PushEdgeColorType(701)
            .PushColorType(539)
            .Append(SeIconChar.BoxedLetterD.ToIconChar())
            .PopColorType()
            .PopEdgeColorType()
            .Append($" Dalamud: {Versioning.GetScmVersion()}")
            .Append($" - {count} {(count != 1 ? "plugins" : "plugin")} loaded");

        if (pm?.SafeMode is true)
            rssb.Builder.PushColorType(17).Append(" [SAFE MODE]").PopColorType();

        textNode->SetText(rssb.Builder.GetViewAsSpan());
    }
}
