using System.Numerics;

using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Extensions;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// A specialization of an ImageNode intended for displaying game icons via IconId.
/// </summary>
/// <remarks>This node is not intended to be used with multiple <see cref="Part"/>'s.</remarks>
internal unsafe class IconImageNode : SimpleImageNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IconImageNode"/> class.
    /// </summary>
    public IconImageNode()
    {
        this.TextureSize = new Vector2(32.0f, 32.0f);
    }

    /// <summary>
    /// Gets or sets the displayed Icon.
    /// </summary>
    public uint IconId
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                this.PartsList[0]->LoadIcon(value);
            }
        }
    }
}
