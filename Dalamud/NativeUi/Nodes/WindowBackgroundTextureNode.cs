using System.Numerics;

using Dalamud.NativeUi.Classes;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Specialization of a NineGridNode for use with <see cref="WindowNode"/>. Not intended for external use.
/// </summary>
internal class WindowBackgroundTextureNode : NineGridNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowBackgroundTextureNode"/> class.
    /// Loads the parts required for a window node's background texture. SelectedPath is used to select the texture with a
    /// brighter border to indicate the window is focused.
    /// </summary>
    /// <param name="selectedPath">If this should load a modified path that represents when the window is focused.</param>
    /// <param name="path">Base texture path to load.</param>
    public WindowBackgroundTextureNode(bool selectedPath, string path = "ui/uld/WindowA_Bg")
    {
        var basePath = $"{path}{(selectedPath ? "Selected" : "Normal")}";

        this.PartsList.Add(
            new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(16.0f, 64.0f), Id = 0, TexturePath = $"{basePath}_Corner.tex" },
            new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 64.0f), Id = 1, TexturePath = $"{basePath}_H.tex" },
            new Part { TextureCoordinates = new Vector2(16.0f, 0.0f), Size = new Vector2(16.0f, 64.0f), Id = 2, TexturePath = $"{basePath}_Corner.tex" },
            new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(16.0f, 32.0f), Id = 3, TexturePath = $"{basePath}_V.tex" },
            new Part { TextureCoordinates = new Vector2(0.0f, 0.0f), Size = new Vector2(32.0f, 32.0f), Id = 4, TexturePath = $"{basePath}_HV.tex" },
            new Part { TextureCoordinates = new Vector2(16.0f, 0.0f), Size = new Vector2(16.0f, 32.0f), Id = 5, TexturePath = $"{basePath}_V.tex" },
            new Part { TextureCoordinates = new Vector2(0.0f, 64.0f), Size = new Vector2(16.0f, 32.0f), Id = 6, TexturePath = $"{basePath}_Corner.tex" },
            new Part { TextureCoordinates = new Vector2(0.0f, 64.0f), Size = new Vector2(32.0f, 32.0f), Id = 7, TexturePath = $"{basePath}_H.tex" },
            new Part { TextureCoordinates = new Vector2(16.0f, 64.0f), Size = new Vector2(16.0f, 32.0f), Id = 8, TexturePath = $"{basePath}_Corner.tex" });
    }
}
