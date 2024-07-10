using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// Class used to modify the data used when rendering nameplates.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class NamePlateGui : IInternalDisposableService, INamePlateGui
{
    /// <summary>
    /// The index for the number array used by the NamePlate addon.
    /// </summary>
    public const int NumberArrayIndex = 5;

    /// <summary>
    /// The index for the string array used by the NamePlate addon.
    /// </summary>
    public const int StringArrayIndex = 4;

    /// <summary>
    /// The index for of the FullUpdate entry in the NamePlate number array.
    /// </summary>
    internal const int NumberArrayFullUpdateIndex = 4;

    /// <summary>
    /// An empty null-terminated string pointer allocated in unmanaged memory, used to tag removed fields.
    /// </summary>
    internal static readonly nint EmptyStringPointer = CreateEmptyStringPointer();

    [ServiceManager.ServiceDependency]
    private readonly AddonLifecycle addonLifecycle = Service<AddonLifecycle>.Get();

    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    private readonly AddonLifecycleEventListener preRequestedUpdateListener;

    private NamePlateUpdateContext? context;

    private NamePlateUpdateHandler[] updateHandlers = [];

    [ServiceManager.ServiceConstructor]
    private NamePlateGui()
    {
        this.preRequestedUpdateListener = new AddonLifecycleEventListener(
            AddonEvent.PreRequestedUpdate,
            "NamePlate",
            this.OnPreRequestedUpdate);

        this.addonLifecycle.RegisterListener(this.preRequestedUpdateListener);
    }

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnNamePlateUpdate;

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnDataUpdate;

    /// <inheritdoc/>
    public unsafe void RequestRedraw()
    {
        var addon = this.gameGui.GetAddonByName("NamePlate");
        if (addon != 0)
        {
            var raptureAtkModule = RaptureAtkModule.Instance();
            if (raptureAtkModule == null)
            {
                return;
            }

            ((AddonNamePlate*)addon)->DoFullUpdate = 1;
            var namePlateNumberArrayData = raptureAtkModule->AtkArrayDataHolder.NumberArrays[NumberArrayIndex];
            namePlateNumberArrayData->SetValue(NumberArrayFullUpdateIndex, 1);
        }
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.addonLifecycle.UnregisterListener(this.preRequestedUpdateListener);
    }

    /// <summary>
    /// Strips the surrounding quotes from a free company tag. If the quotes are not present in the expected location,
    /// no modifications will be made.
    /// </summary>
    /// <param name="text">A quoted free company tag.</param>
    /// <returns>A span containing the free company tag without its surrounding quote characters.</returns>
    internal static ReadOnlySpan<byte> StripFreeCompanyTagQuotes(ReadOnlySpan<byte> text)
    {
        if (text.Length > 4 && text[..3].SequenceEqual(" «"u8) && text[^2..].SequenceEqual("»"u8))
        {
            return text[3..^2];
        }

        return text;
    }

    /// <summary>
    /// Strips the surrounding quotes from a title. If the quotes are not present in the expected location, no
    /// modifications will be made.
    /// </summary>
    /// <param name="text">A quoted title.</param>
    /// <returns>A span containing the title without its surrounding quote characters.</returns>
    internal static ReadOnlySpan<byte> StripTitleQuotes(ReadOnlySpan<byte> text)
    {
        if (text.Length > 5 && text[..3].SequenceEqual("《"u8) && text[^3..].SequenceEqual("》"u8))
        {
            return text[3..^3];
        }

        return text;
    }

    private static nint CreateEmptyStringPointer()
    {
        var pointer = Marshal.AllocHGlobal(1);
        Marshal.WriteByte(pointer, 0, 0);
        return pointer;
    }

    private void CreateHandlers(NamePlateUpdateContext createdContext)
    {
        var handlers = new List<NamePlateUpdateHandler>();
        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++)
        {
            handlers.Add(new NamePlateUpdateHandler(createdContext, i));
        }

        this.updateHandlers = handlers.ToArray();
    }

    private void OnPreRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (this.OnDataUpdate == null && this.OnNamePlateUpdate == null)
        {
            return;
        }

        var reqArgs = (AddonRequestedUpdateArgs)args;
        if (this.context == null)
        {
            this.context = new NamePlateUpdateContext(this.objectTable, reqArgs);
            this.CreateHandlers(this.context);
        }
        else
        {
            this.context.ResetState(reqArgs);
        }

        var activeNamePlateCount = this.context.ActiveNamePlateCount;
        if (activeNamePlateCount == 0)
            return;

        var activeHandlers = this.updateHandlers[..activeNamePlateCount];

        if (this.context.IsFullUpdate)
        {
            foreach (var handler in activeHandlers)
            {
                handler.ResetState();
            }

            this.OnDataUpdate?.Invoke(this.context, activeHandlers);
            this.OnNamePlateUpdate?.Invoke(this.context, activeHandlers);
            if (this.context.HasParts)
                this.ApplyBuilders(activeHandlers);
        }
        else
        {
            var udpatedHandlers = new List<NamePlateUpdateHandler>(activeNamePlateCount);
            foreach (var handler in activeHandlers)
            {
                handler.ResetState();
                if (handler.IsUpdating)
                    udpatedHandlers.Add(handler);
            }

            if (this.OnDataUpdate is not null)
            {
                this.OnDataUpdate?.Invoke(this.context, activeHandlers);
                this.OnNamePlateUpdate?.Invoke(this.context, udpatedHandlers);
                if (this.context.HasParts)
                    this.ApplyBuilders(activeHandlers);
            }
            else if (udpatedHandlers.Count != 0)
            {
                var changedHandlersSpan = udpatedHandlers.ToArray().AsSpan();
                this.OnNamePlateUpdate?.Invoke(this.context, udpatedHandlers);
                if (this.context.HasParts)
                    this.ApplyBuilders(changedHandlersSpan);
            }
        }
    }

    private void ApplyBuilders(Span<NamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            if (handler.PartsContainer is { } container)
            {
                container.ApplyBuilders(handler);
            }
        }
    }
}

/// <summary>
/// Plugin-scoped version of a AddonEventManager service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<INamePlateGui>]
#pragma warning restore SA1015
internal class NamePlateGuiPluginScoped : IInternalDisposableService, INamePlateGui
{
    [ServiceManager.ServiceDependency]
    private readonly NamePlateGui parentService = Service<NamePlateGui>.Get();

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnNamePlateUpdate
    {
        add
        {
            if (this.OnNamePlateUpdateScoped == null)
                this.parentService.OnNamePlateUpdate += this.OnNamePlateUpdateForward;
            this.OnNamePlateUpdateScoped += value;
        }

        remove
        {
            this.OnNamePlateUpdateScoped -= value;
            if (this.OnNamePlateUpdateScoped == null)
                this.parentService.OnNamePlateUpdate -= this.OnNamePlateUpdateForward;
        }
    }

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnDataUpdate
    {
        add
        {
            if (this.OnDataUpdateScoped == null)
                this.parentService.OnDataUpdate += this.OnDataUpdateForward;
            this.OnDataUpdateScoped += value;
        }

        remove
        {
            this.OnDataUpdateScoped -= value;
            if (this.OnDataUpdateScoped == null)
                this.parentService.OnDataUpdate -= this.OnDataUpdateForward;
        }
    }

    private event INamePlateGui.OnPlateUpdateDelegate? OnNamePlateUpdateScoped;

    private event INamePlateGui.OnPlateUpdateDelegate? OnDataUpdateScoped;

    /// <inheritdoc/>
    public void RequestRedraw()
    {
        this.parentService.RequestRedraw();
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.parentService.OnNamePlateUpdate -= this.OnNamePlateUpdateForward;
        this.OnNamePlateUpdateScoped = null;

        this.parentService.OnDataUpdate -= this.OnDataUpdateForward;
        this.OnDataUpdateScoped = null;
    }

    private void OnNamePlateUpdateForward(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        this.OnNamePlateUpdateScoped?.Invoke(context, handlers);
    }

    private void OnDataUpdateForward(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        this.OnDataUpdateScoped?.Invoke(context, handlers);
    }
}
