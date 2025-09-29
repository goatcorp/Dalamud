using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class SettingsEntry<T> : SettingsEntry
{
    private readonly LoadSettingDelegate load;
    private readonly SaveSettingDelegate save;
    private readonly Action<T?>? change;

    private object? valueBacking;

    public SettingsEntry(
        LazyLoc name,
        LazyLoc description,
        LoadSettingDelegate load,
        SaveSettingDelegate save,
        Action<T?>? change = null,
        Func<T?, string?>? warning = null,
        Func<T?, string?>? validity = null,
        Func<bool>? visibility = null)
    {
        this.load = load;
        this.save = save;
        this.change = change;
        this.Name = name;
        this.Description = description;
        this.CheckWarning = warning;
        this.CheckValidity = validity;
        this.CheckVisibility = visibility;
    }

    public delegate T? LoadSettingDelegate(DalamudConfiguration config);

    public delegate void SaveSettingDelegate(T? value, DalamudConfiguration config);

    public T? Value
    {
        get => this.valueBacking == default ? default : (T)this.valueBacking;
        set
        {
            if (Equals(value, this.valueBacking))
                return;
            this.valueBacking = value;
            this.change?.Invoke(value);
        }
    }

    public LazyLoc Description { get; }

    public Action<SettingsEntry<T>>? CustomDraw { get; init; }

    public Func<T?, string?>? CheckValidity { get; init; }

    public Func<T?, string?>? CheckWarning { get; init; }

    public Func<bool>? CheckVisibility { get; init; }

    public override bool IsVisible => this.CheckVisibility?.Invoke() ?? true;

    public override void Draw()
    {
        var name = this.Name.ToString();
        var description = this.Description.ToString();

        Debug.Assert(!string.IsNullOrWhiteSpace(name), "Name is empty");

        var type = typeof(T);

        if (this.CustomDraw is not null)
        {
            this.CustomDraw.Invoke(this);
        }
        else if (type == typeof(DirectoryInfo))
        {
            ImGui.TextWrapped(name);

            var value = this.Value as DirectoryInfo;
            var nativeBuffer = value?.FullName ?? string.Empty;

            if (ImGui.InputText($"###{this.Id.ToString()}", ref nativeBuffer, 1000))
            {
                this.valueBacking = !string.IsNullOrEmpty(nativeBuffer) ? new DirectoryInfo(nativeBuffer) : null;
            }
        }
        else if (type == typeof(string))
        {
            ImGui.TextWrapped(name);

            var nativeBuffer = this.Value as string ?? string.Empty;

            if (ImGui.InputText($"###{this.Id.ToString()}", ref nativeBuffer, 1000))
            {
                this.valueBacking = nativeBuffer;
            }
        }
        else if (type == typeof(bool))
        {
            var nativeValue = this.Value as bool? ?? false;

            if (ImGui.Checkbox($"{name}###{this.Id.ToString()}", ref nativeValue))
            {
                this.valueBacking = nativeValue;
                this.change?.Invoke(this.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            {
                ImGui.TextWrapped(this.Description);
            }
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
