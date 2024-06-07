using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using ImGuiNET;
using Newtonsoft.Json;
using Serilog;

namespace Dalamud.Interface.Style;

/// <summary>
/// Superclass for all versions of the Dalamud style model.
/// </summary>
public abstract class StyleModel
{
    private static int numPushedStyles = 0;
    private static int numPushedColors = 0;
    private static bool hasPushedOnce = false;

    /// <summary>
    /// Gets or sets the name of the style model.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets class representing Dalamud-builtin <see cref="ImGuiColors"/>.
    /// </summary>
    [JsonProperty("dol")]
    public DalamudColors? BuiltInColors { get; set; }

    /// <summary>
    /// Gets or sets version number of this model.
    /// </summary>
    [JsonProperty("ver")]
    public int Version { get; set; }

    /// <summary>
    /// Get a StyleModel based on the current Dalamud style, with the current version.
    /// </summary>
    /// <returns>The current style.</returns>
    public static StyleModel GetFromCurrent() => StyleModelV1.Get();

    /// <summary>
    /// Get the current style model, as per configuration.
    /// </summary>
    /// <returns>The current style, as per configuration.</returns>
    public static StyleModel? GetConfiguredStyle()
    {
        var configuration = Service<DalamudConfiguration>.Get();
        return configuration.SavedStyles?.FirstOrDefault(x => x.Name == configuration.ChosenStyle);
    }

    /// <summary>
    /// Get an enumerable of all saved styles.
    /// </summary>
    /// <returns>Enumerable of saved styles.</returns>
    public static IEnumerable<StyleModel>? GetConfiguredStyles() => Service<DalamudConfiguration>.Get().SavedStyles;

    /// <summary>
    /// Deserialize a style model.
    /// </summary>
    /// <param name="model">The serialized model.</param>
    /// <returns>The deserialized model.</returns>
    /// <exception cref="ArgumentException">Thrown in case the version of the model is not known.</exception>
    public static StyleModel? Deserialize(string model)
    {
        var json = Util.DecompressString(Convert.FromBase64String(model.Substring(3)));

        if (model.StartsWith(StyleModelV1.SerializedPrefix))
            return JsonConvert.DeserializeObject<StyleModelV1>(json);

        throw new ArgumentException("Was not a compressed style model.");
    }

    /// <summary>
    /// [TEMPORARY] Transfer old non-polymorphic style models to the new format.
    /// </summary>
    public static void TransferOldModels()
    {
        var configuration = Service<DalamudConfiguration>.Get();

        if (configuration.SavedStylesOld == null)
            return;

        configuration.SavedStyles = new List<StyleModel>();
        configuration.SavedStyles.AddRange(configuration.SavedStylesOld);

        Log.Information("Transferred {NumStyles} styles", configuration.SavedStyles.Count);

        configuration.SavedStylesOld = null;
        configuration.QueueSave();
    }

    /// <summary>
    /// Serialize this style model.
    /// </summary>
    /// <returns>Serialized style model as string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the version of the style model is unknown.</exception>
    public string Serialize()
    {
        string prefix;
        switch (this)
        {
            case StyleModelV1:
                prefix = StyleModelV1.SerializedPrefix;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return prefix + Convert.ToBase64String(Util.CompressString(JsonConvert.SerializeObject(this)));
    }

    /// <summary>
    /// Apply this style model to ImGui.
    /// </summary>
    public abstract void Apply();

    /// <summary>
    /// Push this StyleModel into the ImGui style/color stack.
    /// </summary>
    public abstract void Push();

    /// <summary>
    /// Pop this style model from the ImGui style/color stack.
    /// </summary>
    public void Pop()
    {
        if (!hasPushedOnce)
            throw new InvalidOperationException("Wasn't pushed at least once.");

        ImGui.PopStyleVar(numPushedStyles);
        ImGui.PopStyleColor(numPushedColors);
    }

    /// <summary>
    /// Push a style var.
    /// </summary>
    /// <param name="style">Style kind.</param>
    /// <param name="arg">Style var.</param>
    protected void PushStyleHelper(ImGuiStyleVar style, float arg)
    {
        ImGui.PushStyleVar(style, arg);

        if (!hasPushedOnce)
            numPushedStyles++;
    }

    /// <summary>
    /// Push a style var.
    /// </summary>
    /// <param name="style">Style kind.</param>
    /// <param name="arg">Style var.</param>
    protected void PushStyleHelper(ImGuiStyleVar style, Vector2 arg)
    {
        ImGui.PushStyleVar(style, arg);

        if (!hasPushedOnce)
            numPushedStyles++;
    }

    /// <summary>
    /// Push a style color.
    /// </summary>
    /// <param name="color">Color kind.</param>
    /// <param name="value">Color value.</param>
    protected void PushColorHelper(ImGuiCol color, Vector4 value)
    {
        ImGui.PushStyleColor(color, value);

        if (!hasPushedOnce)
            numPushedColors++;
    }

    /// <summary>
    /// Indicate that you have pushed.
    /// </summary>
    protected void DonePushing()
    {
        hasPushedOnce = true;
    }
}
