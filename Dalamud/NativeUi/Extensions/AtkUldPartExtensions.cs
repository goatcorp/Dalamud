using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Data;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Extensions;

/// <summary>
/// Extension methods for AtkUldParts and their contained Assets/Textures.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Stylecop doesn't understand Extension Blocks, you cant prefix with 'this'.")]
internal static unsafe class AtkUldPartExtensions
{
    private static readonly ModuleLog Log = new("AtkUldPartExtensions");

    /// <summary>
    /// Cache to reduce cost of repeated lookups on if a specific file exists.
    /// File existence is not expected to change after the game is launched.
    /// </summary>
    private static readonly Dictionary<string, bool> FileExistsCache = [];

    extension(ref AtkUldPart part) {

        /// <summary>
        /// Gets a value indicating whether the texture is not null and ready.
        /// </summary>
        public bool IsTextureReady
            => part.UldAsset is not null && part.UldAsset->AtkTexture.IsTextureReady();

        /// <summary>
        /// Gets the size of the loaded texture, or <see cref="Vector2.Zero"/> if null or not ready.
        /// </summary>
        public Vector2 LoadedTextureSize
            => part.GetActualTextureSize();

        /// <summary>
        /// Gets the texture path of the loaded texture, or <see cref="string.Empty"/> if null or not ready.
        /// </summary>
        public string LoadedPath
            => part.GetLoadedPath();

        /// <summary>
        /// Load the texture from the given path.
        /// </summary>
        /// <remarks>
        /// Omit '_hr1' from provided paths as they will be stripped.
        /// The games own texture loading system will load the appropriate texture resolution according to the current in-game settings.
        /// Additionally, ensure that you are not loading textures before login, as that will lock the loaded textures themes to the games default theme texture,
        /// even in places where native uses the texture.
        /// </remarks>
        /// <param name="path">Texture path to resolve.</param>
        /// <param name="resolveTheme">If the path should have its path adjusted to the current theme.</param>
        public void LoadTexture(string path, bool resolveTheme = true)
        {
            try
            {
                if (part.UldAsset is null) return;

                part.TryUnloadTexture();

                // Check for empty path after unloading, as setting empty
                // path may indicate intention to unload the texture.
                if (path is "") return;

                var texturePath = path.Replace("_hr1", string.Empty);

                var themedPath = texturePath.Replace("uld", GetThemePathModifier());
                if (FileExists(themedPath) && resolveTheme)
                {
                    texturePath = themedPath;
                }

                if (FileExists(texturePath))
                {
                    part.UldAsset->AtkTexture.LoadTextureWithDefaultVersion(texturePath);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error in AtkUldPartExtensions LoadTexture");
            }
        }

        /// <summary>
        /// Loads a texture based on an iconId.
        /// </summary>
        /// <remarks>
        /// This will attempt to find which icon sub folder the iconId actually lives in.
        /// </remarks>
        public void LoadIcon(uint iconId)
            => part.UldAsset->AtkTexture.LoadIconTexture(iconId, GetIconSubFolder(iconId));

        /// <summary>
        /// Load a texture from a Texture* directly.
        /// </summary>
        /// <remarks>
        /// <em>Warning, calling this multiple times on the same part may corrupt the game state.</em>
        /// Additionally unloading this part with a Texture* set may attempt to release the texture that wasn't owned in the first place.
        /// Undefined behavior may result.
        /// </remarks>
        /// <param name="texture">Texture to load.</param>
        public void LoadTexture(Texture* texture)
        {
            if (part.UldAsset is null) return;

            part.TryUnloadTexture();
            part.UldAsset->AtkTexture.KernelTexture = texture;
            part.UldAsset->AtkTexture.TextureType = TextureType.KernelTexture;
        }

        /// <summary>
        /// Loads texture from a IDalamudTextureWrap.
        /// </summary>
        /// <remarks>
        /// The texture wrap must remain valid for the lifetime of this node.
        /// </remarks>
        /// <param name="textureWrap">Texture wrap to load.</param>
        public void LoadTexture(IDalamudTextureWrap textureWrap)
        {
            var texturePointer = Service<TextureManager>.Get().ConvertToKernelTexture(textureWrap, true);
            if (texturePointer is null) return;

            part.LoadTexture(texturePointer);
        }

        private string GetLoadedPath()
        {
            if (part.UldAsset is null) return string.Empty;
            if (part.UldAsset->AtkTexture.Resource is null) return string.Empty;
            if (part.UldAsset->AtkTexture.Resource->TexFileResourceHandle is null) return string.Empty;

            return part.UldAsset->AtkTexture.Resource->TexFileResourceHandle->FileName.ToString();
        }

        private void TryUnloadTexture()
        {
            if (part.UldAsset is null) return;
            if (!part.UldAsset->AtkTexture.IsTextureReady()) return;
            if (part.UldAsset->AtkTexture.TextureType is 0) return;
            if (part.UldAsset->AtkTexture.KernelTexture is null) return;

            part.UldAsset->AtkTexture.ReleaseTexture();
            part.UldAsset->AtkTexture.KernelTexture = null;
            part.UldAsset->AtkTexture.TextureType = 0;
        }

        private Vector2 GetActualTextureSize()
        {
            if (part.UldAsset is null) return Vector2.Zero;
            if (!part.UldAsset->AtkTexture.IsTextureReady()) return Vector2.Zero;
            if (part.UldAsset->AtkTexture.TextureType is 0) return Vector2.Zero;
            if (part.UldAsset->AtkTexture.KernelTexture is null) return Vector2.Zero;

            var width = part.UldAsset->AtkTexture.GetTextureWidth();
            var height = part.UldAsset->AtkTexture.GetTextureHeight();
            return new Vector2(width, height);
        }
    }

    private static IconSubFolder GetIconSubFolder(uint iconId)
    {
        var textureManager = AtkStage.Instance()->AtkTextureResourceManager;
        Span<byte> buffer = stackalloc byte[0x100];
        buffer.Clear();
        var bytePointer = (byte*)Unsafe.AsPointer(ref buffer[0]);

        var textureScale = textureManager->DefaultTextureScale;
        var targetFolder = (IconSubFolder)textureManager->IconLanguage;

        // Try to resolve the path using the current language
        AtkTexture.GetIconPath(bytePointer, iconId, textureScale, targetFolder);
        var pathResult = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(bytePointer));

        // If the resolved path doesn't exist, re-process with default folder
        return FileExists(pathResult) ? targetFolder : IconSubFolder.None;
    }

    private static string GetThemePathModifier() => AtkStage.Instance()->AtkUIColorHolder->ActiveColorThemeType switch {
        not 0 => $"uld/img{AtkStage.Instance()->AtkUIColorHolder->ActiveColorThemeType:00}",
        _ => "uld",
    };

    private static bool FileExists(string path)
    {
        if (FileExistsCache.TryGetValue(path, out var result)) return result;

        var fileExists = Service<DataManager>.Get().FileExists(path);
        FileExistsCache.TryAdd(path, fileExists);

        return fileExists;
    }
}
