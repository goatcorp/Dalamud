using System.Collections.Concurrent;
using System.Diagnostics;

using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using Serilog;

namespace Dalamud.Game.Config;

/// <summary>
/// Represents a section of the game config and contains helper functions for accessing and setting values.
/// </summary>
public class GameConfigSection
{
    private readonly Framework framework;
    private readonly ConcurrentDictionary<string, uint> indexMap = new();
    private readonly ConcurrentDictionary<uint, object> enumMap = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GameConfigSection"/> class.
    /// </summary>
    /// <param name="sectionName">Name of the section.</param>
    /// <param name="framework">The framework service.</param>
    /// <param name="configBase">Unmanaged ConfigBase instance.</param>
    internal unsafe GameConfigSection(string sectionName, Framework framework, ConfigBase* configBase)
        : this(sectionName, framework, () => configBase)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameConfigSection"/> class.
    /// </summary>
    /// <param name="sectionName">Name of the section.</param>
    /// <param name="framework">The framework service.</param>
    /// <param name="getConfigBase">A function that determines which ConfigBase instance should be used.</param>
    internal GameConfigSection(string sectionName, Framework framework, GetConfigBaseDelegate getConfigBase)
    {
        this.SectionName = sectionName;
        this.framework = framework;
        this.GetConfigBase = getConfigBase;
        Log.Verbose("[GameConfig] Initalizing {SectionName} with {ConfigCount} entries.", this.SectionName, this.ConfigCount);
    }

    /// <summary>
    /// Delegate that gets the struct the section accesses.
    /// </summary>
    /// <returns>Pointer to unmanaged ConfigBase.</returns>
    internal unsafe delegate ConfigBase* GetConfigBaseDelegate();

    /// <summary>
    /// Event which is fired when a game config option is changed within the section.
    /// </summary>
    internal event EventHandler<ConfigChangeEvent>? Changed; 

    /// <summary>
    /// Gets the number of config entries contained within the section.
    /// Some entries may be empty with no data.
    /// </summary>
    public unsafe uint ConfigCount => this.GetConfigBase()->ConfigCount;

    /// <summary>
    /// Gets the name of the config section.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Gets the pointer to the config section container.
    /// </summary>
    internal GetConfigBaseDelegate GetConfigBase { get; }

    /// <summary>
    /// Attempts to get a boolean config option.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public unsafe bool TryGetBool(string name, out bool value)
    {
        value = false;
        if (!this.TryGetIndex(name, out var index))
        {
            return false;
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            return false;
        }

        value = entry->Value.UInt != 0;
        return true;
    }

    /// <summary>
    /// Attempts to get a boolean config option.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(string name, out bool value) => this.TryGetBool(name, out value);

    /// <summary>
    /// Get a boolean config option.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <returns>Value of the config option.</returns>
    /// <exception cref="ConfigOptionNotFoundException">Thrown if the config option is not found.</exception>
    public bool GetBool(string name)
    {
        if (!this.TryGetBool(name, out var value))
        {
            throw new ConfigOptionNotFoundException(this.SectionName, name);
        }

        return value;
    }

    /// <summary>
    /// Set a boolean config option.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public unsafe void Set(string name, bool value)
    {
        if (!this.TryGetIndex(name, out var index))
        {
            throw new ConfigOptionNotFoundException(this.SectionName, name);
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            throw new UnreachableException($"An unexpected error was encountered setting {name} in {this.SectionName}");
        }

        if ((ConfigType)entry->Type != ConfigType.UInt)
        {
            throw new IncorrectConfigTypeException(this.SectionName, name, (ConfigType)entry->Type, ConfigType.UInt);
        }

        entry->SetValue(value ? 1U : 0U);
    }

    /// <summary>
    /// Attempts to get an unsigned integer config value.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public unsafe bool TryGetUInt(string name, out uint value)
    {
        value = 0;
        if (!this.TryGetIndex(name, out var index))
        {
            return false;
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            return false;
        }

        value = entry->Value.UInt;
        return true;
    }

    /// <summary>
    /// Attempts to get an unsigned integer config value.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(string name, out uint value) => this.TryGetUInt(name, out value);

    /// <summary>
    /// Get an unsigned integer config option.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <returns>Value of the config option.</returns>
    /// <exception cref="ConfigOptionNotFoundException">Thrown if the config option is not found.</exception>
    public uint GetUInt(string name)
    {
        if (!this.TryGetUInt(name, out var value))
        {
            throw new ConfigOptionNotFoundException(this.SectionName, name);
        }

        return value;
    }

    /// <summary>
    /// Set an unsigned integer config option.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public unsafe void Set(string name, uint value)
    {
        this.framework.RunOnFrameworkThread(() =>
        {
            if (!this.TryGetIndex(name, out var index))
            {
                throw new ConfigOptionNotFoundException(this.SectionName, name);
            }

            if (!this.TryGetEntry(index, out var entry))
            {
                throw new UnreachableException($"An unexpected error was encountered setting {name} in {this.SectionName}");
            }

            if ((ConfigType)entry->Type != ConfigType.UInt)
            {
                throw new IncorrectConfigTypeException(this.SectionName, name, (ConfigType)entry->Type, ConfigType.UInt);
            }

            entry->SetValue(value);
        });
    }

    /// <summary>
    /// Attempts to get a float config value.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public unsafe bool TryGetFloat(string name, out float value)
    {
        value = 0;
        if (!this.TryGetIndex(name, out var index))
        {
            return false;
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            return false;
        }

        value = entry->Value.Float;
        return true;
    }

    /// <summary>
    /// Attempts to get a float config value.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(string name, out float value) => this.TryGetFloat(name, out value);

    /// <summary>
    /// Get a float config option.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <returns>Value of the config option.</returns>
    /// <exception cref="ConfigOptionNotFoundException">Thrown if the config option is not found.</exception>
    public float GetFloat(string name)
    {
        if (!this.TryGetFloat(name, out var value))
        {
            throw new ConfigOptionNotFoundException(this.SectionName, name);
        }

        return value;
    }

    /// <summary>
    /// Set a float config option.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public unsafe void Set(string name, float value)
    {
        this.framework.RunOnFrameworkThread(() =>
        {
            if (!this.TryGetIndex(name, out var index))
            {
                throw new ConfigOptionNotFoundException(this.SectionName, name);
            }

            if (!this.TryGetEntry(index, out var entry))
            {
                throw new UnreachableException($"An unexpected error was encountered setting {name} in {this.SectionName}");
            }

            if ((ConfigType)entry->Type != ConfigType.Float)
            {
                throw new IncorrectConfigTypeException(this.SectionName, name, (ConfigType)entry->Type, ConfigType.Float);
            }

            entry->SetValue(value);
        });
    }

    /// <summary>
    /// Attempts to get a string config value.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public unsafe bool TryGetString(string name, out string value)
    {
        value = string.Empty;
        if (!this.TryGetIndex(name, out var index))
        {
            return false;
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            return false;
        }

        if (entry->Type != 4)
        {
            return false;
        }

        if (entry->Value.String == null)
        {
            return false;
        }

        value = entry->Value.String->ToString();
        return true;
    }

    /// <summary>
    /// Attempts to get a string config value.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">The returned value of the config option.</param>
    /// <returns>A value representing the success.</returns>
    public bool TryGet(string name, out string value) => this.TryGetString(name, out value);

    /// <summary>
    /// Get a string config option.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <returns>Value of the config option.</returns>
    /// <exception cref="ConfigOptionNotFoundException">Thrown if the config option is not found.</exception>
    public string GetString(string name)
    {
        if (!this.TryGetString(name, out var value))
        {
            throw new ConfigOptionNotFoundException(this.SectionName, name);
        }

        return value;
    }

    /// <summary>
    /// Set a string config option.
    /// Note: Not all config options will be be immediately reflected in the game.
    /// </summary>
    /// <param name="name">Name of the config option.</param>
    /// <param name="value">New value of the config option.</param>
    /// <exception cref="ConfigOptionNotFoundException">Throw if the config option is not found.</exception>
    /// <exception cref="UnreachableException">Thrown if the name of the config option is found, but the struct was not.</exception>
    public unsafe void Set(string name, string value)
    {
        this.framework.RunOnFrameworkThread(() =>
        {
            if (!this.TryGetIndex(name, out var index))
            {
                throw new ConfigOptionNotFoundException(this.SectionName, name);
            }

            if (!this.TryGetEntry(index, out var entry))
            {
                throw new UnreachableException($"An unexpected error was encountered setting {name} in {this.SectionName}");
            }

            if ((ConfigType)entry->Type != ConfigType.String)
            {
                throw new IncorrectConfigTypeException(this.SectionName, name, (ConfigType)entry->Type, ConfigType.String);
            }

            entry->SetValue(value);
        });
    }

    /// <summary>
    /// Attempts to get the properties of a UInt option from the config section.
    /// </summary>
    /// <param name="name">Name of the option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public unsafe bool TryGetProperties(string name, out UIntConfigProperties? properties)
    {
        if (!this.TryGetIndex(name, out var index))
        {
            properties = null;
            return false;
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            properties = null;
            return false;
        }

        if ((ConfigType)entry->Type != ConfigType.UInt)
        {
            properties = null;
            return false;
        }

        var prop = &entry->Properties.UInt;
        properties = new UIntConfigProperties(prop->DefaultValue, prop->MinValue, prop->MaxValue);
        return true;
    }

    /// <summary>
    /// Attempts to get the properties of a Float option from the config section.
    /// </summary>
    /// <param name="name">Name of the option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public unsafe bool TryGetProperties(string name, out FloatConfigProperties? properties)
    {
        if (!this.TryGetIndex(name, out var index))
        {
            properties = null;
            return false;
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            properties = null;
            return false;
        }

        if ((ConfigType)entry->Type != ConfigType.Float)
        {
            properties = null;
            return false;
        }

        var prop = &entry->Properties.Float;
        properties = new FloatConfigProperties(prop->DefaultValue, prop->MinValue, prop->MaxValue);
        return true;
    }

    /// <summary>
    /// Attempts to get the properties of a String option from the config section.
    /// </summary>
    /// <param name="name">Name of the option to get the properties of.</param>
    /// <param name="properties">Details of the option: Minimum, Maximum, and Default values.</param>
    /// <returns>A value representing the success.</returns>
    public unsafe bool TryGetProperties(string name, out StringConfigProperties? properties)
    {
        if (!this.TryGetIndex(name, out var index))
        {
            properties = null;
            return false;
        }

        if (!this.TryGetEntry(index, out var entry))
        {
            properties = null;
            return false;
        }

        if ((ConfigType)entry->Type != ConfigType.String)
        {
            properties = null;
            return false;
        }

        var prop = entry->Properties.String;
        properties = new StringConfigProperties(prop.DefaultValue == null ? null : MemoryHelper.ReadSeString(prop.DefaultValue));
        return true;
    }

    /// <summary>
    /// Invokes a change event within the config section.
    /// </summary>
    /// <param name="entry">The config entry that was changed.</param>
    /// <typeparam name="TEnum">SystemConfigOption, UiConfigOption, or UiControlOption.</typeparam>
    /// <returns>The ConfigChangeEvent record.</returns>
    internal unsafe ConfigChangeEvent? InvokeChange<TEnum>(ConfigEntry* entry) where TEnum : Enum
    {
        if (!this.enumMap.TryGetValue(entry->Index, out var enumObject))
        {
            if (entry->Name == null) return null;
            var name = MemoryHelper.ReadStringNullTerminated(new IntPtr(entry->Name));
            if (Enum.TryParse(typeof(TEnum), name, out enumObject))
            {
                this.enumMap.TryAdd(entry->Index, enumObject);
            }
            else
            {
                enumObject = null;
                this.enumMap.TryAdd(entry->Index, null);
            }
        }

        if (enumObject == null) return null;
        var eventArgs = new ConfigChangeEvent<TEnum>((TEnum)enumObject);
        this.Changed?.InvokeSafely(this, eventArgs);
        return eventArgs;
    }
    
    private unsafe bool TryGetIndex(string name, out uint index)
    {
        if (this.indexMap.TryGetValue(name, out index))
        {
            return true;
        }

        var configBase = this.GetConfigBase();
        var e = configBase->ConfigEntry;
        for (var i = 0U; i < configBase->ConfigCount; i++, e++)
        {
            if (e->Name == null)
            {
                continue;
            }

            var eName = MemoryHelper.ReadStringNullTerminated(new IntPtr(e->Name));
            if (eName.Equals(name))
            {
                this.indexMap.TryAdd(name, i);
                index = i;
                return true;
            }
        }

        index = 0;
        return false;
    }

    private unsafe bool TryGetEntry(uint index, out ConfigEntry* entry)
    {
        entry = null;
        var configBase = this.GetConfigBase();
        if (configBase->ConfigEntry == null || index >= configBase->ConfigCount)
        {
            return false;
        }

        entry = configBase->ConfigEntry;
        entry += index;
        return true;
    }
}
