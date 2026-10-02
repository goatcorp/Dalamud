namespace Dalamud.NativeUi.Enums;

/// <summary>
/// Enum representing a relative position for inserting new nodes into an existing node tree.
/// </summary>
internal enum NodePosition
{
    /// <summary>
    /// Inserts as the nodes NextSibling, which will cause it to render underneath the target node.
    /// </summary>
    BeforeTarget,

    /// <summary>
    /// Inserts as the nodes PrevSibling, will with cause it to render over top of the target node.
    /// </summary>
    AfterTarget,

    /// <summary>
    /// Inserts as the first child of the target's parent node. Causing it to render underneath all other sibling nodes.
    /// </summary>
    BeforeAllSiblings,

    /// <summary>
    /// Inserts as the last child of the target's parent node. Causing it to render over top of all other sibling nodes.
    /// </summary>
    AfterAllSiblings,

    /// <summary>
    /// Inserts as the last child of the target's node.
    /// </summary>
    AsLastChild,

    /// <summary>
    /// Inserts as the first child of the target's node.
    /// </summary>
    AsFirstChild,
}
