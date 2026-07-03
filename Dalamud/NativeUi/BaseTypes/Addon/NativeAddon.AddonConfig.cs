using System.IO;
using System.Numerics;
using System.Text.Json;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Addon;

// todo: consider a dalamud-y way to save and load addon position and size information.
// internal unsafe partial class NativeAddon {
//     private readonly JsonSerializerOptions serializerOptions = new() {
//         WriteIndented = true,
//         IncludeFields = true,
//     };
//
//     private AddonConfig LoadAddonConfig() {
//         var directory = Services.PluginInterface.ConfigDirectory;
//         var file = new FileInfo(Path.Combine(directory.FullName, $"{this.InternalName}.addon.json"));
//         if (!file.Exists) {
//             file.Create().Close();
//
//             var newConfig = new AddonConfig();
//             this.SaveAddonConfig(newConfig);
//             return newConfig;
//         }
//
//         AddonConfig? addonConfig;
//
//         try {
//             var data = File.ReadAllText(file.FullName);
//             addonConfig = JsonSerializer.Deserialize<AddonConfig>(data, this.serializerOptions);
//             addonConfig ??= new AddonConfig();
//         }
//         catch (Exception e) {
//             Services.Log.Error(e, "Exception while deserializing AddonConfig, creating new config.");
//             addonConfig = new AddonConfig();
//             this.SaveAddonConfig(addonConfig);
//         }
//
//         return addonConfig;
//     }
//
//     private void SaveAddonConfig(AddonConfig addonConfig) {
//         var directory = Services.PluginInterface.ConfigDirectory;
//         var file = new FileInfo(Path.Combine(directory.FullName, $"{this.InternalName}.addon.json"));
//
//         var data = JsonSerializer.Serialize(addonConfig, this.serializerOptions);
//
//         FilesystemUtil.WriteAllTextSafe(file.FullName, data);
//     }
//
//     private void SaveAddonConfig() {
//         var configData = new AddonConfig {
//             Position = new Vector2(this.InternalAddon->X, this.InternalAddon->Y),
//             Scale = this.InternalAddon->Scale / AtkUnitBase.GetGlobalUIScale(),
//         };
//
//         this.SaveAddonConfig(configData);
//     }
// }
