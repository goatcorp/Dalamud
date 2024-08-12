using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Memory;

using ImGuiNET;

using Lumina.Data.Files;
using Lumina.Data.Parsing.Uld;

using static Lumina.Data.Parsing.Uld.Keyframes;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Displays the full data of the selected ULD element.
/// </summary>
internal class UldWidget : IDataWindowWidget
{
    private int selectedUld;
    private int selectedFrameData;
    private int selectedTimeline;
    private int selectedParts;
    private int selectedUldStyle;
    // ULD styles can be hardcoded for now as they don't add new ones regularly. Can later try and find where to load these from in the game EXE.
    private (string Display, string Location)[] uldStyles = [
        ("Dark", "uld/"),
        ("Light", "uld/light/"),
        ("Classic FF", "uld/third/"),
        ("Clear Blue", "uld/fourth/")
    ];

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "uld" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "ULD";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        UldWidgetData.ReloadStrings();
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var uldString = UldWidgetData.GetUldStrings();
        if (ImGui.Combo("Select Uld", ref this.selectedUld, uldString.Select(t => t.Display).ToArray(), uldString.Length))
            this.selectedFrameData = this.selectedTimeline = this.selectedParts = 0; // reset selected parts when changing ULD
        ImGui.Combo("Uld theme", ref this.selectedUldStyle, this.uldStyles.Select(t => t.Display).ToArray(), this.uldStyles.Length);

        var dataManager = Service<DataManager>.Get();
        var textureManager = Service<TextureManager>.Get();

        var uld = dataManager.GetFile<UldFile>(uldString[this.selectedUld].Loc);

        if (uld == null)
        {
            ImGui.Text("Failed to load ULD file.");
            return;
        }

        if (ImGui.CollapsingHeader("Texture Entries"))
        {
            if (!ImGui.BeginTable("##uldTextureEntries", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                return;
            ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            foreach (var textureEntry in uld.AssetData)
                this.DrawTextureEntry(textureEntry);

            ImGui.EndTable();
        }

        if (ImGui.CollapsingHeader("Timeline"))
        {
            ImGui.SliderInt("Timeline", ref this.selectedTimeline, 0, uld.Timelines.Length - 1);
            this.DrawTimelines(uld.Timelines[this.selectedTimeline]);
        }

        if (ImGui.CollapsingHeader("Parts"))
        {
            ImGui.SliderInt("Parts", ref this.selectedParts, 0, uld.Parts.Length - 1);
            this.DrawParts(uld.Parts[this.selectedParts], uld.AssetData, dataManager, textureManager);
        }
    }

    private unsafe void DrawTextureEntry(UldRoot.TextureEntry textureEntry)
    {
        ImGui.TableNextColumn();
        fixed (char* p = textureEntry.Path)
            ImGui.TextUnformatted(new string(p));
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(textureEntry.Id.ToString());
    }

    private void DrawTimelines(UldRoot.Timeline timeline)
    {
        ImGui.SliderInt("FrameData", ref this.selectedFrameData, 0, timeline.FrameData.Length - 1);
        var frameData = timeline.FrameData[this.selectedFrameData];
        ImGui.TextUnformatted($"FrameInfo: {frameData.StartFrame} -> {frameData.EndFrame}");
        ImGui.Indent();
        foreach (var frameDataKeyGroup in frameData.KeyGroups)
        {
            ImGui.TextUnformatted($"{frameDataKeyGroup.Usage:G} {frameDataKeyGroup.Type:G}");
            foreach (var keyframe in frameDataKeyGroup.Frames)
                this.DrawTimelineKeyGroupFrame(keyframe);
        }

        ImGui.Unindent();
    }

    private void DrawTimelineKeyGroupFrame(IKeyframe frame)
    {
        switch (frame)
        {
            case BaseKeyframeData baseKeyframeData:
                ImGui.TextUnformatted($"Time: {baseKeyframeData.Time} | Interpolation: {baseKeyframeData.Interpolation} | Acceleration: {baseKeyframeData.Acceleration} | Deceleration: {baseKeyframeData.Deceleration}");
                break;
            case Float1Keyframe float1Keyframe:
                this.DrawTimelineKeyGroupFrame(float1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {float1Keyframe.Value}");
                break;
            case Float2Keyframe float2Keyframe:
                this.DrawTimelineKeyGroupFrame(float2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {float2Keyframe.Value[0]} | Value2: {float2Keyframe.Value[1]}");
                break;
            case Float3Keyframe float3Keyframe:
                this.DrawTimelineKeyGroupFrame(float3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {float3Keyframe.Value[0]} | Value2: {float3Keyframe.Value[1]} | Value3: {float3Keyframe.Value[2]}");
                break;
            case SByte1Keyframe sbyte1Keyframe:
                this.DrawTimelineKeyGroupFrame(sbyte1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {sbyte1Keyframe.Value}");
                break;
            case SByte2Keyframe sbyte2Keyframe:
                this.DrawTimelineKeyGroupFrame(sbyte2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {sbyte2Keyframe.Value[0]} | Value2: {sbyte2Keyframe.Value[1]}");
                break;
            case SByte3Keyframe sbyte3Keyframe:
                this.DrawTimelineKeyGroupFrame(sbyte3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {sbyte3Keyframe.Value[0]} | Value2: {sbyte3Keyframe.Value[1]} | Value3: {sbyte3Keyframe.Value[2]}");
                break;
            case Byte1Keyframe byte1Keyframe:
                this.DrawTimelineKeyGroupFrame(byte1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {byte1Keyframe.Value}");
                break;
            case Byte2Keyframe byte2Keyframe:
                this.DrawTimelineKeyGroupFrame(byte2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {byte2Keyframe.Value[0]} | Value2: {byte2Keyframe.Value[1]}");
                break;
            case Byte3Keyframe byte3Keyframe:
                this.DrawTimelineKeyGroupFrame(byte3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {byte3Keyframe.Value[0]} | Value2: {byte3Keyframe.Value[1]} | Value3: {byte3Keyframe.Value[2]}");
                break;
            case Short1Keyframe short1Keyframe:
                this.DrawTimelineKeyGroupFrame(short1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {short1Keyframe.Value}");
                break;
            case Short2Keyframe short2Keyframe:
                this.DrawTimelineKeyGroupFrame(short2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {short2Keyframe.Value[0]} | Value2: {short2Keyframe.Value[1]}");
                break;
            case Short3Keyframe short3Keyframe:
                this.DrawTimelineKeyGroupFrame(short3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {short3Keyframe.Value[0]} | Value2: {short3Keyframe.Value[1]} | Value3: {short3Keyframe.Value[2]}");
                break;
            case UShort1Keyframe ushort1Keyframe:
                this.DrawTimelineKeyGroupFrame(ushort1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {ushort1Keyframe.Value}");
                break;
            case UShort2Keyframe ushort2Keyframe:
                this.DrawTimelineKeyGroupFrame(ushort2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {ushort2Keyframe.Value[0]} | Value2: {ushort2Keyframe.Value[1]}");
                break;
            case UShort3Keyframe ushort3Keyframe:
                this.DrawTimelineKeyGroupFrame(ushort3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {ushort3Keyframe.Value[0]} | Value2: {ushort3Keyframe.Value[1]} | Value3: {ushort3Keyframe.Value[2]}");
                break;
            case Int1Keyframe int1Keyframe:
                this.DrawTimelineKeyGroupFrame(int1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {int1Keyframe.Value}");
                break;
            case Int2Keyframe int2Keyframe:
                this.DrawTimelineKeyGroupFrame(int2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {int2Keyframe.Value[0]} | Value2: {int2Keyframe.Value[1]}");
                break;
            case Int3Keyframe int3Keyframe:
                this.DrawTimelineKeyGroupFrame(int3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {int3Keyframe.Value[0]} | Value2: {int3Keyframe.Value[1]} | Value3: {int3Keyframe.Value[2]}");
                break;
            case UInt1Keyframe uint1Keyframe:
                this.DrawTimelineKeyGroupFrame(uint1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {uint1Keyframe.Value}");
                break;
            case UInt2Keyframe uint2Keyframe:
                this.DrawTimelineKeyGroupFrame(uint2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {uint2Keyframe.Value[0]} | Value2: {uint2Keyframe.Value[1]}");
                break;
            case UInt3Keyframe uint3Keyframe:
                this.DrawTimelineKeyGroupFrame(uint3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {uint3Keyframe.Value[0]} | Value2: {uint3Keyframe.Value[1]} | Value3: {uint3Keyframe.Value[2]}");
                break;
            case Bool1Keyframe bool1Keyframe:
                this.DrawTimelineKeyGroupFrame(bool1Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value: {bool1Keyframe.Value}");
                break;
            case Bool2Keyframe bool2Keyframe:
                this.DrawTimelineKeyGroupFrame(bool2Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {bool2Keyframe.Value[0]} | Value2: {bool2Keyframe.Value[1]}");
                break;
            case Bool3Keyframe bool3Keyframe:
                this.DrawTimelineKeyGroupFrame(bool3Keyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Value1: {bool3Keyframe.Value[0]} | Value2: {bool3Keyframe.Value[1]} | Value3: {bool3Keyframe.Value[2]}");
                break;
            case ColorKeyframe colorKeyframe:
                this.DrawTimelineKeyGroupFrame(colorKeyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | Add: {colorKeyframe.AddRed} {colorKeyframe.AddGreen} {colorKeyframe.AddBlue} | Multiply: {colorKeyframe.MultiplyRed} {colorKeyframe.MultiplyGreen} {colorKeyframe.MultiplyBlue}");
                break;
            case LabelKeyframe labelKeyframe:
                this.DrawTimelineKeyGroupFrame(labelKeyframe.Keyframe);
                ImGui.SameLine(0, 0);
                ImGui.TextUnformatted($" | LabelCommand: {labelKeyframe.LabelCommand} | JumpId: {labelKeyframe.JumpId} | LabelId: {labelKeyframe.LabelId}");
                break;
        }
    }

    private unsafe void DrawParts(UldRoot.PartsData partsData, UldRoot.TextureEntry[] textureEntries, DataManager dataManager, TextureManager textureManager)
    {
        for (var index = 0; index < partsData.Parts.Length; index++)
        {
            ImGui.TextUnformatted($"Index: {index}");
            var partsDataPart = partsData.Parts[index];
            ImGui.SameLine();
            if (textureEntries.All(t => t.Id != partsDataPart.TextureId))
            {
                ImGui.TextUnformatted($"Could not find texture for id {partsDataPart.TextureId}");
                continue;
            }

            var texturePathChars = textureEntries.First(t => t.Id == partsDataPart.TextureId).Path;
            string texturePath;
            fixed (char* p = texturePathChars)
                texturePath = new string(p);
            var texFile = dataManager.GetFile<TexFile>(texturePath.Replace("uld/", this.uldStyles[this.selectedUldStyle].Location));
            if (texFile == null)
            {
                // try loading from default location if not found in selected style
                texFile = dataManager.GetFile<TexFile>(texturePath);
                if (texFile == null)
                {
                    ImGui.TextUnformatted($"Failed to load texture file {texturePath}");
                    continue;
                }
            }
            var wrap = textureManager.CreateFromTexFile(texFile);
            var texSize = new Vector2(texFile.Header.Width, texFile.Header.Height);
            var uv0 = new Vector2(partsDataPart.U, partsDataPart.V);
            var partSize = new Vector2(partsDataPart.W, partsDataPart.H);
            var uv1 = uv0 + partSize;
            ImGui.Image(wrap.ImGuiHandle, partSize, uv0 / texSize, uv1 / texSize);
            wrap.Dispose();
        }
    }
}

/// <summary>
/// Contains the raw data for the ULD widget.
/// </summary>
internal class UldWidgetData
{
    // 48 8D 15 ?? ?? ?? ?? is the part of the signatures that contain the string location offset
    // 48 = 64 bit register prefix
    // 8D = LEA instruction
    // 15 = register to store offset in (RDX in this case as Component::GUI::AtkUnitBase_LoadUldByName name component is loaded from RDX)
    // ?? ?? ?? ?? = offset to string location
    private static readonly (string Sig, nint Offset)[] UldSigLocations = [
        ("45 33 C0 48 8D 15 ?? ?? ?? ?? 48 8B CF 48 8B 5C 24 30 48 83 C4 20 5F E9 ?? ?? ?? ??", 6),
        ("48 8D 15 ?? ?? ?? ?? 45 33 C0 48 8B CE 48 8B 5C ?? ?? 48 8B 74 ?? ?? 48 83 C4 20 5F E9 ?? ?? ?? ??", 3),
        ("48 8D 15 ?? ?? ?? ?? 45 33 C0 48 8B CB 48 83 C4 20 5B E9 ?? ?? ?? ??", 3),
        ("48 8D 15 ?? ?? ?? ?? 41 B9 ?? ?? ?? ?? 45 33 C0 E8 ?? ?? ?? ??", 3),
        ("48 8D 15 ?? ?? ?? ?? 41 B9 ?? ?? ?? ?? 45 33 C0 E9 ?? ?? ?? ??", 3),
        ("48 8D 15 ?? ?? ?? ?? 45 33 C0 48 8B CB E8 ?? ?? ?? ??", 3),
        ("48 8D 15 ?? ?? ?? ?? 41 B0 01 E9 ?? ?? ?? ??", 3),
        ("48 8D 15 ?? ?? ?? ?? 45 33 C0 E9 ?? ?? ?? ??", 3)
    ];

    private static (string Display, string Loc)[]? uldStrings;

    /// <summary>
    /// Gets all known ULD locations in the game based on a few signatures.
    /// </summary>
    /// <returns>Uld locations.</returns>
    internal static (string Display, string Loc)[] GetUldStrings()
    {
        if (uldStrings == null)
            ParseUldStrings();

        return uldStrings!;
    }

    /// <summary>
    /// Reloads the ULD strings.
    /// </summary>
    internal static void ReloadStrings()
    {
        uldStrings = null;
        ParseUldStrings();
    }

    private static void ParseUldStrings()
    {
        // game contains possibly around 1500 ULD files but current sigs only find less than that due to how they are used
        var locations = new List<string>(1000);
        var sigScanner = new SigScanner(Process.GetCurrentProcess().MainModule!);
        foreach (var (uldSig, strLocOffset) in UldSigLocations)
        {
            var eas = sigScanner.ScanAllText(uldSig);
            foreach (var ea in eas)
            {
                var strLoc = ea + strLocOffset;
                // offset instruction is always 4 bytes so need to read as uint and cast to nint for offset calculation
                var offset = (nint)MemoryHelper.Read<uint>(strLoc);
                // strings are always stored as c strings and relative from end of offset instruction
                var str = MemoryHelper.ReadStringNullTerminated(strLoc + 4 + offset);
                locations.Add(str);
            }
        }

        uldStrings = locations.Distinct().Order().Select(t => (t, $"ui/uld/{t}.uld")).ToArray();
    }
}
