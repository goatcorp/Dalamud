// using Dalamud.Hooking;
// using Dalamud.Hooking.Internal;
//
// using FFXIVClientStructs.FFXIV.Component.GUI;
//
// namespace Dalamud.NativeUi.BaseTypes.Addon;

// todo: need to consider a more dalamud-y way of creating and using this hook.
// /// <summary>
// /// .
// /// </summary>
// internal unsafe partial class NativeAddon
// {
//     private static Hook<AtkUnitBase.Delegates.FireCallback>? fireCallbackHook;
//
//     internal static void InitializeCloseCallback()
//     {
//         fireCallbackHook = Hook<AtkUnitBase.Delegates.FireCallback>.FromAddress(AtkUnitBase.Addresses.FireCallback.Value, OnFireCallback);
//         fireCallbackHook.Enable();
//     }
//
//     private static bool OnFireCallback(AtkUnitBase* thisPtr, uint valueCount, AtkValue* values, bool close)
//     {
//         foreach (var addon in CreatedAddons)
//         {
//             if (addon == thisPtr && close && addon is { RespectCloseAll: true, IsOverlayAddon: false })
//             {
//                 addon.Close();
//                 return true;
//             }
//         }
//
//         return fireCallbackHook!.Original(thisPtr, valueCount, values, close);
//     }
//
//     internal static void DisposeCloseCallback()
//     {
//         fireCallbackHook?.Dispose();
//         fireCallbackHook = null;
//     }
// }
