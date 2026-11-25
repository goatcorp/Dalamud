using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Fonts;

/// <summary>Draws an entity.</summary>
/// <param name="drawList">The draw list.</param>
/// <param name="topLeft">The top left of the entity.</param>
/// <param name="context">The entity's source, style, and resolved layout metrics.</param>
/// <remarks>An exception is logged, and the entity is not drawn.</remarks>
/// <seealso cref="TextEntity"/>
public delegate void TextEntityDrawDelegate(ImDrawListPtr drawList, Vector2 topLeft, in TextEntityContext context);
