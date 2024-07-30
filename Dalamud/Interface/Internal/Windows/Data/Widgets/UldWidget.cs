using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Data;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Lumina.Data;
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

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "uld" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "ULD";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        ImGui.Combo("Select Uld", ref this.selectedUld, UldWidgetData.UldLocations, UldWidgetData.UldLocations.Length);

        var dataManager = Service<DataManager>.Get();
        var textureManager = Service<TextureManager>.Get();

        var uld = dataManager.GetFile<UldFile>(UldWidgetData.UldLocations[this.selectedUld]);

        if (uld == null)
        {
            ImGui.Text("Failed to load ULD file.");
            return;
        }

        if (!ImGui.BeginTable("##uldTextureEntries", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            return;
        ImGui.TableSetupColumn("Path", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableHeadersRow();

        foreach (var textureEntry in uld.AssetData)
            this.DrawTextureEntry(textureEntry);

        ImGui.EndTable();

        if (ImGui.CollapsingHeader("Timelines"))
        {
            ImGui.Columns(2);
            foreach (var uldTimeline in uld.Timelines)
                this.DrawTimelines(uldTimeline);
            ImGui.Columns(1);
        }

        foreach (var partsData in uld.Parts)
            this.DrawParts(partsData, uld.AssetData, dataManager, textureManager);
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
        if (ImGui.CollapsingHeader($"Uld Timeline {timeline.Id}"))
        {
            foreach (var frameData in timeline.FrameData)
            {
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
        }

        ImGui.NextColumn();
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
        if (ImGui.CollapsingHeader($"Parts {partsData.Id}"))
        {
            for (var index = 0; index < partsData.Parts.Length; index++)
            {
                ImGui.TextUnformatted($"Index: {index}");
                ImGui.SameLine();
                var partsDataPart = partsData.Parts[index];
                if (textureEntries.All(t => t.Id != partsDataPart.TextureId))
                {
                    ImGui.TextUnformatted($"Could not find texture for id {partsDataPart.TextureId}");
                    continue;
                }

                var texturePathChars = textureEntries.First(t => t.Id == partsDataPart.TextureId).Path;
                string texturePath;
                fixed (char* p = texturePathChars)
                    texturePath = new string(p);
                var texFile = dataManager.GetFile<TexFile>(texturePath);
                if (texFile == null) continue;
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
}

/// <summary>
/// Contains the raw data for the ULD widget.
/// </summary>
internal class UldWidgetData
{
    /// <summary>
    /// All known uld locations referenced in executable.
    /// Generated from FFXIVClientStructs\ida\ffxiv_extract_uld_names.py as of 7.01
    /// </summary>
    #pragma warning disable SA1401 // Fields should be private - this needs to be internal or public for above class to access it
    internal static string[] UldLocations =
    [
        "ui/uld/Achievement.uld",
        "ui/uld/AchievementInfo.uld",
        "ui/uld/AchievementSetting.uld",
        "ui/uld/ActionBar.uld",
        "ui/uld/ActionBarCustom.uld",
        "ui/uld/ActionBarHorizontal.uld",
        "ui/uld/ActionBarVertical.uld",
        "ui/uld/ActionContents.uld",
        "ui/uld/ActionCross.uld",
        "ui/uld/ActionCrossEditor.uld",
        "ui/uld/ActionDetail.uld",
        "ui/uld/ActionDoubleCrossLeft.uld",
        "ui/uld/ActionDoubleCrossRight.uld",
        "ui/uld/ActionMenuActionSetting.uld",
        "ui/uld/ActionMenuReplaceList.uld",
        "ui/uld/AdventureNoteBook.uld",
        "ui/uld/AdventureNotice.uld",
        "ui/uld/AetherGauge.uld",
        "ui/uld/Alarm.uld",
        "ui/uld/AlarmSetting.uld",
        "ui/uld/Alliance.uld",
        "ui/uld/Alliance48.uld",
        "ui/uld/AllianceMemberList.uld",
        "ui/uld/AozActiveSetInputString.uld",
        "ui/uld/AozActiveSetList.uld",
        "ui/uld/AozBonusList.uld",
        "ui/uld/AozBriefing.uld",
        "ui/uld/AozContentsResult.uld",
        "ui/uld/AozNoteBook.uld",
        "ui/uld/AozNoteBookFilterSettings.uld",
        "ui/uld/AquariumFishList.uld",
        "ui/uld/AquariumSetting.uld",
        "ui/uld/ArchiveItem.uld",
        "ui/uld/AreaText.uld",
        "ui/uld/ArmouryBoard.uld",
        "ui/uld/ArmouryNoteBook.uld",
        "ui/uld/BagStatus.uld",
        "ui/uld/BalloonMessage.uld",
        "ui/uld/Bank.uld",
        "ui/uld/BannerConfirm.uld",
        "ui/uld/BannerContents.uld",
        "ui/uld/BannerContentsSetting.uld",
        "ui/uld/BannerEditor.uld",
        "ui/uld/BannerGearsetLink.uld",
        "ui/uld/BannerList.uld",
        "ui/uld/BannerMIP.uld",
        "ui/uld/BannerMIPSetting.uld",
        "ui/uld/BannerUpdateView.uld",
        "ui/uld/BasketBall.uld",
        "ui/uld/BattleTalk.uld",
        "ui/uld/BeastTribeSupplyList.uld",
        "ui/uld/BeginnerChannelInviteDialogue.uld",
        "ui/uld/BeginnerChannelJoinList.uld",
        "ui/uld/BeginnerChannelKick.uld",
        "ui/uld/BeginnersRoomCompletedTraining.uld",
        "ui/uld/BeginnersRoomMainWindow.uld",
        "ui/uld/BlackList.uld",
        "ui/uld/BlackListInputMemo.uld",
        "ui/uld/BotanistGame.uld",
        "ui/uld/BreedCoupling.uld",
        "ui/uld/BreedNameList.uld",
        "ui/uld/BreedNaming.uld",
        "ui/uld/BreedTraining.uld",
        "ui/uld/BreedTraining.uld",
        "ui/uld/Buddy.uld",
        "ui/uld/BuddyAction.uld",
        "ui/uld/BuddyAppearance.uld",
        "ui/uld/BuddyEquipList.uld",
        "ui/uld/BuddyInspect.uld",
        "ui/uld/BuddyInventory.uld",
        "ui/uld/BuddySkill.uld",
        "ui/uld/CabinetStore.uld",
        "ui/uld/CabinetWithdraw.uld",
        "ui/uld/CastBar.uld",
        "ui/uld/CharaMake_BgSelector.uld",
        "ui/uld/CharaMake_BirthDay.uld",
        "ui/uld/CharaMake_CharName.uld",
        "ui/uld/CharaMake_City.uld",
        "ui/uld/CharaMake_ClassSelector.uld",
        "ui/uld/CharaMake_DataExport.uld",
        "ui/uld/CharaMake_DataImport.uld",
        "ui/uld/CharaMake_DataImportDialog.uld",
        "ui/uld/CharaMake_DataInputString.uld",
        "ui/uld/CharaMake_Feature.uld",
        "ui/uld/CharaMake_Feature_Color_Eye.uld",
        "ui/uld/CharaMake_Feature_Color_FacePaint.uld",
        "ui/uld/CharaMake_Feature_Color_Hair.uld",
        "ui/uld/CharaMake_Feature_Color_L.uld",
        "ui/uld/CharaMake_Feature_Color_Lip.uld",
        "ui/uld/CharaMake_Feature_ListIcon_FacePaint.uld",
        "ui/uld/CharaMake_Feature_ListIcon_FaceType.uld",
        "ui/uld/CharaMake_Feature_ListIcon_Feature.uld",
        "ui/uld/CharaMake_Feature_ListIcon_Hair.uld",
        "ui/uld/CharaMake_Feature_ListIcon_Hair.uld",
        "ui/uld/CharaMake_Feature_ListIcon_Tail.uld",
        "ui/uld/CharaMake_Feature_ListIcon_Tatoo.uld",
        "ui/uld/CharaMake_Feature_Option12.uld",
        "ui/uld/CharaMake_Feature_Option2.uld",
        "ui/uld/CharaMake_Feature_Option3.uld",
        "ui/uld/CharaMake_Feature_Option4.uld",
        "ui/uld/CharaMake_Feature_Option5.uld",
        "ui/uld/CharaMake_Feature_Option6.uld",
        "ui/uld/CharaMake_Feature_Slider.uld",
        "ui/uld/CharaMake_Guardian.uld",
        "ui/uld/CharaMake_Help.uld",
        "ui/uld/CharaMake_Notice.uld",
        "ui/uld/CharaMake_Pose.uld",
        "ui/uld/CharaMake_Progress.uld",
        "ui/uld/CharaMake_ProgressSub.uld",
        "ui/uld/CharaMake_ProgressSub.uld",
        "ui/uld/CharaMake_Return.uld",
        "ui/uld/CharaMake_SelectYesNo.uld",
        "ui/uld/CharaMake_SelectYesNo.uld",
        "ui/uld/CharaMake_SelectYesNo.uld",
        "ui/uld/CharaMake_Shadow.uld",
        "ui/uld/CharaMake_Title.uld",
        "ui/uld/CharaMake_WorldServer.uld",
        "ui/uld/CharaSelect_DKT_Progress.uld",
        "ui/uld/CharaSelect_DKT_Select.uld",
        "ui/uld/CharaSelect_DKT_YesNo.uld",
        "ui/uld/CharaSelect_Detail.uld",
        "ui/uld/CharaSelect_Info.uld",
        "ui/uld/CharaSelect_Info.uld",
        "ui/uld/CharaSelect_Info_Progress.uld",
        "ui/uld/CharaSelect_Legacy.uld",
        "ui/uld/CharaSelect_ListMenu.uld",
        "ui/uld/CharaSelect_NameInput.uld",
        "ui/uld/CharaSelect_Remain.uld",
        "ui/uld/CharaSelect_Return.uld",
        "ui/uld/CharaSelect_Shadow.uld",
        "ui/uld/CharaSelect_Title.uld",
        "ui/uld/CharaSelect_WKT_Info.uld",
        "ui/uld/CharaSelect_WKT_Info.uld",
        "ui/uld/CharaSelect_WKT_Progress.uld",
        "ui/uld/CharaSelect_WKT_YesNo.uld",
        "ui/uld/CharaSelect_Warning.uld",
        "ui/uld/CharaSelect_WorldServer.uld",
        "ui/uld/Character.uld",
        "ui/uld/CharacterBestEquipment.uld",
        "ui/uld/CharacterCard.uld",
        "ui/uld/CharacterCardClassJobSetting.uld",
        "ui/uld/CharacterCardDesignSetting.uld",
        "ui/uld/CharacterCardPermissionSetting.uld",
        "ui/uld/CharacterCardPlayStyleSetting.uld",
        "ui/uld/CharacterCardProfileSetting.uld",
        "ui/uld/CharacterClass.uld",
        "ui/uld/CharacterGlassSelector.uld",
        "ui/uld/CharacterInspect.uld",
        "ui/uld/CharacterNote.uld",
        "ui/uld/CharacterProfile.uld",
        "ui/uld/CharacterRepute.uld",
        "ui/uld/CharacterSalvage.uld",
        "ui/uld/CharacterStatus.uld",
        "ui/uld/CharacterTitle.uld",
        "ui/uld/ChatConfig.uld",
        "ui/uld/ChatLogPanel.uld",
        "ui/uld/CheckBoxDialogue.uld",
        "ui/uld/CircleBlackList.uld",
        "ui/uld/CircleBoardEdit.uld",
        "ui/uld/CircleBook.uld",
        "ui/uld/CircleBookSetting.uld",
        "ui/uld/CircleFinder.uld",
        "ui/uld/CircleFinderHighlight.uld",
        "ui/uld/CircleFinderSearch.uld",
        "ui/uld/CircleFinderSetting.uld",
        "ui/uld/CircleInputString.uld",
        "ui/uld/CircleInvite.uld",
        "ui/uld/CircleList.uld",
        "ui/uld/CircleMemberGroupEdit.uld",
        "ui/uld/CircleQuestionnaire.uld",
        "ui/uld/ColorantColoringSelector.uld",
        "ui/uld/ColorantEquipmentSelector.uld",
        "ui/uld/CompanyCraftDelivery.uld",
        "ui/uld/CompanyCraftRecipe.uld",
        "ui/uld/Concentration.uld",
        "ui/uld/ConfigBackUpCharacterDownloadSelect.uld",
        "ui/uld/ConfigBackUpCharacterLoad.uld",
        "ui/uld/ConfigBackUpCharacterMainmenu.uld",
        "ui/uld/ConfigBackUpCharacterRestore.uld",
        "ui/uld/ConfigBackUpSystemLoad.uld",
        "ui/uld/ConfigBackUpSystemMainmenu.uld",
        "ui/uld/ConfigBackUpSystemRestore.uld",
        "ui/uld/ConfigCharacter.uld",
        "ui/uld/ConfigCharacterChatLogDetail.uld",
        "ui/uld/ConfigCharacterChatLogGeneral.uld",
        "ui/uld/ConfigCharacterChatLogNameSetting.uld",
        "ui/uld/ConfigCharacterChatLogRingTone.uld",
        "ui/uld/ConfigCharacterHotBarCommon.uld",
        "ui/uld/ConfigCharacterHotBarDisplay.uld",
        "ui/uld/ConfigCharacterHotBarXHB.uld",
        "ui/uld/ConfigCharacterHotBarXHBCustom.uld",
        "ui/uld/ConfigCharacterHudGeneral.uld",
        "ui/uld/ConfigCharacterHudHud.uld",
        "ui/uld/ConfigCharacterHudPartyList.uld",
        "ui/uld/ConfigCharacterHudPartyListRoleSortSetting.uld",
        "ui/uld/ConfigCharacterItem.uld",
        "ui/uld/ConfigCharacterNamePlateGeneral.uld",
        "ui/uld/ConfigCharacterNamePlateMyself.uld",
        "ui/uld/ConfigCharacterNamePlateNpc.uld",
        "ui/uld/ConfigCharacterNamePlateOthers.uld",
        "ui/uld/ConfigCharacterNamePlatePVP.uld",
        "ui/uld/ConfigCharacterOperationCharacter.uld",
        "ui/uld/ConfigCharacterOperationCircle.uld",
        "ui/uld/ConfigCharacterOperationGeneral.uld",
        "ui/uld/ConfigCharacterOperationMouse.uld",
        "ui/uld/ConfigCharacterOperationTarget.uld",
        "ui/uld/ConfigKeybind.uld",
        "ui/uld/ConfigLog.uld",
        "ui/uld/ConfigLogColor.uld",
        "ui/uld/ConfigLogFilter.uld",
        "ui/uld/ConfigLogFilterInputString.uld",
        "ui/uld/ConfigPadCalibration.uld",
        "ui/uld/ConfigPadCustomize.uld",
        "ui/uld/ConfigSystem.uld",
        "ui/uld/ContactList.uld",
        "ui/uld/ContentGauge.uld",
        "ui/uld/ContentMemberList.uld",
        "ui/uld/ContentsFinderConfirm.uld",
        "ui/uld/ContentsFinderMenu.uld",
        "ui/uld/ContentsFinderReady.uld",
        "ui/uld/ContentsFinderSetting.uld",
        "ui/uld/ContentsFinderSetting.uld",
        "ui/uld/ContentsFinderStatus.uld",
        "ui/uld/ContentsFinderStatus.uld",
        "ui/uld/ContentsFinderSupply.uld",
        "ui/uld/ContentsInfo.uld",
        "ui/uld/ContentsInfoDetail.uld",
        "ui/uld/ContentsNoteBook.uld",
        "ui/uld/ContentsReplayReadyCheck.uld",
        "ui/uld/ContentsReplayReadyCheckAlliance.uld",
        "ui/uld/ContentsReplaySetting.uld",
        "ui/uld/ContextIconMenu.uld",
        "ui/uld/ContextMenu.uld",
        "ui/uld/ContextMenuTitle.uld",
        "ui/uld/CountDownSettingDialog.uld",
        "ui/uld/CreditCast.uld",
        "ui/uld/CreditCast2.uld",
        "ui/uld/CreditEnd.uld",
        "ui/uld/CreditEnd.uld",
        "ui/uld/CreditEnd2.uld",
        "ui/uld/CrossWorldLinkShell.uld",
        "ui/uld/CruisingCreel.uld",
        "ui/uld/CruisingInfo.uld",
        "ui/uld/CruisingTimeTable.uld",
        "ui/uld/CurrencySetting.uld",
        "ui/uld/Cursor.uld",
        "ui/uld/CursorLocation.uld",
        "ui/uld/CursorRect.uld",
        "ui/uld/CutScene.uld",
        "ui/uld/DTR.uld",
        "ui/uld/DawnParty.uld",
        "ui/uld/DawnPartyHelp.uld",
        "ui/uld/DeepDungeonInformation.uld",
        "ui/uld/DeepDungeonInspect.uld",
        "ui/uld/DeepDungeonResult.uld",
        "ui/uld/DeepDungeonSaveData.uld",
        "ui/uld/DeepDungeonScoreList.uld",
        "ui/uld/DeepDungeonTopMenu.uld",
        "ui/uld/Description.uld",
        "ui/uld/DescriptionDD.uld",
        "ui/uld/Dialogue.uld",
        "ui/uld/Emj.uld",
        "ui/uld/EmjConfig.uld",
        "ui/uld/EmjRankPoint.uld",
        "ui/uld/EmjRankResult.uld",
        "ui/uld/Emote.uld",
        "ui/uld/EmoteStatusHelp.uld",
        "ui/uld/EurekaChainText.uld",
        "ui/uld/EurekaElementalEdit.uld",
        "ui/uld/EurekaElementalHud.uld",
        "ui/uld/EurekaLogosAetherList.uld",
        "ui/uld/EurekaLogosShardList.uld",
        "ui/uld/EurekaWeaponUpgrade.uld",
        "ui/uld/EurekaWeaponUpgradeDialogue.uld",
        "ui/uld/EventSkip.uld",
        "ui/uld/ExcellentTrade.uld",
        "ui/uld/Exp.uld",
        "ui/uld/ExplorationConfirm.uld",
        "ui/uld/ExplorationReport.uld",
        "ui/uld/ExplorationShip.uld",
        "ui/uld/ExplorationSubMarine.uld",
        "ui/uld/FGSCountDown.uld",
        "ui/uld/FGSEnterDialogue.uld",
        "ui/uld/FGSExitDialogue.uld",
        "ui/uld/FGSStageDescription.uld",
        "ui/uld/FGSStageIntroBanner.uld",
        "ui/uld/FashionCheck.uld",
        "ui/uld/FateProgress.uld",
        "ui/uld/FateReward.uld",
        "ui/uld/FgsHudGoal.uld",
        "ui/uld/FgsHudScore.uld",
        "ui/uld/FgsHudStatus.uld",
        "ui/uld/FgsHudWatchWindow.uld",
        "ui/uld/FgsRaceLog.uld",
        "ui/uld/FgsResult.uld",
        "ui/uld/FgsResultWinner.uld",
        "ui/uld/FgsScreenEliminated.uld",
        "ui/uld/FgsScreenQualified.uld",
        "ui/uld/FgsScreenRoundOver.uld",
        "ui/uld/FgsScreenWinner.uld",
        "ui/uld/FieldMarker.uld",
        "ui/uld/FishGuide.uld",
        "ui/uld/FishGuideFilterSetting.uld",
        "ui/uld/FishRecords.uld",
        "ui/uld/FishRelease.uld",
        "ui/uld/FishingBait.uld",
        "ui/uld/FishingHarpoon.uld",
        "ui/uld/FishingNoteBook.uld",
        "ui/uld/FittingRoom.uld",
        "ui/uld/FittingRoomGlassSelect.uld",
        "ui/uld/FittingRoomStore.uld",
        "ui/uld/FittingRoomStoreSetting.uld",
        "ui/uld/FlyingPermission.uld",
        "ui/uld/FocusTargetInfo.uld",
        "ui/uld/FreeCompany.uld",
        "ui/uld/FreeCompanyAction.uld",
        "ui/uld/FreeCompanyActivity.uld",
        "ui/uld/FreeCompanyApplication.uld",
        "ui/uld/FreeCompanyChest.uld",
        "ui/uld/FreeCompanyChestLog.uld",
        "ui/uld/FreeCompanyCrestColor.uld",
        "ui/uld/FreeCompanyCrestDecal.uld",
        "ui/uld/FreeCompanyCrestEditor.uld",
        "ui/uld/FreeCompanyCrestSymbolColor.uld",
        "ui/uld/FreeCompanyExchange.uld",
        "ui/uld/FreeCompanyExchange.uld",
        "ui/uld/FreeCompanyInputMessage.uld",
        "ui/uld/FreeCompanyInputString.uld",
        "ui/uld/FreeCompanyInputString.uld",
        "ui/uld/FreeCompanyInputString.uld",
        "ui/uld/FreeCompanyInputString.uld",
        "ui/uld/FreeCompanyInputString.uld",
        "ui/uld/FreeCompanyMember.uld",
        "ui/uld/FreeCompanyProfile.uld",
        "ui/uld/FreeCompanyProfileEdit.uld",
        "ui/uld/FreeCompanyRank.uld",
        "ui/uld/FreeCompanyRights.uld",
        "ui/uld/FreeCompanyStatus.uld",
        "ui/uld/FreeCompanyTopics.uld",
        "ui/uld/FreeShop.uld",
        "ui/uld/FriendGroupEdit.uld",
        "ui/uld/Gacha.uld",
        "ui/uld/GateResult.uld",
        "ui/uld/Gathering.uld",
        "ui/uld/GatheringCollectable.uld",
        "ui/uld/GatheringLocationEffect.uld",
        "ui/uld/GatheringNoteBook.uld",
        "ui/uld/GcArmyChangeClass.uld",
        "ui/uld/GcArmyChangeExpeditionTrait.uld",
        "ui/uld/GcArmyChangeMiragePrism.uld",
        "ui/uld/GcArmyChangeTactic.uld",
        "ui/uld/GcArmyMemberList.uld",
        "ui/uld/GcArmyMemberProfile.uld",
        "ui/uld/GcArmyMiragePrism.uld",
        "ui/uld/GcArmyMiragePrismDialogue.uld",
        "ui/uld/GcArmyOrder.uld",
        "ui/uld/GcArmyTrainingList.uld",
        "ui/uld/GearSetList.uld",
        "ui/uld/GearSetPreview.uld",
        "ui/uld/GearSetRegistered.uld",
        "ui/uld/GearSetView.uld",
        "ui/uld/GoldSaucer.uld",
        "ui/uld/GoldSaucerCardDeck.uld",
        "ui/uld/GoldSaucerCardDeckEdit.uld",
        "ui/uld/GoldSaucerCardFilter.uld",
        "ui/uld/GoldSaucerCardList.uld",
        "ui/uld/GoldSaucerEmj.uld",
        "ui/uld/GoldSaucerGeneral.uld",
        "ui/uld/GoldSaucerRaceAppearance.uld",
        "ui/uld/GoldSaucerRaceParameter.uld",
        "ui/uld/GoldSaucerRacePedigree.uld",
        "ui/uld/GoldSaucerReward.uld",
        "ui/uld/GoldSaucerVerminion.uld",
        "ui/uld/GrandCompany0.uld",
        "ui/uld/GrandCompanyRank.uld",
        "ui/uld/GrandCompanyRankUp.uld",
        "ui/uld/GrandCompanySupplyList.uld",
        "ui/uld/GrandCompanySupplyReward.uld",
        "ui/uld/GroupPoseCameraSetting.uld",
        "ui/uld/GroupPoseGuide.uld",
        "ui/uld/GroupPoseStampEditor.uld",
        "ui/uld/GroupPoseStampImage.uld",
        "ui/uld/GsScreenText.uld",
        "ui/uld/GuildLeve.uld",
        "ui/uld/GuildLeveDifficulty.uld",
        "ui/uld/HWDGathererInspection.uld",
        "ui/uld/HWDGathererInspectionTargetList.uld",
        "ui/uld/HWDInfoBoard.uld",
        "ui/uld/HWDMonument.uld",
        "ui/uld/HWDScoreList.uld",
        "ui/uld/HWDSupply.uld",
        "ui/uld/Hammer.uld",
        "ui/uld/HeavensturnDescription.uld",
        "ui/uld/HelpWindow.uld",
        "ui/uld/HotobarCopy.uld",
        "ui/uld/HousingAppealSetting.uld",
        "ui/uld/HousingBlackListAuthority.uld",
        "ui/uld/HousingBlackListEviction.uld",
        "ui/uld/HousingBlackListSetting.uld",
        "ui/uld/HousingChocoboList.uld",
        "ui/uld/HousingConfig.uld",
        "ui/uld/HousingConfigLight.uld",
        "ui/uld/HousingEditExterior.uld",
        "ui/uld/HousingEditInterior.uld",
        "ui/uld/HousingEditMessage.uld",
        "ui/uld/HousingFurnitureCatalog.uld",
        "ui/uld/HousingGardening.uld",
        "ui/uld/HousingGoods.uld",
        "ui/uld/HousingLayout.uld",
        "ui/uld/HousingMate.uld",
        "ui/uld/HousingPadGuide.uld",
        "ui/uld/HousingReleaseAuthority.uld",
        "ui/uld/HousingSelectBlock.uld",
        "ui/uld/HousingSelectDeed.uld",
        "ui/uld/HousingSelectRoom.uld",
        "ui/uld/HousingSignBoard.uld",
        "ui/uld/HousingTravellersNote.uld",
        "ui/uld/HousingWheelStand.uld",
        "ui/uld/HousingWithdrawStorage.uld",
        "ui/uld/HowTo.uld",
        "ui/uld/HowToList.uld",
        "ui/uld/HowToNotice.uld",
        "ui/uld/HudLayout.uld",
        "ui/uld/HudLayoutBg.uld",
        "ui/uld/HudLayoutCurrentBuffStatus.uld",
        "ui/uld/HudLayoutCurrentTarget.uld",
        "ui/uld/HudLayoutCurrentWindow.uld",
        "ui/uld/HudLayoutRect.uld",
        "ui/uld/HudLayoutSetCopy.uld",
        "ui/uld/HudLayoutSnapSetting.uld",
        "ui/uld/IdlingCameraSetting.uld",
        "ui/uld/Image.uld",
        "ui/uld/ImageActionLearnedAOZ.uld",
        "ui/uld/ImageGenericTitle.uld",
        "ui/uld/ImageLocationTitle.uld",
        "ui/uld/ImageLocationTitleShort.uld",
        "ui/uld/ImageSnipeTitle.uld",
        "ui/uld/ImageStreakBKC.uld",
        "ui/uld/ImageStreakSXT.uld",
        "ui/uld/ImageSystem.uld",
        "ui/uld/InclusionShop.uld",
        "ui/uld/InputMessage.uld",
        "ui/uld/InputNumeric.uld",
        "ui/uld/InputSearchComment.uld",
        "ui/uld/InputString.uld",
        "ui/uld/Invaluable.uld",
        "ui/uld/Inventory.uld",
        "ui/uld/InventoryEvent.uld",
        "ui/uld/InventoryExpansion.uld",
        "ui/uld/InventoryGrid.uld",
        "ui/uld/InventoryGrid.uld",
        "ui/uld/InventoryGridCrystal.uld",
        "ui/uld/InventoryLarge.uld",
        "ui/uld/ItemFinder.uld",
        "ui/uld/ItemInspection.uld",
        "ui/uld/ItemInspectionList.uld",
        "ui/uld/ItemSearch.uld",
        "ui/uld/ItemSearchFilter.uld",
        "ui/uld/ItemSearchHistory.uld",
        "ui/uld/ItemSearchResult.uld",
        "ui/uld/ItemSearchSetting.uld",
        "ui/uld/Jigsaw.uld",
        "ui/uld/JigsawEvent.uld",
        "ui/uld/JobHudAST0.uld",
        "ui/uld/JobHudBLM0.uld",
        "ui/uld/JobHudBLM1.uld",
        "ui/uld/JobHudBRD0.uld",
        "ui/uld/JobHudDNC0.uld",
        "ui/uld/JobHudDNC1.uld",
        "ui/uld/JobHudDRG0.uld",
        "ui/uld/JobHudDRK0.uld",
        "ui/uld/JobHudDRK1.uld",
        "ui/uld/JobHudGFF0.uld",
        "ui/uld/JobHudGFF1.uld",
        "ui/uld/JobHudGNB0.uld",
        "ui/uld/JobHudMCH0.uld",
        "ui/uld/JobHudMNK0.uld",
        "ui/uld/JobHudMNK1.uld",
        "ui/uld/JobHudNIN0.uld",
        "ui/uld/JobHudNIN1.uld",
        "ui/uld/JobHudNotice.uld",
        "ui/uld/JobHudPLD0.uld",
        "ui/uld/JobHudRDB0.uld",
        "ui/uld/JobHudRDB1.uld",
        "ui/uld/JobHudRDM0.uld",
        "ui/uld/JobHudRPM0.uld",
        "ui/uld/JobHudRPM1.uld",
        "ui/uld/JobHudRRP0.uld",
        "ui/uld/JobHudRRP1.uld",
        "ui/uld/JobHudSAM0.uld",
        "ui/uld/JobHudSAM1.uld",
        "ui/uld/JobHudSCH0.uld",
        "ui/uld/JobHudSCH1.uld",
        "ui/uld/JobHudSMN0.uld",
        "ui/uld/JobHudSMN1.uld",
        "ui/uld/JobHudWAR0.uld",
        "ui/uld/JobHudWHM0.uld",
        "ui/uld/Journal.uld",
        "ui/uld/LFG.uld",
        "ui/uld/LFGCondition.uld",
        "ui/uld/LFGDetail.uld",
        "ui/uld/LFGFilterSettings.uld",
        "ui/uld/LFGPrivate.uld",
        "ui/uld/LFGRecruiterNameSearch.uld",
        "ui/uld/LFGSearch.uld",
        "ui/uld/LFGSearchLogSetting.uld",
        "ui/uld/LFGSelectRole.uld",
        "ui/uld/LegacyItemStorage.uld",
        "ui/uld/LetterAddress.uld",
        "ui/uld/LetterEditor.uld",
        "ui/uld/LetterHistory.uld",
        "ui/uld/LetterList.uld",
        "ui/uld/LetterViewer.uld",
        "ui/uld/LevelDown.uld",
        "ui/uld/LevelUp.uld",
        "ui/uld/LevelUp2.uld",
        "ui/uld/LimitBreak.uld",
        "ui/uld/LinkShell.uld",
        "ui/uld/ListColorChooser.uld",
        "ui/uld/ListGrid.uld",
        "ui/uld/ListIcon.uld",
        "ui/uld/Loading.uld",
        "ui/uld/LotteryDaily.uld",
        "ui/uld/LotteryWeekly.uld",
        "ui/uld/LotteryWeeklyHistory.uld",
        "ui/uld/LotteryWeeklyRewardList.uld",
        "ui/uld/LovmActionDetail.uld",
        "ui/uld/LovmConfirm.uld",
        "ui/uld/LovmHeader.uld",
        "ui/uld/LovmHelp.uld",
        "ui/uld/LovmHudCutIn.uld",
        "ui/uld/LovmPalette.uld",
        "ui/uld/LovmPaletteEdit.uld",
        "ui/uld/LovmPartyList.uld",
        "ui/uld/LovmQueueList.uld",
        "ui/uld/LovmRanking.uld",
        "ui/uld/LovmReady.uld",
        "ui/uld/LovmResult.uld",
        "ui/uld/LovmStatus.uld",
        "ui/uld/LovmTargetInfo.uld",
        "ui/uld/LovmTypeInfo.uld",
        "ui/uld/LovmVersus.uld",
        "ui/uld/MJIAnimalBreeding.uld",
        "ui/uld/MJIAnimalBreedingAutomatic.uld",
        "ui/uld/MJIAnimalNameInputString.uld",
        "ui/uld/MJIBuilding.uld",
        "ui/uld/MJIBuildingMove.uld",
        "ui/uld/MJIBuildingProgress.uld",
        "ui/uld/MJICraftDemandResearch.uld",
        "ui/uld/MJICraftMaterialConfirmation.uld",
        "ui/uld/MJICraftSales.uld",
        "ui/uld/MJICraftScheduleMaintenance.uld",
        "ui/uld/MJICraftScheduleMaterialList.uld",
        "ui/uld/MJICraftSchedulePreset.uld",
        "ui/uld/MJICraftScheduleSetting.uld",
        "ui/uld/MJIDisposeShop.uld",
        "ui/uld/MJIDisposeShopShipping.uld",
        "ui/uld/MJIDisposeShopShippingBulk.uld",
        "ui/uld/MJIEntrance.uld",
        "ui/uld/MJIFarmAutomatic.uld",
        "ui/uld/MJIFarmManagement.uld",
        "ui/uld/MJIGatheringHouse.uld",
        "ui/uld/MJIGatheringHouseExplore.uld",
        "ui/uld/MJIHousingGoods.uld",
        "ui/uld/MJIHud.uld",
        "ui/uld/MJIMinionManagement.uld",
        "ui/uld/MJIMinionNoteBook.uld",
        "ui/uld/MJIMissionComplete.uld",
        "ui/uld/MJINekomimiRequest.uld",
        "ui/uld/MJIPadGuide.uld",
        "ui/uld/MJIRecipeNoteBook.uld",
        "ui/uld/MJISetting.uld",
        "ui/uld/MYCActionSelect.uld",
        "ui/uld/MYCCharacterNote.uld",
        "ui/uld/MYCDuelRequest.uld",
        "ui/uld/MYCDuelRequestDialogue.uld",
        "ui/uld/MYCInfo.uld",
        "ui/uld/MYCItemBag.uld",
        "ui/uld/MYCItemBox.uld",
        "ui/uld/MYCItemMySet.uld",
        "ui/uld/MYCRelicGrowth.uld",
        "ui/uld/Macro.uld",
        "ui/uld/MacroTextCommandList.uld",
        "ui/uld/MainCommand.uld",
        "ui/uld/MateriaAttach.uld",
        "ui/uld/MateriaAttachDialog.uld",
        "ui/uld/MateriaParameter.uld",
        "ui/uld/MateriaRetrieveDialog.uld",
        "ui/uld/Materialize.uld",
        "ui/uld/MaterializeDialog.uld",
        "ui/uld/MemberRankAssign.uld",
        "ui/uld/MemberRankEdit.uld",
        "ui/uld/MemberRankOrderEdit.uld",
        "ui/uld/MentorCertification.uld",
        "ui/uld/MentorCondition.uld",
        "ui/uld/MentorRenewDialogue.uld",
        "ui/uld/MerchantEquipSelect.uld",
        "ui/uld/MerchantSetting.uld",
        "ui/uld/MerchantShop.uld",
        "ui/uld/MinerBotanistAim.uld",
        "ui/uld/MinerGame.uld",
        "ui/uld/MiniDungeonAnswer.uld",
        "ui/uld/MiniDungeonResult.uld",
        "ui/uld/MinionNoteBook.uld",
        "ui/uld/MinionNotebookYKW.uld",
        "ui/uld/MiragePrism.uld",
        "ui/uld/MiragePrismBox.uld",
        "ui/uld/MiragePrismBoxFilterSetting.uld",
        "ui/uld/MiragePrismBoxItemDetail.uld",
        "ui/uld/MiragePrismCrystalize.uld",
        "ui/uld/MiragePrismExecute.uld",
        "ui/uld/MiragePrismExecute.uld",
        "ui/uld/MiragePrismPlate.uld",
        "ui/uld/MiragePrismPlateDialogue.uld",
        "ui/uld/MiragePrismRemove.uld",
        "ui/uld/Mobhunt.uld",
        "ui/uld/Mobhunt2.uld",
        "ui/uld/Mobhunt3.uld",
        "ui/uld/Mobhunt4.uld",
        "ui/uld/Mobhunt5.uld",
        "ui/uld/Mobhunt6.uld",
        "ui/uld/Money.uld",
        "ui/uld/MonsterNoteBook.uld",
        "ui/uld/MoogleCollection.uld",
        "ui/uld/MoogleCollectionRewardList.uld",
        "ui/uld/MountNoteBook.uld",
        "ui/uld/Mount_speed.uld",
        "ui/uld/MovieSubtitle_Opening_Credit.uld",
        "ui/uld/MultipleHelpWindow.uld",
        "ui/uld/MuteList.uld",
        "ui/uld/NeedGreed.uld",
        "ui/uld/NeedGreedTargeting.uld",
        "ui/uld/Negotiation.uld",
        "ui/uld/NgWordEdit.uld",
        "ui/uld/NgWordFilterList.uld",
        "ui/uld/NgWordFilterSetting.uld",
        "ui/uld/Notification.uld",
        "ui/uld/NotificationItem.uld",
        "ui/uld/OperationGuide.uld",
        "ui/uld/Orchestrion.uld",
        "ui/uld/Orchestrion.uld",
        "ui/uld/OrchestrionPlayList.uld",
        "ui/uld/OrchestrionPlayListEdit.uld",
        "ui/uld/OrchestrionPlayListMusicSelect.uld",
        "ui/uld/OrnamentNoteBook.uld",
        "ui/uld/PVPCalendar.uld",
        "ui/uld/PVPFrontlineGauge.uld",
        "ui/uld/PVPFrontlineResultDetail.uld",
        "ui/uld/PVPMKSCountDown.uld",
        "ui/uld/PVPMKSReward.uld",
        "ui/uld/PVPPresetView.uld",
        "ui/uld/PVPScreenInformationHotBar.uld",
        "ui/uld/PVPSimulationAlliance.uld",
        "ui/uld/PVPSimulationDisplay.uld",
        "ui/uld/PVPSimulationDisplay2.uld",
        "ui/uld/PVPSimulationHeader.uld",
        "ui/uld/PVPSimulationHeader2.uld",
        "ui/uld/PVPSimulationMachineSelect.uld",
        "ui/uld/PVPTeamOrganization.uld",
        "ui/uld/PVPWelcomeDialogue.uld",
        "ui/uld/PadMouseMode.uld",
        "ui/uld/PartyMemberList.uld",
        "ui/uld/PcSearchDetail.uld",
        "ui/uld/PcSearchSelectClass.uld",
        "ui/uld/PcSearchSelectLocation.uld",
        "ui/uld/Performance.uld",
        "ui/uld/PerformanceGamePadGuide.uld",
        "ui/uld/PerformanceKeybind.uld",
        "ui/uld/PerformanceMetronome.uld",
        "ui/uld/PerformanceMetronomeSetting.uld",
        "ui/uld/PerformancePlayGuideSetting.uld",
        "ui/uld/PerformanceReadyCheck.uld",
        "ui/uld/PerformanceReadyCheckReceive.uld",
        "ui/uld/PerformanceToneChange.uld",
        "ui/uld/PerformanceWide.uld",
        "ui/uld/PicturePreview.uld",
        "ui/uld/ProgressBar.uld",
        "ui/uld/PunchingMachine.uld",
        "ui/uld/Purify.uld",
        "ui/uld/PurifyAuto.uld",
        "ui/uld/PurifyDialog.uld",
        "ui/uld/PurifyResult.uld",
        "ui/uld/Puzzle.uld",
        "ui/uld/PvPCharacter.uld",
        "ui/uld/PvPDuelRequest.uld",
        "ui/uld/PvPFrontlineHeader.uld",
        "ui/uld/PvPMKSBattleLog.uld",
        "ui/uld/PvPMKSHeader.uld",
        "ui/uld/PvPMKSRankRatingFunction.uld",
        "ui/uld/PvPMKSResult.uld",
        "ui/uld/PvPSpectatorCameraList.uld",
        "ui/uld/PvPSpectatorList.uld",
        "ui/uld/PvPTeam.uld",
        "ui/uld/PvPTeamActivity.uld",
        "ui/uld/PvPTeamMember.uld",
        "ui/uld/PvPTeamResult.uld",
        "ui/uld/PvPTeamStatus.uld",
        "ui/uld/Pvp.uld",
        "ui/uld/PvpActionAdditional.uld",
        "ui/uld/PvpActionJob.uld",
        "ui/uld/PvpActionQuickChat.uld",
        "ui/uld/PvpActionTraits.uld",
        "ui/uld/PvpColosseum.uld",
        "ui/uld/PvpFrontLine.uld",
        "ui/uld/PvpSimulation.uld",
        "ui/uld/QteButton.uld",
        "ui/uld/QteButtonKeep.uld",
        "ui/uld/QteButtonMashing.uld",
        "ui/uld/QteScreenInfo.uld",
        "ui/uld/QteStreak.uld",
        "ui/uld/QuestRedoUIHUD.uld",
        "ui/uld/RaceChocoboConfirm.uld",
        "ui/uld/RaceChocoboItemBox.uld",
        "ui/uld/RaceChocoboParameter.uld",
        "ui/uld/RaceChocoboRanking.uld",
        "ui/uld/RaceChocoboReady.uld",
        "ui/uld/RaceChocoboResult.uld",
        "ui/uld/RaceChocoboStatus.uld",
        "ui/uld/RaidFinder.uld",
        "ui/uld/RatingViewer.uld",
        "ui/uld/ReadyCheck.uld",
        "ui/uld/RecipeMaterialList.uld",
        "ui/uld/RecipeNoteBook.uld",
        "ui/uld/RecipeNoteBookFilterSetting.uld",
        "ui/uld/RecipeNotebookInspectionList.uld",
        "ui/uld/RecipeProductList.uld",
        "ui/uld/RecommendList.uld",
        "ui/uld/Reconstruction.uld",
        "ui/uld/ReconstructionBuyBack.uld",
        "ui/uld/Relic2Glass.uld",
        "ui/uld/Relic2Growth.uld",
        "ui/uld/Relic2GrowthFragment.uld",
        "ui/uld/RelicGlass.uld",
        "ui/uld/RelicMagicite.uld",
        "ui/uld/RelicMandervilleConfirm.uld",
        "ui/uld/RelicMandervilleGrowth.uld",
        "ui/uld/RelicNoteBook.uld",
        "ui/uld/RelicSphereScroll.uld",
        "ui/uld/RelicSphereUpgrade.uld",
        "ui/uld/Repair.uld",
        "ui/uld/Request.uld",
        "ui/uld/ResidentDragTarget.uld",
        "ui/uld/RetainerCharacter.uld",
        "ui/uld/RetainerHistory.uld",
        "ui/uld/RetainerInventory.uld",
        "ui/uld/RetainerInventoryGrid.uld",
        "ui/uld/RetainerInventoryGridCrystal.uld",
        "ui/uld/RetainerInventoryLarge.uld",
        "ui/uld/RetainerList.uld",
        "ui/uld/RetainerSell.uld",
        "ui/uld/RetainerSellList.uld",
        "ui/uld/RetainerSort.uld",
        "ui/uld/RetainerTask.uld",
        "ui/uld/RetainerTaskDetail.uld",
        "ui/uld/RetainerTaskSupply.uld",
        "ui/uld/RetainerTransferList.uld",
        "ui/uld/RetainerTransferProgress.uld",
        "ui/uld/ReturnerDialogue.uld",
        "ui/uld/ReturnerDialogueDetail.uld",
        "ui/uld/RhythmActionResult.uld",
        "ui/uld/RhythmActionStatus.uld",
        "ui/uld/RideShooting.uld",
        "ui/uld/RideShootingBg.uld",
        "ui/uld/RideShootingResult.uld",
        "ui/uld/RoadStoneResult.uld",
        "ui/uld/Salvage.uld",
        "ui/uld/SalvageAuto.uld",
        "ui/uld/SalvageDialog.uld",
        "ui/uld/SalvageResult.uld",
        "ui/uld/SatisfactionSupply.uld",
        "ui/uld/SatisfactionSupplyChangeMiragePrism.uld",
        "ui/uld/SatisfactionSupplyMiragePrism.uld",
        "ui/uld/ScenarioTree.uld",
        "ui/uld/ScenarioTreeDetail.uld",
        "ui/uld/ScreenFrame.uld",
        "ui/uld/ScreenInfo_CountDown.uld",
        "ui/uld/ScreenInfo_RaceChocobo.uld",
        "ui/uld/ScreenInfo_RaceChocoboLogo.uld",
        "ui/uld/SelectCustomString.uld",
        "ui/uld/SelectIconString.uld",
        "ui/uld/SelectList.uld",
        "ui/uld/SelectOk.uld",
        "ui/uld/SelectOkTitle.uld",
        "ui/uld/SelectStringCutScene.uld",
        "ui/uld/SelectStringEventGimmick.uld",
        "ui/uld/ShopCard.uld",
        "ui/uld/ShopCardDialog.uld",
        "ui/uld/ShopExchangeCoin.uld",
        "ui/uld/ShopExchangeCurrency.uld",
        "ui/uld/ShopExchangeCurrencyDialog.uld",
        "ui/uld/ShopExchangeItem.uld",
        "ui/uld/SkyIslandResult.uld",
        "ui/uld/SkyIslandResult2.uld",
        "ui/uld/SkyIslandSetting.uld",
        "ui/uld/SkyIslandSpoilTrade.uld",
        "ui/uld/SnipeHud.uld",
        "ui/uld/SnipeHudBg.uld",
        "ui/uld/Social.uld",
        "ui/uld/SocialDetailA.uld",
        "ui/uld/SocialDetailB.uld",
        "ui/uld/SocialList.uld",
        "ui/uld/Status.uld",
        "ui/uld/StorageCheck.uld",
        "ui/uld/StorySupport.uld",
        "ui/uld/StorySupportMemberSelect.uld",
        "ui/uld/Streak.uld",
        "ui/uld/Streak.uld",
        "ui/uld/SubCommandSetting.uld",
        "ui/uld/SupplyCollectableItems.uld",
        "ui/uld/SupportDesk.uld",
        "ui/uld/SupportDeskEditor.uld",
        "ui/uld/SupportDeskList.uld",
        "ui/uld/SupportDeskViewer.uld",
        "ui/uld/Synthesis.uld",
        "ui/uld/SynthesisCondition.uld",
        "ui/uld/SynthesisSimple.uld",
        "ui/uld/SynthesisSimpleDialog.uld",
        "ui/uld/SynthesisSimulator.uld",
        "ui/uld/Talk.uld",
        "ui/uld/TalkAutoMessageSelector.uld",
        "ui/uld/TalkAutoMessageSetting.uld",
        "ui/uld/TalkSpeed.uld",
        "ui/uld/TalkSubtitle.uld",
        "ui/uld/TargetCursor.uld",
        "ui/uld/TargetCursorGrand.uld",
        "ui/uld/TargetInfo.uld",
        "ui/uld/TargetInfoBuffDebuff.uld",
        "ui/uld/TargetInfoCastBar.uld",
        "ui/uld/TargetInfoMainTarget.uld",
        "ui/uld/Teleport.uld",
        "ui/uld/TeleportHousingFriend.uld",
        "ui/uld/TeleportSetting.uld",
        "ui/uld/TeleportTown.uld",
        "ui/uld/Text1.uld",
        "ui/uld/Text2.uld",
        "ui/uld/TextAchievementUnlocked.uld",
        "ui/uld/TextChain.uld",
        "ui/uld/TextClassChange.uld",
        "ui/uld/TextContentsNoteBook.uld",
        "ui/uld/TextFishingNote.uld",
        "ui/uld/TextGimmickHint.uld",
        "ui/uld/TextHousingGardening.uld",
        "ui/uld/TextMonsterNote0.uld",
        "ui/uld/TextMonsterNote1.uld",
        "ui/uld/TextMonsterNote2.uld",
        "ui/uld/TextRelicAtma.uld",
        "ui/uld/Tips.uld",
        "ui/uld/Title_Connect.uld",
        "ui/uld/Title_DataCenter.uld",
        "ui/uld/Title_Menu.uld",
        "ui/uld/Title_MovieSelector.uld",
        "ui/uld/Title_Revision.uld",
        "ui/uld/Title_Rights.uld",
        "ui/uld/Title_Version.uld",
        "ui/uld/Title_Worldmap.uld",
        "ui/uld/Title_Worldmap.uld",
        "ui/uld/Title_WorldmapBg.uld",
        "ui/uld/Title_WorldmapBg.uld",
        "ui/uld/TofuObjectList.uld",
        "ui/uld/TofuPresetList.uld",
        "ui/uld/ToolTipS.uld",
        "ui/uld/TourismMenu.uld",
        "ui/uld/Trade.uld",
        "ui/uld/TradeMultiple.uld",
        "ui/uld/TreasureChallenge.uld",
        "ui/uld/TreasureMap.uld",
        "ui/uld/TripleTriad.uld",
        "ui/uld/TripleTriadApplication.uld",
        "ui/uld/TripleTriadDeckConfirmation.uld",
        "ui/uld/TripleTriadDeckSelect.uld",
        "ui/uld/TripleTriadPickUpDeckSelect.uld",
        "ui/uld/TripleTriadPlayerInfo.uld",
        "ui/uld/TripleTriadRanking.uld",
        "ui/uld/TripleTriadResult.uld",
        "ui/uld/TripleTriadRoundResult.uld",
        "ui/uld/TripleTriadRule.uld",
        "ui/uld/TripleTriadRuleAnnounce.uld",
        "ui/uld/TripleTriadRuleSetting.uld",
        "ui/uld/TripleTriadTournamentPlayer.uld",
        "ui/uld/TripleTriadTournamentReport.uld",
        "ui/uld/TripleTriadTournamentResult.uld",
        "ui/uld/TripleTriadTournamentReward.uld",
        "ui/uld/TripleTriadTournamentSchedule.uld",
        "ui/uld/TurnBreak.uld",
        "ui/uld/TurnBreakResult.uld",
        "ui/uld/TurnBreakTitle.uld",
        "ui/uld/TutorialContents.uld",
        "ui/uld/UserPolicyPerformance.uld",
        "ui/uld/VVDActionSelect.uld",
        "ui/uld/VVDFinder.uld",
        "ui/uld/VVDNoteBook.uld",
        "ui/uld/VaseSetting.uld",
        "ui/uld/VoteKick.uld",
        "ui/uld/VoteKickDialogue.uld",
        "ui/uld/VoteMvp.uld",
        "ui/uld/VoteTreasure.uld",
        "ui/uld/Warning.uld",
        "ui/uld/WeatherReport.uld",
        "ui/uld/WebGuidance.uld",
        "ui/uld/WebLauncher.uld",
        "ui/uld/WebLink.uld",
        "ui/uld/Wedding.uld",
        "ui/uld/WeddingNotification.uld",
        "ui/uld/WeeklyBingo.uld",
        "ui/uld/WeeklyBingoBonusInfo.uld",
        "ui/uld/WeeklyBingoResult.uld",
        "ui/uld/WeeklyPuzzle.uld",
        "ui/uld/WeeklyPuzzleResult.uld",
        "ui/uld/WorkshopSupply.uld",
        "ui/uld/WorkshopSupply2.uld",
        "ui/uld/WorkshopSupply3.uld",
        "ui/uld/WorldTranslate.uld",
        "ui/uld/WorldTranslateConfirm.uld",
        "ui/uld/WorldTranslateStatus.uld",
        "ui/uld/ex_hotbar_editor.uld",
        "ui/uld/parameter.uld"
    ];
    #pragma warning restore SA1401 // Fields should be private
}
