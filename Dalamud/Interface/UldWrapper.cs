using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Data;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Uld;

namespace Dalamud.Interface;

/// <summary> Wrapper for multi-icon sprite sheets defined by ULD files. </summary>
public class UldWrapper : IDisposable
{
    private readonly DataManager data;
    private readonly UiBuilder uiBuilder;
    private readonly Dictionary<string, (uint Id, int Width, int Height, bool HD, byte[] RgbaData)> textures = new();

    /// <summary> Initializes a new instance of the <see cref="UldWrapper"/> class, wrapping an ULD file. </summary>
    /// <param name="uiBuilder">The UiBuilder used to load textures.</param>
    /// <param name="uldPath">The requested ULD file.</param>
    internal UldWrapper(UiBuilder uiBuilder, string uldPath)
    {
        this.uiBuilder = uiBuilder;
        this.data = Service<DataManager>.Get();
        this.Uld = this.data.GetFile<UldFile>(uldPath);
    }

    /// <summary> Gets the loaded ULD file if it exists. </summary>
    public UldFile? Uld { get; private set; }

    /// <summary> Gets a value indicating whether the requested ULD could be loaded. </summary>
    public bool Valid
        => this.Uld != null;

    /// <summary> Load a part of a multi-icon sheet as a texture. </summary>
    /// <param name="texturePath">The path of the requested texture.</param>
    /// <param name="part">The index of the desired icon.</param>
    /// <returns>A TextureWrap containing the requested part if it exists and null otherwise.</returns>
    public IDalamudTextureWrap? LoadTexturePart(string texturePath, int part)
    {
        if (!this.Valid)
        {
            return null;
        }

        if (!this.textures.TryGetValue(texturePath, out var texture))
        {
            var tuple = this.GetTexture(texturePath);
            if (!tuple.HasValue)
            {
                return null;
            }

            texture = tuple.Value;
            this.textures[texturePath] = texture;
        }

        return this.CreateTexture(texture.Id, texture.Width, texture.Height, texture.HD, texture.RgbaData, part);
    }

    /// <summary> Clear all stored data and remove the loaded ULD. </summary>
    public void Dispose()
    {
        this.textures.Clear();
        this.Uld = null;
    }

    private IDalamudTextureWrap? CreateTexture(uint id, int width, int height, bool hd, byte[] rgbaData, int partIdx)
    {
        var idx = 0;
        UldRoot.PartData? partData = null;

        // Iterate over all available parts that have the corresponding TextureId,
        // count up to the required one and return it if it exists.
        foreach (var part in this.Uld!.Parts.SelectMany(p => p.Parts))
        {
            if (part.TextureId != id)
            {
                continue;
            }

            if (idx++ == partIdx)
            {
                partData = part;
                break;
            }
        }

        if (!partData.HasValue)
        {
            return null;
        }

        // Double all dimensions for HD textures.
        var d = hd ? partData.Value with
        {
            H = (ushort)(partData.Value.H * 2),
            W = (ushort)(partData.Value.W * 2),
            U = (ushort)(partData.Value.U * 2),
            V = (ushort)(partData.Value.V * 2),
        } : partData.Value;

        return this.CopyRect(width, height, rgbaData, d);
    }

    private IDalamudTextureWrap? CopyRect(int width, int height, byte[] rgbaData, UldRoot.PartData part)
    {
        if (part.U + part.W > width || part.V + part.H > height)
        {
            return null;
        }

        var imageData = new byte[part.W * part.H * 4];

        // Iterate over all lines and copy the relevant ones,
        // assuming a 4-byte-per-pixel standard layout.
        for (var y = 0; y < part.H; ++y)
        {
            var inputSlice = rgbaData.AsSpan().Slice((((part.V + y) * width) + part.U) * 4, part.W * 4);
            var outputSlice = imageData.AsSpan(y * part.W * 4);
            inputSlice.CopyTo(outputSlice);
        }

        return this.uiBuilder.LoadImageRaw(imageData, part.W, part.H, 4);
    }

    private (uint Id, int Width, int Height, bool HD, byte[] RgbaData)? GetTexture(string texturePath)
    {
        if (!this.Valid)
        {
            return null;
        }

        // Always replace the HD version with the regular one as ULDs do not contain the HD suffix.
        texturePath = texturePath.Replace("_hr1", string.Empty);

        // Search the requested texture asset in the ULD and store its ID if it exists.
        var id = uint.MaxValue;
        foreach (var part in this.Uld!.AssetData)
        {
            var maxLength = Math.Min(part.Path.Length, texturePath.AsSpan().Length);
            if (part.Path.AsSpan()[..maxLength].SequenceEqual(texturePath.AsSpan()[..maxLength]))
            {
                id = part.Id;
                break;
            }
        }

        if (id == uint.MaxValue)
        {
            return null;
        }

        // Try to load HD textures first. 
        var hrPath = texturePath.Replace(".tex", "_hr1.tex");
        var substitution = Service<TextureManager>.Get();
        hrPath = substitution.GetSubstitutedPath(hrPath);
        var hd = true;
        var tex = Path.IsPathRooted(hrPath) 
                      ? this.data.GameData.GetFileFromDisk<TexFile>(hrPath) 
                      : this.data.GetFile<TexFile>(hrPath);
        if (tex == null)
        {
            hd = false;
            texturePath = substitution.GetSubstitutedPath(texturePath);
            tex = Path.IsPathRooted(texturePath)
                      ? this.data.GameData.GetFileFromDisk<TexFile>(texturePath)
                      : this.data.GetFile<TexFile>(texturePath);

            // Neither texture could be loaded.
            if (tex == null)
            {
                return null;
            }
        }

        return (id, tex.Header.Width, tex.Header.Height, hd, tex.GetRgbaImageData());
    }
}
