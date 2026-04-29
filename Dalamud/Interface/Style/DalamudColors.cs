using System.Numerics;

using Dalamud.Interface.Colors;

using Newtonsoft.Json;

namespace Dalamud.Interface.Style;
#pragma warning disable SA1600

public class DalamudColors
{
    [JsonProperty("a")]
    public Vector4? DalamudRed { get; set; }

    [JsonProperty("b")]
    public Vector4? DalamudGrey { get; set; }

    [JsonProperty("c")]
    public Vector4? DalamudGrey2 { get; set; }

    [JsonProperty("d")]
    public Vector4? DalamudGrey3 { get; set; }

    [JsonProperty("e")]
    public Vector4? DalamudWhite { get; set; }

    [JsonProperty("f")]
    public Vector4? DalamudWhite2 { get; set; }

    [JsonProperty("g")]
    public Vector4? DalamudOrange { get; set; }

    [JsonProperty("h")]
    public Vector4? TankBlue { get; set; }

    [JsonProperty("i")]
    public Vector4? HealerGreen { get; set; }

    [JsonProperty("j")]
    public Vector4? DPSRed { get; set; }

    [JsonProperty("k")]
    public Vector4? DalamudYellow { get; set; }

    [JsonProperty("l")]
    public Vector4? DalamudViolet { get; set; }

    [JsonProperty("m")]
    public Vector4? ParsedGrey { get; set; }

    [JsonProperty("n")]
    public Vector4? ParsedGreen { get; set; }

    [JsonProperty("o")]
    public Vector4? ParsedBlue { get; set; }

    [JsonProperty("p")]
    public Vector4? ParsedPurple { get; set; }

    [JsonProperty("q")]
    public Vector4? ParsedOrange { get; set; }

    [JsonProperty("r")]
    public Vector4? ParsedPink { get; set; }

    [JsonProperty("s")]
    public Vector4? ParsedGold { get; set; }

    [JsonProperty("t")]
    public Vector4? InfoForeground { get; set; }

    [JsonProperty("u")]
    public Vector4? InfoBackground { get; set; }

    [JsonProperty("v")]
    public Vector4? SuccessForeground { get; set; }

    [JsonProperty("w")]
    public Vector4? SuccessBackground { get; set; }

    [JsonProperty("x")]
    public Vector4? WarningForeground { get; set; }

    [JsonProperty("y")]
    public Vector4? WarningBackground { get; set; }

    [JsonProperty("z")]
    public Vector4? ErrorForeground { get; set; }

    [JsonProperty("aa")]
    public Vector4? ErrorBackground { get; set; }

    [JsonProperty("ab")]
    public Vector4? AttentionForeground { get; set; }

    [JsonProperty("ac")]
    public Vector4? AttentionBackground { get; set; }

    public void Apply()
    {
        if (this.DalamudRed.HasValue)
        {
            ImGuiColors.DalamudRed = this.DalamudRed.Value;
        }

        if (this.DalamudGrey.HasValue)
        {
            ImGuiColors.DalamudGrey = this.DalamudGrey.Value;
        }

        if (this.DalamudGrey2.HasValue)
        {
            ImGuiColors.DalamudGrey2 = this.DalamudGrey2.Value;
        }

        if (this.DalamudGrey3.HasValue)
        {
            ImGuiColors.DalamudGrey3 = this.DalamudGrey3.Value;
        }

        if (this.DalamudWhite.HasValue)
        {
            ImGuiColors.DalamudWhite = this.DalamudWhite.Value;
        }

        if (this.DalamudWhite2.HasValue)
        {
            ImGuiColors.DalamudWhite2 = this.DalamudWhite2.Value;
        }

        if (this.DalamudOrange.HasValue)
        {
            ImGuiColors.DalamudOrange = this.DalamudOrange.Value;
        }

        if (this.TankBlue.HasValue)
        {
            ImGuiColors.TankBlue = this.TankBlue.Value;
        }

        if (this.HealerGreen.HasValue)
        {
            ImGuiColors.HealerGreen = this.HealerGreen.Value;
        }

        if (this.DPSRed.HasValue)
        {
            ImGuiColors.DPSRed = this.DPSRed.Value;
        }

        if (this.DalamudYellow.HasValue)
        {
            ImGuiColors.DalamudYellow = this.DalamudYellow.Value;
        }

        if (this.DalamudViolet.HasValue)
        {
            ImGuiColors.DalamudViolet = this.DalamudViolet.Value;
        }

        if (this.ParsedGrey.HasValue)
        {
            ImGuiColors.ParsedGrey = this.ParsedGrey.Value;
        }

        if (this.ParsedGreen.HasValue)
        {
            ImGuiColors.ParsedGreen = this.ParsedGreen.Value;
        }

        if (this.ParsedBlue.HasValue)
        {
            ImGuiColors.ParsedBlue = this.ParsedBlue.Value;
        }

        if (this.ParsedPurple.HasValue)
        {
            ImGuiColors.ParsedPurple = this.ParsedPurple.Value;
        }

        if (this.ParsedOrange.HasValue)
        {
            ImGuiColors.ParsedOrange = this.ParsedOrange.Value;
        }

        if (this.ParsedPink.HasValue)
        {
            ImGuiColors.ParsedPink = this.ParsedPink.Value;
        }

        if (this.ParsedGold.HasValue)
        {
            ImGuiColors.ParsedGold = this.ParsedGold.Value;
        }

        if (this.InfoForeground.HasValue)
        {
            ImGuiColors.InfoForeground = this.InfoForeground.Value;
        }

        if (this.InfoBackground.HasValue)
        {
            ImGuiColors.InfoBackground = this.InfoBackground.Value;
        }

        if (this.SuccessForeground.HasValue)
        {
            ImGuiColors.SuccessForeground = this.SuccessForeground.Value;
        }

        if (this.SuccessBackground.HasValue)
        {
            ImGuiColors.SuccessBackground = this.SuccessBackground.Value;
        }

        if (this.WarningForeground.HasValue)
        {
            ImGuiColors.WarningForeground = this.WarningForeground.Value;
        }

        if (this.WarningBackground.HasValue)
        {
            ImGuiColors.WarningBackground = this.WarningBackground.Value;
        }

        if (this.ErrorForeground.HasValue)
        {
            ImGuiColors.ErrorForeground = this.ErrorForeground.Value;
        }

        if (this.ErrorBackground.HasValue)
        {
            ImGuiColors.ErrorBackground = this.ErrorBackground.Value;
        }

        if (this.AttentionForeground.HasValue)
        {
            ImGuiColors.AttentionForeground = this.AttentionForeground.Value;
        }

        if (this.AttentionBackground.HasValue)
        {
            ImGuiColors.AttentionBackground = this.AttentionBackground.Value;
        }
    }
}

#pragma warning restore SA1600
