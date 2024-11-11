using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Component.GUI;

using static System.Reflection.BindingFlags;
using static Dalamud.Interface.Internal.UiDebug2.UiDebug2;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <inheritdoc cref="AddonTree"/>
public unsafe partial class AddonTree
{
    private static readonly Dictionary<string, Type?> AddonTypeDict = [];

    private static readonly Assembly? ClientStructsAssembly = typeof(Addon).Assembly;

    /// <summary>
    /// Gets or sets a collection of names for field offsets that have been documented in FFXIVClientStructs.
    /// </summary>
    internal Dictionary<nint, List<string>> FieldNames { get; set; } = [];

    private object? GetAddonObj(AtkUnitBase* addon)
    {
        if (addon == null)
        {
            return null;
        }

        if (AddonTypeDict.TryAdd(this.AddonName, null) && ClientStructsAssembly != null)
        {
            try
            {
                foreach (var t in from t in ClientStructsAssembly.GetTypes()
                                  where t.IsPublic
                                  let xivAddonAttr = (Addon?)t.GetCustomAttribute(typeof(Addon), false)
                                  where xivAddonAttr != null
                                  where xivAddonAttr.AddonIdentifiers.Contains(this.AddonName)
                                  select t)
                {
                    AddonTypeDict[this.AddonName] = t;
                    break;
                }
            }
            catch
            {
                // ignored
            }
        }

        return AddonTypeDict.TryGetValue(this.AddonName, out var result) && result != null ? Marshal.PtrToStructure(new(addon), result) : *addon;
    }

    private void PopulateFieldNames(nint ptr)
    {
        this.PopulateFieldNames(this.GetAddonObj((AtkUnitBase*)ptr), ptr);
    }

    private void PopulateFieldNames(object? obj, nint baseAddr, List<string>? path = null)
    {
        if (obj == null)
        {
            return;
        }

        path ??= [];
        var baseType = obj.GetType();

        foreach (var field in baseType.GetFields(Static | Public | NonPublic | Instance))
        {
            if (field.GetCustomAttribute(typeof(FieldOffsetAttribute)) is FieldOffsetAttribute offset)
            {
                try
                {
                    var fieldAddr = baseAddr + offset.Value;
                    var name = field.Name[0] == '_' ? char.ToUpperInvariant(field.Name[1]) + field.Name[2..] : field.Name;
                    var fieldType = field.FieldType;

                    if (!field.IsStatic && fieldType.IsPointer)
                    {
                        var pointer = (nint)Pointer.Unbox((Pointer)field.GetValue(obj)!);
                        var itemType = fieldType.GetElementType();
                        ParsePointer(fieldAddr, pointer, itemType, name);
                    }
                    else if (fieldType.IsExplicitLayout)
                    {
                        ParseExplicitField(fieldAddr, field, fieldType, name);
                    }
                    else if (fieldType.Name.Contains("FixedSizeArray"))
                    {
                        ParseFixedSizeArray(fieldAddr, fieldType, name);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to parse field at {offset.Value:X} in {baseType}!\n{ex}");
                }
            }
        }

        return;

        void ParseExplicitField(nint fieldAddr, FieldInfo field, MemberInfo fieldType, string name)
        {
            try
            {
                if (this.FieldNames.TryAdd(fieldAddr, [..path, name]) && fieldType.DeclaringType == baseType)
                {
                    this.PopulateFieldNames(field.GetValue(obj), fieldAddr, [..path, name]);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to parse explicit field: {fieldType} {name} in {baseType}!\n{ex}");
            }
        }

        void ParseFixedSizeArray(nint fieldAddr, Type fieldType, string name)
        {
            try
            {
                var spanLength = (int)(fieldType.CustomAttributes.ToArray()[0].ConstructorArguments[0].Value ?? 0);

                if (spanLength <= 0)
                {
                    return;
                }

                var itemType = fieldType.UnderlyingSystemType.GenericTypeArguments[0];

                if (!itemType.IsGenericType)
                {
                    var size = Marshal.SizeOf(itemType);
                    for (var i = 0; i < spanLength; i++)
                    {
                        var itemAddr = fieldAddr + (size * i);
                        var itemName = $"{name}[{i}]";

                        this.FieldNames.TryAdd(itemAddr, [..path, itemName]);

                        var item = Marshal.PtrToStructure(itemAddr, itemType);
                        if (itemType.DeclaringType == baseType)
                        {
                            this.PopulateFieldNames(item, itemAddr, [..path, itemName]);
                        }
                    }
                }
                else if (itemType.Name.Contains("Pointer"))
                {
                    itemType = itemType.GenericTypeArguments[0];

                    for (var i = 0; i < spanLength; i++)
                    {
                        var itemAddr = fieldAddr + (0x08 * i);
                        var pointer = Marshal.ReadIntPtr(itemAddr);
                        ParsePointer(itemAddr, pointer, itemType, $"{name}[{i}]");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to parse fixed size array: {fieldType} {name} in {baseType}!\n{ex}");
            }
        }

        void ParsePointer(nint fieldAddr, nint pointer, Type? itemType, string name)
        {
            try
            {
                if (pointer == 0)
                {
                    return;
                }

                this.FieldNames.TryAdd(fieldAddr, [..path, name]);
                this.FieldNames.TryAdd(pointer, [..path, name]);

                if (itemType?.DeclaringType != baseType || itemType.IsPointer)
                {
                    return;
                }

                var item = Marshal.PtrToStructure(pointer, itemType);
                this.PopulateFieldNames(item, pointer, [..path, name]);
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to parse pointer: {itemType}* {name} in {baseType}!\n{ex}");
            }
        }
    }
}
