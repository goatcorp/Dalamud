using System.Numerics;

using Dalamud.NativeUi.Classes;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// A simplified implementation of a <see cref="CounterNode"/>, with default font set to Money font.
/// </summary>
internal class SimpleCounterNode : CounterNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleCounterNode"/> class.
    /// Constructs a new <see cref="SimpleCounterNode"/>.
    /// </summary>
    public SimpleCounterNode()
    {
        this.PartsList.Add(new Part
        {
            TexturePath = "ui/uld/Money_Number.tex",
            TextureCoordinates = Vector2.Zero,
            Size = new Vector2(22.0f, 22.0f),
        });
    }
}
