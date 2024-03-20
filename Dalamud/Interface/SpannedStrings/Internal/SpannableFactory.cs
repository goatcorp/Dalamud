// using System.IO;
// using System.Linq;
// using System.Runtime.CompilerServices;
// using System.Text;
//
// using Dalamud.Data;
// using Dalamud.Interface.Internal;
// using Dalamud.IoC;
// using Dalamud.IoC.Internal;
// using Dalamud.Plugin.Services;
// using Dalamud.Utility;
//
// using ImGuiNET;
//
// using Lumina.Data.Files;
//
// namespace Dalamud.Interface.SpannedStrings.Internal;
//
// /// <summary>Custom text renderer factory.</summary>
// [ServiceManager.EarlyLoadedService]
// [PluginInterface]
// [InterfaceVersion("1.0")]
// #pragma warning disable SA1015
// [ResolveVia<ISpannableFactory>]
// #pragma warning restore SA1015
// internal class SpannableFactory : IInternalDisposableService, ISpannableFactory
// {
//     private const int MaxMemoryStreamCapacityToPool = 65536;
//
//     [ServiceManager.ServiceDependency]
//     private readonly DataManager dataManager = Service<DataManager>.Get();
//
//     private readonly SpannableRenderer?[] rendererPool;
//     private readonly MemoryStream?[] memoryStreamPool;
//     private readonly byte[] gfdFile;
//     private readonly IDalamudTextureWrap[] gfdTextures;
//
//     private bool disposing;
//
//     [ServiceManager.ServiceConstructor]
//     private SpannableFactory(InterfaceManager.InterfaceManagerWithScene imws, TextureManager textureManager)
//     {
//         this.rendererPool = new SpannableRenderer?[8];
//         this.memoryStreamPool = new MemoryStream?[128];
//
//         var t = this.dataManager.GetFile("common/font/gfdata.gfd")!.Data;
//         t.CopyTo((this.gfdFile = GC.AllocateUninitializedArray<byte>(t.Length, true)).AsSpan());
//         this.gfdTextures =
//             new[]
//                 {
//                     "common/font/fonticon_xinput.tex",
//                     "common/font/fonticon_ps3.tex",
//                     "common/font/fonticon_ps4.tex",
//                     "common/font/fonticon_ps5.tex",
//                     "common/font/fonticon_lys.tex",
//                 }
//                 .Select(x => textureManager.GetTexture(this.dataManager.GetFile<TexFile>(x)!))
//                 .ToArray();
//     }
//
//     /// <summary>Gets the textures for graphic font icons.</summary>
//     internal ReadOnlySpan<IDalamudTextureWrap> GfdTextures => this.gfdTextures;
//
//     /// <summary>Gets the GFD file view.</summary>
//     internal unsafe GfdFileView GfdFileView => new(new(Unsafe.AsPointer(ref this.gfdFile[0]), this.gfdFile.Length));
//
//     /// <inheritdoc/>
//     void IInternalDisposableService.DisposeService()
//     {
//         if (this.disposing)
//             return;
//
//         this.disposing = true;
//         foreach (ref var p in this.rendererPool.AsSpan())
//         {
//             p?.ReleaseUnmanagedResources();
//             p = null;
//         }
//
//         foreach (var t in this.gfdTextures)
//             t.Dispose();
//     }
//
//     /// <inheritdoc/>
//     public unsafe ISpannableRenderer Rent(
//         ISpannableRenderer.Usage usage, ISpannableRenderer.Options options = default)
//     {
//         ThreadSafety.AssertMainThread();
//
//         ImDrawList* drawListPtr;
//         bool putDummy;
//         uint globalId;
//         if (!usage.LabelU8.IsEmpty)
//         {
//             drawListPtr = ImGui.GetWindowDrawList();
//             fixed (byte* p = usage.LabelU8)
//                 globalId = ImGuiNative.igGetID_StrStr(p, p + usage.LabelU8.Length);
//             putDummy = true;
//         }
//         else if (!usage.LabelU16.IsEmpty)
//         {
//             drawListPtr = ImGui.GetWindowDrawList();
//             Span<byte> buf = stackalloc byte[Encoding.UTF8.GetByteCount(usage.LabelU16)];
//             Encoding.UTF8.GetBytes(usage.LabelU16, buf);
//             fixed (byte* p = buf)
//                 globalId = ImGuiNative.igGetID_StrStr(p, p + buf.Length);
//             putDummy = true;
//         }
//         else if (usage.Id is not null)
//         {
//             drawListPtr = ImGui.GetWindowDrawList();
//             globalId = ImGuiNative.igGetID_Ptr((void*)usage.Id.Value);
//             putDummy = true;
//         }
//         else if (usage.PutDummy)
//         {
//             drawListPtr = ImGui.GetWindowDrawList();
//             globalId = 0;
//             putDummy = true;
//         }
//         else
//         {
//             drawListPtr = usage.DrawListPtr;
//             globalId = 0;
//             putDummy = false;
//         }
//
//         var instance = default(SpannableRenderer);
//         foreach (ref var x in this.rendererPool.AsSpan())
//         {
//             if (x is not null)
//             {
//                 instance = x;
//                 x = null;
//                 break;
//             }
//         }
//
//         instance ??= new(this);
//         return instance.Initialize(
//             options,
//             globalId,
//             putDummy,
//             drawListPtr);
//     }
//
//     /// <summary>Adjusts the color by the given opacity.</summary>
//     /// <param name="color">The color.</param>
//     /// <param name="opacity">The opacity.</param>
//     /// <returns>The adjusted color.</returns>
//     internal static uint ApplyOpacity(uint color, float opacity)
//     {
//         if (opacity >= 1f)
//             return color;
//         if (opacity <= 0f)
//             return color & 0xFFFFFFu;
//
//         // Dividing and multiplying by 256, to use flooring. Range is [0, 1).
//         var a = (uint)(((color >> 24) / 256f) * opacity * 256f);
//         return (color & 0xFFFFFFu) | (a << 24);
//     }
//
//     /// <summary>Rents a memory stream.</summary>
//     /// <returns>The rented memory stream.</returns>
//     internal MemoryStream RentMemoryStream()
//     {
//         ThreadSafety.DebugAssertMainThread();
//
//         foreach (ref var x in this.memoryStreamPool.AsSpan())
//         {
//             if (x is not null)
//             {
//                 var instance = x;
//                 x = null;
//                 return instance;
//             }
//         }
//
//         return new();
//     }
//
//     /// <summary>Returns the finished instance of <see cref="SpannableRenderer"/>.</summary>
//     /// <param name="renderer">The instance to return.</param>
//     /// <remarks>For use with <see cref="SpannableRenderer"/>.</remarks>
//     internal void Return(SpannableRenderer? renderer)
//     {
//         if (renderer is null)
//             return;
//         if (!this.disposing)
//         {
//             foreach (ref var x in this.rendererPool.AsSpan())
//             {
//                 if (x is null)
//                 {
//                     x = renderer;
//                     return;
//                 }
//             }
//         }
//
//         renderer.ReleaseUnmanagedResources();
//     }
//
//     /// <summary>Returns an instance of <see cref="MemoryStream"/>.</summary>
//     /// <param name="memoryStream">The instance to return.</param>
//     /// <remarks>For use with <see cref="SpannableRenderer"/>.</remarks>
//     internal void Return(MemoryStream? memoryStream)
//     {
//         ThreadSafety.DebugAssertMainThread();
//
//         if (memoryStream is null || memoryStream.Capacity > MaxMemoryStreamCapacityToPool)
//             return;
//
//         if (!this.disposing)
//         {
//             foreach (ref var x in this.memoryStreamPool.AsSpan())
//             {
//                 if (x is null)
//                 {
//                     memoryStream.Position = 0;
//                     memoryStream.SetLength(0);
//                     x = memoryStream;
//                     return;
//                 }
//             }
//         }
//     }
// }
