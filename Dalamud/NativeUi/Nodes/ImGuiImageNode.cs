using System.IO;
using System.Threading.Tasks;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Extensions;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// A simple image node that allows you to load an IDalamudTextureWrap texture into a native image node.
/// This node creates a single <see cref="Part" />.
/// </summary>
/// <remarks>This node is not intended to be used with multiple <see cref="Part"/>'s.</remarks>
internal class ImGuiImageNode : SimpleImageNode
{
    /// <summary>
    /// Gets or sets the texture from either the game or from disk.
    /// </summary>
    public override string TexturePath
    {
        get => base.TexturePath;
        set
        {
            // If path represents a file system path, we need to load via ImGui.
            if (Path.IsPathRooted(value))
            {
                // Start by hiding the node.
                this.Alpha = 0.0f;

                // Load the texture as a task
                Task.Run(async () =>
                {
                    var newTexture = await Service<TextureManager>.Get().Shared.GetFromFile(value).RentAsync();
                    this.Log.Verbose("Loaded texture from file system: {Value}", value);

                    // Once it's ready, load it into the node on the next frame.
                    await Service<Framework>.Get().Run(() =>
                    {
                        unsafe
                        {
                            if (this.Node is not null)
                            {
                                this.LoadTexture(newTexture);
                                this.TextureSize = newTexture.Size;
                                this.Alpha = 1.0f;
                                this.MarkDirty();
                            }
                        }
                    });
                });
            }

            // else, the path is a game file, and the game itself can do its loading magic.
            else if (Service<DataManager>.Get().FileExists(value))
            {
                unsafe
                {
                    this.PartsList[0]->LoadTexture(value);
                }
            }
        }
    }

    private IDalamudTextureWrap? LoadedTexture { get; set; }

    /// <summary>
    /// Takes ownership of passed in IDalamudTextureWrap, node automatically disposes texture when node is disposed.
    /// </summary>
    /// <remarks>
    /// Don't try to share this texture across nodes.
    /// If you need to have the same texture for multiple nodes use <see cref="TexturePath"/>,
    /// or load one independent instance of <see cref="IDalamudTextureWrap"/> per node.
    /// </remarks>
    /// <param name="texture">Texture to convert to a Kernel Texture and load.</param>
    public unsafe void LoadTexture(IDalamudTextureWrap texture)
    {
        // Load new texture
        this.PartsList[0]->LoadTexture(texture);

        if (this.LoadedTexture is not null)
        {
            this.Log.Verbose("Disposing texture: {DalamudTextureWrap} to load {Texture}", this.LoadedTexture, texture);
        }

        // Dispose any previously used texture
        this.LoadedTexture?.Dispose();

        // Track currently used texture
        this.LoadedTexture = texture;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing, bool isNativeDestructor)
    {
        if (disposing && !this.IsDisposed)
        {
            base.Dispose(disposing, isNativeDestructor);

            if (this.LoadedTexture is not null)
            {
                this.Log.Verbose("Disposing texture: {DalamudTextureWrap}", this.LoadedTexture);
            }

            this.LoadedTexture?.Dispose();
            this.LoadedTexture = null;
        }
    }
}
