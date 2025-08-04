using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Serilog;

namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// Class used to modify the data used when rendering nameplates.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed class NamePlateGui : IInternalDisposableService, INamePlateGui
{
    /// <summary>
    /// The index for of the FullUpdate entry in the NamePlate number array.
    /// </summary>
    internal const int NumberArrayFullUpdateIndex = 4;

    /// <summary>
    /// An empty null-terminated string pointer allocated in unmanaged memory, used to tag removed fields.
    /// </summary>
    internal static readonly nint EmptyStringPointer = CreateEmptyStringPointer();

    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly ObjectTable objectTable = Service<ObjectTable>.Get();

    private readonly NamePlateGuiAddressResolver address;

    private readonly Hook<AtkUnitBase.Delegates.OnRequestedUpdate> onRequestedUpdateHook;

    private NamePlateUpdateContext? context;

    private NamePlateUpdateHandler[] updateHandlers = [];

    [ServiceManager.ServiceConstructor]
    private unsafe NamePlateGui(TargetSigScanner sigScanner)
    {
        this.address = new NamePlateGuiAddressResolver();
        this.address.Setup(sigScanner);

        this.onRequestedUpdateHook = Hook<AtkUnitBase.Delegates.OnRequestedUpdate>.FromAddress(
            this.address.OnRequestedUpdate,
            this.OnRequestedUpdateDetour);
        this.onRequestedUpdateHook.Enable();
    }

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnNamePlateUpdate;

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnPostNamePlateUpdate;

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnDataUpdate;

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnPostDataUpdate;

    /// <inheritdoc/>
    public unsafe void RequestRedraw()
    {
        var addon = (AddonNamePlate*)(nint)this.gameGui.GetAddonByName("NamePlate");
        if (addon != null)
        {
            addon->DoFullUpdate = 1;
            AtkStage.Instance()->GetNumberArrayData(NumberArrayType.NamePlate)->SetValue(NumberArrayFullUpdateIndex, 1);
        }
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.onRequestedUpdateHook.Dispose();
    }

    /// <summary>
    /// Strips the surrounding quotes from a free company tag. If the quotes are not present in the expected location,
    /// no modifications will be made.
    /// </summary>
    /// <param name="text">A quoted free company tag.</param>
    /// <returns>A span containing the free company tag without its surrounding quote characters.</returns>
    internal static ReadOnlySpan<byte> StripFreeCompanyTagQuotes(ReadOnlySpan<byte> text)
    {
        if (text.Length > 4 && text.StartsWith(" «"u8) && text.EndsWith("»"u8))
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
        if (text.Length > 5 && text.StartsWith("《"u8) && text.EndsWith("》"u8))
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

    private unsafe void OnRequestedUpdateDetour(
        AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        var calledOriginal = false;

        try
        {
            if (this.OnDataUpdate == null && this.OnNamePlateUpdate == null && this.OnPostDataUpdate == null &&
                this.OnPostNamePlateUpdate == null)
            {
                return;
            }

            if (this.context == null)
            {
                this.context = new NamePlateUpdateContext(this.objectTable);
                this.CreateHandlers(this.context);
            }

            this.context.ResetState(addon, numberArrayData, stringArrayData);

            var activeNamePlateCount = this.context!.ActiveNamePlateCount;
            if (activeNamePlateCount == 0)
                return;

            var activeHandlers = this.updateHandlers[..activeNamePlateCount];

            if (this.context.IsFullUpdate)
            {
                foreach (var handler in activeHandlers)
                {
                    handler.ResetState();
                }

                this.OnDataUpdate?.InvokeSafely(this.context, activeHandlers);
                this.OnNamePlateUpdate?.InvokeSafely(this.context, activeHandlers);

                if (this.context.HasParts)
                    this.ApplyBuilders(activeHandlers);

                try
                {
                    calledOriginal = true;
                    this.onRequestedUpdateHook.Original.Invoke(addon, numberArrayData, stringArrayData);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Caught exception when calling original AddonNamePlate OnRequestedUpdate.");
                }

                this.OnPostNamePlateUpdate?.InvokeSafely(this.context, activeHandlers);
                this.OnPostDataUpdate?.InvokeSafely(this.context, activeHandlers);
            }
            else
            {
                var updatedHandlers = new List<NamePlateUpdateHandler>(activeNamePlateCount);
                foreach (var handler in activeHandlers)
                {
                    handler.ResetState();
                    if (handler.IsUpdating)
                        updatedHandlers.Add(handler);
                }

                if (this.OnDataUpdate is not null)
                {
                    this.OnDataUpdate?.InvokeSafely(this.context, activeHandlers);
                    this.OnNamePlateUpdate?.InvokeSafely(this.context, updatedHandlers);

                    if (this.context.HasParts)
                        this.ApplyBuilders(activeHandlers);

                    try
                    {
                        calledOriginal = true;
                        this.onRequestedUpdateHook.Original.Invoke(addon, numberArrayData, stringArrayData);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Caught exception when calling original AddonNamePlate OnRequestedUpdate.");
                    }

                    this.OnPostNamePlateUpdate?.InvokeSafely(this.context, updatedHandlers);
                    this.OnPostDataUpdate?.InvokeSafely(this.context, activeHandlers);
                }
                else if (updatedHandlers.Count != 0)
                {
                    this.OnNamePlateUpdate?.InvokeSafely(this.context, updatedHandlers);

                    if (this.context.HasParts)
                        this.ApplyBuilders(updatedHandlers);

                    try
                    {
                        calledOriginal = true;
                        this.onRequestedUpdateHook.Original.Invoke(addon, numberArrayData, stringArrayData);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Caught exception when calling original AddonNamePlate OnRequestedUpdate.");
                    }

                    this.OnPostNamePlateUpdate?.InvokeSafely(this.context, updatedHandlers);
                    this.OnPostDataUpdate?.InvokeSafely(this.context, activeHandlers);
                }
            }
        }
        finally
        {
            if (!calledOriginal)
            {
                try
                {
                    this.onRequestedUpdateHook.Original.Invoke(addon, numberArrayData, stringArrayData);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Caught exception when calling original AddonNamePlate OnRequestedUpdate.");
                }
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

    private void ApplyBuilders(List<NamePlateUpdateHandler> handlers)
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
    public event INamePlateGui.OnPlateUpdateDelegate? OnPostNamePlateUpdate
    {
        add
        {
            if (this.OnPostNamePlateUpdateScoped == null)
                this.parentService.OnPostNamePlateUpdate += this.OnPostNamePlateUpdateForward;

            this.OnPostNamePlateUpdateScoped += value;
        }

        remove
        {
            this.OnPostNamePlateUpdateScoped -= value;
            if (this.OnPostNamePlateUpdateScoped == null)
                this.parentService.OnPostNamePlateUpdate -= this.OnPostNamePlateUpdateForward;
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

    /// <inheritdoc/>
    public event INamePlateGui.OnPlateUpdateDelegate? OnPostDataUpdate
    {
        add
        {
            if (this.OnPostDataUpdateScoped == null)
                this.parentService.OnPostDataUpdate += this.OnPostDataUpdateForward;

            this.OnPostDataUpdateScoped += value;
        }

        remove
        {
            this.OnPostDataUpdateScoped -= value;
            if (this.OnPostDataUpdateScoped == null)
                this.parentService.OnPostDataUpdate -= this.OnPostDataUpdateForward;
        }
    }

    private event INamePlateGui.OnPlateUpdateDelegate? OnNamePlateUpdateScoped;

    private event INamePlateGui.OnPlateUpdateDelegate? OnPostNamePlateUpdateScoped;

    private event INamePlateGui.OnPlateUpdateDelegate? OnDataUpdateScoped;

    private event INamePlateGui.OnPlateUpdateDelegate? OnPostDataUpdateScoped;

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

        this.parentService.OnPostNamePlateUpdate -= this.OnPostNamePlateUpdateForward;
        this.OnPostNamePlateUpdateScoped = null;

        this.parentService.OnDataUpdate -= this.OnDataUpdateForward;
        this.OnDataUpdateScoped = null;

        this.parentService.OnPostDataUpdate -= this.OnPostDataUpdateForward;
        this.OnPostDataUpdateScoped = null;
    }

    private void OnNamePlateUpdateForward(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        this.OnNamePlateUpdateScoped?.Invoke(context, handlers);
    }

    private void OnPostNamePlateUpdateForward(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        this.OnPostNamePlateUpdateScoped?.Invoke(context, handlers);
    }

    private void OnDataUpdateForward(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        this.OnDataUpdateScoped?.Invoke(context, handlers);
    }

    private void OnPostDataUpdateForward(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        this.OnPostDataUpdateScoped?.Invoke(context, handlers);
    }
}
