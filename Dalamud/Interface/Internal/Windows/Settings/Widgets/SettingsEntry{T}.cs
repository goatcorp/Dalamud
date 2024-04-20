using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Dalamud.Configuration.Internal;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class SettingsEntry<T> : SettingsEntry
{
    private readonly LoadSettingDelegate load;
    private readonly SaveSettingDelegate save;
    private readonly Action<T?>? change;

    private object? valueBacking;
    private object? fallbackValue;

    public SettingsEntry(
        string name,
        string description,
        LoadSettingDelegate load,
        SaveSettingDelegate save,
        Action<T?>? change = null,
        Func<T?, string?>? warning = null,
        Func<T?, string?>? validity = null,
        Func<bool>? visibility = null, 
        object? fallbackValue = null)
    {
        this.load = load;
        this.save = save;
        this.change = change;
        this.Name = name;
        this.Description = description;
        this.CheckWarning = warning;
        this.CheckValidity = validity;
        this.CheckVisibility = visibility;

        this.fallbackValue = fallbackValue;
    }

    public delegate T? LoadSettingDelegate(DalamudConfiguration config);

    public delegate void SaveSettingDelegate(T? value, DalamudConfiguration config);

    public T? Value => this.valueBacking == default ? default : (T)this.valueBacking;

    public string Description { get; }

    public Func<T?, string?>? CheckValidity { get; init; }

    public Func<T?, string?>? CheckWarning { get; init; }

    public Func<bool>? CheckVisibility { get; init; }

    public override bool IsVisible => this.CheckVisibility?.Invoke() ?? true;

    public override void Draw()
    {
        Debug.Assert(this.Name != null, "this.Name != null");

        var type = typeof(T);

        if (type == typeof(DirectoryInfo))
        {
            ImGuiHelpers.SafeTextWrapped(this.Name);

            var value = this.Value as DirectoryInfo;
            var nativeBuffer = value?.FullName ?? string.Empty;

            if (ImGui.InputText($"###{this.Id.ToString()}", ref nativeBuffer, 1000))
            {
                this.valueBacking = !string.IsNullOrEmpty(nativeBuffer) ? new DirectoryInfo(nativeBuffer) : null;
            }
        }
        else if (type == typeof(string))
        {
            ImGuiHelpers.SafeTextWrapped(this.Name);

            var nativeBuffer = this.Value as string ?? string.Empty;

            if (ImGui.InputText($"###{this.Id.ToString()}", ref nativeBuffer, 1000))
            {
                this.valueBacking = nativeBuffer;
            }
        }
        else if (type == typeof(bool))
        {
            var nativeValue = this.Value as bool? ?? false;

            if (ImGui.Checkbox($"{this.Name}###{this.Id.ToString()}", ref nativeValue))
            {
                this.valueBacking = nativeValue;
                this.change?.Invoke(this.Value);
            }
        }
        else if (type.IsEnum)
        {
            ImGuiHelpers.SafeTextWrapped(this.Name);

            var idx = (Enum)(this.valueBacking ?? 0);
            var values = Enum.GetValues(type);
            var descriptions =
                values.Cast<Enum>().ToDictionary(x => x, x => x.GetAttribute<SettingsAnnotationAttribute>() ?? new SettingsAnnotationAttribute(x.ToString(), string.Empty));

            if (!descriptions.ContainsKey(idx))
            {
                idx = (Enum)this.fallbackValue ?? throw new Exception("No fallback value for enum");
                this.valueBacking = idx;
            }

            if (ImGui.BeginCombo($"###{this.Id.ToString()}", descriptions[idx].FriendlyName))
            {
                foreach (Enum value in values)
                {
                    if (ImGui.Selectable(descriptions[value].FriendlyName, idx.Equals(value)))
                    {
                        this.valueBacking = value;
                    }
                }

                ImGui.EndCombo();
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
        {
            ImGuiHelpers.SafeTextWrapped(this.Description);
        }

        if (this.CheckValidity != null)
        {
            var validityMsg = this.CheckValidity.Invoke(this.Value);
            this.IsValid = string.IsNullOrEmpty(validityMsg);

            if (!this.IsValid)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
                {
                    ImGui.Text(validityMsg);
                }
            }
        }
        else
        {
            this.IsValid = true;
        }

        var warningMessage = this.CheckWarning?.Invoke(this.Value);

        if (warningMessage != null)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed))
            {
                ImGui.Text(warningMessage);
            }
        }
    }

    public override void Load()
    {
        this.valueBacking = this.load(Service<DalamudConfiguration>.Get());

        if (this.CheckValidity != null)
        {
            this.IsValid = this.CheckValidity(this.Value) == null;
        }
        else
        {
            this.IsValid = true;
        }
    }

    public override void Save() => this.save(this.Value, Service<DalamudConfiguration>.Get());
}

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
[AttributeUsage(AttributeTargets.Field)]
internal class SettingsAnnotationAttribute : Attribute
{
    public SettingsAnnotationAttribute(string friendlyName, string description)
    {
        this.FriendlyName = friendlyName;
        this.Description = description;
    }

    public string FriendlyName { get; set; }

    public string Description { get; set; }
}
