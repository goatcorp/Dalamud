using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui;
using Dalamud.Interface.Internal.UiDebug.Browsing;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Ipc.Internal;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Bindings.ImGui.ImGuiWindowFlags;

namespace Dalamud.Interface.Internal.UiDebug;

// Original version by aers https://github.com/aers/FFXIVUIDebug
// Also incorporates features from Caraxi's fork https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Debugging/UIDebug.cs

/// <summary>
/// A tool for browsing the contents and structure of UI elements.
/// </summary>
internal partial class UiDebug : IDisposable
{
    /// <inheritdoc cref="ModuleLog"/>
    internal static readonly ModuleLog Log = ModuleLog.Create<UiDebug>();

    private const string TypeMappedNodesDataShareName = "TypeMappedCustomNodes";
    private const string StringMappedNodesDataShareName = "StringMappedCustomNodes";

    private readonly ElementSelector elementSelector;
    private readonly DataCachePluginId dalamudInternalId = new("DalamudInternal", Guid.NewGuid());

    /// <summary>
    /// Initializes a new instance of the <see cref="UiDebug"/> class.
    /// </summary>
    internal UiDebug()
    {
        this.elementSelector = new ElementSelector(this);
        CustomNodeTypeDefinitions = Service<DataShare>.Get().GetOrCreateData(TypeMappedNodesDataShareName, this.dalamudInternalId, () => new ConcurrentDictionary<nint, Type>());
        CustomNodeStringDefinitions = Service<DataShare>.Get().GetOrCreateData(StringMappedNodesDataShareName, this.dalamudInternalId, () => new ConcurrentDictionary<nint, string>());
    }

    /// <summary> Gets a mapping of address to typename for custom nodes.</summary>
    internal static ConcurrentDictionary<nint, Type>? CustomNodeTypeDefinitions { get; private set; }

    /// <summary> Gets a mapping of address to typename for custom nodes.</summary>
    internal static ConcurrentDictionary<nint, string>? CustomNodeStringDefinitions { get; private set; }

    /// <inheritdoc cref="IGameGui"/>
    internal static IGameGui GameGui { get; set; } = Service<GameGui>.Get();

    /// <summary>
    /// Gets a collection of <see cref="AddonTree"/> instances, each representing an <see cref="FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase"/>.
    /// </summary>
    internal static Dictionary<string, AddonTree> AddonTrees { get; } = [];

    /// <summary>
    /// Gets or sets a window system to handle any popout windows for addons or nodes.
    /// </summary>
    internal static WindowSystem PopoutWindows { get; set; } = new("UiDebugPopouts");

    /// <summary>
    /// Gets or sets the name of the currently-selected <see cref="AtkUnitBase"/>.
    /// </summary>
    internal string? SelectedAddonName { get; set; }

    /// <summary>
    /// Clears all windows and <see cref="AddonTree"/>s.
    /// </summary>
    public void Dispose()
    {
        foreach (var a in AddonTrees)
        {
            a.Value.Dispose();
        }

        AddonTrees.Clear();
        PopoutWindows.RemoveAllWindows();
        this.elementSelector.Dispose();

        Service<DataShare>.Get().RelinquishData(TypeMappedNodesDataShareName,  this.dalamudInternalId);
        Service<DataShare>.Get().RelinquishData(StringMappedNodesDataShareName,  this.dalamudInternalId);
    }

    /// <summary>
    /// Returns a string representing the type passed in.
    /// </summary>
    /// <param name="type">Type to parse.</param>
    /// <param name="fullName">Whether to include full typename.</param>
    /// <returns>String representation of the type.</returns>
    internal static string GetReadableTypeName(Type type, bool fullName = false)
    {
        var stars = string.Empty;

        var i = 0;
        while (type.IsPointer)
        {
            stars += "*";
            type = type.GetElementType()!;
            if (i++ > 10) break; // not yet encountered, but better be safe!
        }

        if (type == typeof(nint) || type.GetElementType() == typeof(nint))
            return "nint" + stars;

        if (!type.IsEnum)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return "bool" + stars;
                case TypeCode.Char:
                    return "char" + stars;
                case TypeCode.SByte:
                    return "sbyte" + stars;
                case TypeCode.Byte:
                    return "byte" + stars;
                case TypeCode.Int16:
                    return "short" + stars;
                case TypeCode.UInt16:
                    return "ushort" + stars;
                case TypeCode.Int32:
                    return "int" + stars;
                case TypeCode.UInt32:
                    return "uint" + stars;
                case TypeCode.Int64:
                    return "long" + stars;
                case TypeCode.UInt64:
                    return "ulong" + stars;
                case TypeCode.Single:
                    return "float" + stars;
                case TypeCode.Double:
                    return "double" + stars;
                case TypeCode.Decimal:
                    return "decimal" + stars;
                case TypeCode.String:
                    return "string" + stars;
            }
        }

        if (type.IsGenericType)
        {
            var nameEndPos = type.Name.IndexOf('`');
            if (nameEndPos == -1)
                nameEndPos = type.Name.Length;

            return $"{type.Name[..nameEndPos]}<{string.Join(",", type.GetGenericArguments().Select(t => GetReadableTypeName(t, fullName)))}>{stars}";
        }

        if (type.IsUnmanagedFunctionPointer)
        {
            var argTypes = type.GetFunctionPointerParameterTypes();
            var argTypeStr = argTypes.Length > 0
                                 ? string.Join(", ", argTypes.Select(argType => GetReadableTypeName(argType, fullName)))
                                 : string.Empty;
            var retType = GetReadableTypeName(type.GetFunctionPointerReturnType(), fullName);
            return $"delegate* unmanaged<{(string.IsNullOrEmpty(argTypeStr) ? string.Empty : argTypeStr + ", ")}{retType}>";
        }

        return (fullName ? type.FullName ?? type.Name : type.Name) + stars;
    }

    /// <summary>
    /// Draws the UiDebug tool's interface and contents.
    /// </summary>
    internal void Draw()
    {
        PopoutWindows.Draw();
        this.DrawSidebar();
        this.DrawMainPanel();
    }

    private void DrawMainPanel()
    {
        ImGui.SameLine();

        using var ch = ImRaii.Child("###uiDebugMainPanel"u8, new(-1, -1), true, HorizontalScrollbar);

        if (ch.Success)
        {
            if (this.elementSelector.Active)
            {
                this.elementSelector.DrawSelectorOutput();
            }
            else
            {
                if (this.SelectedAddonName != null)
                {
                    var addonTree = AddonTree.GetOrCreate(this.SelectedAddonName);

                    if (addonTree == null)
                    {
                        this.SelectedAddonName = null;
                        return;
                    }

                    addonTree.Draw();
                }
            }
        }
    }
}
