using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Serilog;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Utility.Internal;

/// <summary>Utility function for saving textures.</summary>
[ServiceManager.EarlyLoadedService]
internal sealed class DevTextureSaveMenu : IInternalDisposableService
{
    [ServiceManager.ServiceDependency]
    private readonly InterfaceManager interfaceManager = Service<InterfaceManager>.Get();

    private readonly FileDialogManager fileDialogManager;

    [ServiceManager.ServiceConstructor]
    private DevTextureSaveMenu()
    {
        this.fileDialogManager = new();
        this.interfaceManager.Draw += this.InterfaceManagerOnDraw;
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.interfaceManager.Draw -= this.InterfaceManagerOnDraw;

    /// <summary>Shows a context menu confirming texture save.</summary>
    /// <param name="initiatorName">Name of the initiator.</param>
    /// <param name="name">Suggested name of the file being saved.</param>
    /// <param name="texture">A task returning the texture to save.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ShowTextureSaveMenuAsync(
        string initiatorName,
        string name,
        Task<IDalamudTextureWrap> texture)
    {
        name = new StringBuilder(name)
               .Replace('<', '_')
               .Replace('>', '_')
               .Replace(':', '_')
               .Replace('"', '_')
               .Replace('/', '_')
               .Replace('\\', '_')
               .Replace('|', '_')
               .Replace('?', '_')
               .Replace('*', '_')
               .ToString();

        var isCopy = false;
        try
        {
            var initiatorScreenOffset = ImGui.GetMousePos();
            using var textureWrap = await texture;
            var textureManager = await Service<TextureManager>.GetAsync();
            var popupName = $"{nameof(this.ShowTextureSaveMenuAsync)}_{textureWrap.Handle.Handle:X}";

            BitmapCodecInfo? encoder;
            {
                var first = true;
                var encoders = textureManager.Wic.GetSupportedEncoderInfos().ToList();
                var tcs = new TaskCompletionSource<BitmapCodecInfo?>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                Service<InterfaceManager>.Get().Draw += DrawChoices;

                encoder = await tcs.Task;

                [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "This shall not escape")]
                void DrawChoices()
                {
                    if (first)
                    {
                        ImGui.OpenPopup(popupName);
                        first = false;
                    }

                    ImGui.SetNextWindowPos(initiatorScreenOffset, ImGuiCond.Appearing);
                    if (!ImGui.BeginPopup(
                            popupName,
                            ImGuiWindowFlags.AlwaysAutoResize |
                            ImGuiWindowFlags.NoTitleBar |
                            ImGuiWindowFlags.NoSavedSettings))
                    {
                        Service<InterfaceManager>.Get().Draw -= DrawChoices;
                        tcs.TrySetCanceled();
                        return;
                    }

                    if (ImGui.Selectable("Copy"))
                        tcs.TrySetResult(null);
                    foreach (var encoder2 in encoders)
                    {
                        if (ImGui.Selectable(encoder2.Name))
                            tcs.TrySetResult(encoder2);
                    }

                    const float previewImageWidth = 320;
                    var size = textureWrap.Size;
                    if (size.X > previewImageWidth)
                        size *= previewImageWidth / size.X;
                    if (size.Y > previewImageWidth)
                        size *= previewImageWidth / size.Y;
                    ImGui.Image(textureWrap.Handle, size);

                    if (tcs.Task.IsCompleted)
                        ImGui.CloseCurrentPopup();

                    ImGui.EndPopup();
                }
            }

            if (encoder is null)
            {
                isCopy = true;
                await textureManager.CopyToClipboardAsync(textureWrap, name, true);
            }
            else
            {
                var props = new Dictionary<string, object>();
                if (encoder.ContainerGuid == GUID.GUID_ContainerFormatTiff)
                    props["CompressionQuality"] = 1.0f;
                else if (encoder.ContainerGuid == GUID.GUID_ContainerFormatJpeg ||
                         encoder.ContainerGuid == GUID.GUID_ContainerFormatHeif ||
                         encoder.ContainerGuid == GUID.GUID_ContainerFormatWmp)
                    props["ImageQuality"] = 1.0f;

                var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.fileDialogManager.SaveFileDialog(
                    "Save texture...",
                    $"{encoder.Name.Replace(',', '.')}{{{string.Join(',', encoder.Extensions)}}}",
                    name + encoder.Extensions.First(),
                    encoder.Extensions.First(),
                    (ok, path2) =>
                    {
                        if (!ok)
                            tcs.SetCanceled();
                        else
                            tcs.SetResult(path2);
                    });
                var path = await tcs.Task.ConfigureAwait(false);

                await textureManager.SaveToFileAsync(textureWrap, encoder.ContainerGuid, path, props: props);

                var notif = Service<NotificationManager>.Get().AddNotification(
                    new()
                    {
                        Content = $"File saved to: {path}",
                        Title = initiatorName,
                        Type = NotificationType.Success,
                    });
                notif.Click += n =>
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    n.Notification.DismissNow();
                };
            }
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException)
                return;

            Log.Error(
                e,
                $"{nameof(DalamudInterface)}.{nameof(this.ShowTextureSaveMenuAsync)}({initiatorName}, {name})");
            Service<NotificationManager>.Get().AddNotification(
                isCopy
                    ? $"Failed to copy file: {e}"
                    : $"Failed to save file: {e}",
                initiatorName,
                NotificationType.Error);
        }
    }

    private void InterfaceManagerOnDraw() => this.fileDialogManager.Draw();
}
