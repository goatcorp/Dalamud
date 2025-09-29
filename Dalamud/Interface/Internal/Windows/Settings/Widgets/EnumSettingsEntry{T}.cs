using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class EnumSettingsEntry<T> : SettingsEntry
    where T : struct, Enum
{
    private readonly LoadSettingDelegate load;
    private readonly SaveSettingDelegate save;
    private readonly Action<T>? change;

    private readonly T fallbackValue;

    private T valueBacking;

    public EnumSettingsEntry(
        LazyLoc name,
        LazyLoc description,
        LoadSettingDelegate load,
        SaveSettingDelegate save,
        Action<T>? change = null,
        Func<T, string?>? warning = null,
        Func<T, string?>? validity = null,
        Func<bool>? visibility = null,
        T fallbackValue = default)
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

    public delegate T LoadSettingDelegate(DalamudConfiguration config);

    public delegate void SaveSettingDelegate(T value, DalamudConfiguration config);

    public T Value
    {
        get => this.valueBacking;
        set
        {
            if (Equals(value, this.valueBacking))
                return;
            this.valueBacking = value;
            this.change?.Invoke(value);
        }
    }

    public LazyLoc Description { get; }

    public Action<EnumSettingsEntry<T>>? CustomDraw { get; init; }

    public Func<T, string?>? CheckValidity { get; init; }

    public Func<T, string?>? CheckWarning { get; init; }

    public Func<bool>? CheckVisibility { get; init; }

    public Func<T, string> FriendlyEnumNameGetter { get; init; } = x => x.ToString();

    public Func<T, string> FriendlyEnumDescriptionGetter { get; init; } = _ => string.Empty;

    public override bool IsVisible => this.CheckVisibility?.Invoke() ?? true;

    public override void Draw()
    {
        var name = this.Name.ToString();
        var description = this.Description.ToString();

        Debug.Assert(!string.IsNullOrWhiteSpace(name), "Name is empty");

        if (this.CustomDraw is not null)
        {
            this.CustomDraw.Invoke(this);
        }
        else
        {
            ImGui.TextWrapped(name);

            var idx = this.valueBacking;
            var values = Enum.GetValues<T>();

            if (!values.Contains(idx))
            {
                idx = Enum.IsDefined(this.fallbackValue)
                          ? this.fallbackValue
                          : throw new InvalidOperationException("No fallback value for enum");
                this.valueBacking = idx;
            }

            if (ImGui.BeginCombo($"###{this.Id.ToString()}", this.FriendlyEnumNameGetter(idx)))
            {
                foreach (var value in values)
                {
                    if (ImGui.Selectable(this.FriendlyEnumNameGetter(value), idx.Equals(value)))
                    {
                        this.valueBacking = value;
                    }
                }

                ImGui.EndCombo();
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
        {
            var desc = this.FriendlyEnumDescriptionGetter(this.valueBacking);

            if (!string.IsNullOrWhiteSpace(desc))
            {
                ImGui.TextWrapped(desc);
                ImGuiHelpers.ScaledDummy(2);
            }

            ImGui.TextWrapped(description);
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
