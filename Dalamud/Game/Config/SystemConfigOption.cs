namespace Dalamud.Game.Config;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

/// <summary>
/// Config options in the System section.
/// </summary>
public enum SystemConfigOption
{
    /// <summary>
    /// System option with the internal name GuidVersion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GuidVersion", ConfigType.UInt)]
    GuidVersion,

    /// <summary>
    /// System option with the internal name ConfigVersion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ConfigVersion", ConfigType.UInt)]
    ConfigVersion,

    /// <summary>
    /// System option with the internal name Language.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Language", ConfigType.UInt)]
    Language,

    /// <summary>
    /// System option with the internal name Region.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Region", ConfigType.UInt)]
    Region,

    /// <summary>
    /// System option with the internal name UPnP.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("UPnP", ConfigType.UInt)]
    UPnP,

    /// <summary>
    /// System option with the internal name Port.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Port", ConfigType.UInt)]
    Port,

    /// <summary>
    /// System option with the internal name LastLogin0.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LastLogin0", ConfigType.UInt)]
    LastLogin0,

    /// <summary>
    /// System option with the internal name LastLogin1.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LastLogin1", ConfigType.UInt)]
    LastLogin1,

    /// <summary>
    /// System option with the internal name WorldId.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("WorldId", ConfigType.UInt)]
    WorldId,

    /// <summary>
    /// System option with the internal name ServiceIndex.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ServiceIndex", ConfigType.UInt)]
    ServiceIndex,

    /// <summary>
    /// System option with the internal name DktSessionId.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("DktSessionId", ConfigType.String)]
    DktSessionId,

    /// <summary>
    /// System option with the internal name MainAdapter.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("MainAdapter", ConfigType.String)]
    MainAdapter,

    /// <summary>
    /// System option with the internal name ScreenLeft.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScreenLeft", ConfigType.UInt)]
    ScreenLeft,

    /// <summary>
    /// System option with the internal name ScreenTop.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScreenTop", ConfigType.UInt)]
    ScreenTop,

    /// <summary>
    /// System option with the internal name ScreenWidth.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScreenWidth", ConfigType.UInt)]
    ScreenWidth,

    /// <summary>
    /// System option with the internal name ScreenHeight.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScreenHeight", ConfigType.UInt)]
    ScreenHeight,

    /// <summary>
    /// System option with the internal name ScreenMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScreenMode", ConfigType.UInt)]
    ScreenMode,

    /// <summary>
    /// System option with the internal name FullScreenWidth.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FullScreenWidth", ConfigType.UInt)]
    FullScreenWidth,

    /// <summary>
    /// System option with the internal name FullScreenHeight.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FullScreenHeight", ConfigType.UInt)]
    FullScreenHeight,

    /// <summary>
    /// System option with the internal name Refreshrate.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Refreshrate", ConfigType.UInt)]
    Refreshrate,

    /// <summary>
    /// System option with the internal name Fps.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Fps", ConfigType.UInt)]
    Fps,

    /// <summary>
    /// System option with the internal name AntiAliasing.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AntiAliasing", ConfigType.UInt)]
    AntiAliasing,

    /// <summary>
    /// System option with the internal name FPSInActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FPSInActive", ConfigType.UInt)]
    FPSInActive,

    /// <summary>
    /// System option with the internal name ResoMouseDrag.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ResoMouseDrag", ConfigType.UInt)]
    ResoMouseDrag,

    /// <summary>
    /// System option with the internal name MouseOpeLimit.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseOpeLimit", ConfigType.UInt)]
    MouseOpeLimit,

    /// <summary>
    /// System option with the internal name LangSelectSub.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LangSelectSub", ConfigType.UInt)]
    LangSelectSub,

    /// <summary>
    /// System option with the internal name Gamma.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Gamma", ConfigType.UInt)]
    Gamma,

    /// <summary>
    /// System option with the internal name UiBaseScale.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("UiBaseScale", ConfigType.UInt)]
    UiBaseScale,

    /// <summary>
    /// System option with the internal name CharaLight.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CharaLight", ConfigType.UInt)]
    CharaLight,

    /// <summary>
    /// System option with the internal name UiHighScale.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("UiHighScale", ConfigType.UInt)]
    UiHighScale,

    /// <summary>
    /// System option with the internal name TextureFilterQuality.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TextureFilterQuality", ConfigType.UInt)]
    TextureFilterQuality,

    /// <summary>
    /// System option with the internal name TextureAnisotropicQuality.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TextureAnisotropicQuality", ConfigType.UInt)]
    TextureAnisotropicQuality,

    /// <summary>
    /// System option with the internal name SSAO.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SSAO", ConfigType.UInt)]
    SSAO,

    /// <summary>
    /// System option with the internal name Glare.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Glare", ConfigType.UInt)]
    Glare,

    /// <summary>
    /// System option with the internal name DistortionWater.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DistortionWater", ConfigType.UInt)]
    DistortionWater,

    /// <summary>
    /// System option with the internal name DepthOfField.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DepthOfField", ConfigType.UInt)]
    DepthOfField,

    /// <summary>
    /// System option with the internal name RadialBlur.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("RadialBlur", ConfigType.UInt)]
    RadialBlur,

    /// <summary>
    /// System option with the internal name Vignetting.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Vignetting", ConfigType.UInt)]
    Vignetting,

    /// <summary>
    /// System option with the internal name GrassQuality.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GrassQuality", ConfigType.UInt)]
    GrassQuality,

    /// <summary>
    /// System option with the internal name TranslucentQuality.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TranslucentQuality", ConfigType.UInt)]
    TranslucentQuality,

    /// <summary>
    /// System option with the internal name ShadowVisibilityType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityType", ConfigType.UInt)]
    ShadowVisibilityType,

    /// <summary>
    /// System option with the internal name ShadowSoftShadowType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowSoftShadowType", ConfigType.UInt)]
    ShadowSoftShadowType,

    /// <summary>
    /// System option with the internal name ShadowTextureSizeType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowTextureSizeType", ConfigType.UInt)]
    ShadowTextureSizeType,

    /// <summary>
    /// System option with the internal name ShadowCascadeCountType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowCascadeCountType", ConfigType.UInt)]
    ShadowCascadeCountType,

    /// <summary>
    /// System option with the internal name LodType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LodType", ConfigType.UInt)]
    LodType,

    /// <summary>
    /// System option with the internal name StreamingType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("StreamingType", ConfigType.UInt)]
    StreamingType,

    /// <summary>
    /// System option with the internal name GeneralQuality.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GeneralQuality", ConfigType.UInt)]
    GeneralQuality,

    /// <summary>
    /// System option with the internal name OcclusionCulling.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("OcclusionCulling", ConfigType.UInt)]
    OcclusionCulling,

    /// <summary>
    /// System option with the internal name ShadowLOD.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowLOD", ConfigType.UInt)]
    ShadowLOD,

    /// <summary>
    /// System option with the internal name PhysicsType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsType", ConfigType.UInt)]
    PhysicsType,

    /// <summary>
    /// System option with the internal name MapResolution.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapResolution", ConfigType.UInt)]
    MapResolution,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeSelf.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeSelf", ConfigType.UInt)]
    ShadowVisibilityTypeSelf,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeParty", ConfigType.UInt)]
    ShadowVisibilityTypeParty,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeOther.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeOther", ConfigType.UInt)]
    ShadowVisibilityTypeOther,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeEnemy", ConfigType.UInt)]
    ShadowVisibilityTypeEnemy,

    /// <summary>
    /// System option with the internal name PhysicsTypeSelf.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeSelf", ConfigType.UInt)]
    PhysicsTypeSelf,

    /// <summary>
    /// System option with the internal name PhysicsTypeParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeParty", ConfigType.UInt)]
    PhysicsTypeParty,

    /// <summary>
    /// System option with the internal name PhysicsTypeOther.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeOther", ConfigType.UInt)]
    PhysicsTypeOther,

    /// <summary>
    /// System option with the internal name PhysicsTypeEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeEnemy", ConfigType.UInt)]
    PhysicsTypeEnemy,

    /// <summary>
    /// System option with the internal name ReflectionType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ReflectionType", ConfigType.UInt)]
    ReflectionType,

    /// <summary>
    /// System option with the internal name ScreenShotImageType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScreenShotImageType", ConfigType.UInt)]
    ScreenShotImageType,

    /// <summary>
    /// System option with the internal name IsSoundDisable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundDisable", ConfigType.UInt)]
    IsSoundDisable,

    /// <summary>
    /// System option with the internal name IsSoundAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundAlways", ConfigType.UInt)]
    IsSoundAlways,

    /// <summary>
    /// System option with the internal name IsSoundBgmAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundBgmAlways", ConfigType.UInt)]
    IsSoundBgmAlways,

    /// <summary>
    /// System option with the internal name IsSoundSeAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundSeAlways", ConfigType.UInt)]
    IsSoundSeAlways,

    /// <summary>
    /// System option with the internal name IsSoundVoiceAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundVoiceAlways", ConfigType.UInt)]
    IsSoundVoiceAlways,

    /// <summary>
    /// System option with the internal name IsSoundSystemAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundSystemAlways", ConfigType.UInt)]
    IsSoundSystemAlways,

    /// <summary>
    /// System option with the internal name IsSoundEnvAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundEnvAlways", ConfigType.UInt)]
    IsSoundEnvAlways,

    /// <summary>
    /// System option with the internal name IsSoundPerformAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundPerformAlways", ConfigType.UInt)]
    IsSoundPerformAlways,

    /// <summary>
    /// System option with the internal name PadGuid.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadGuid", ConfigType.UInt)]
    PadGuid,

    /// <summary>
    /// System option with the internal name InstanceGuid.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("InstanceGuid", ConfigType.String)]
    InstanceGuid,

    /// <summary>
    /// System option with the internal name ProductGuid.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("ProductGuid", ConfigType.String)]
    ProductGuid,

    /// <summary>
    /// System option with the internal name DeadArea.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("DeadArea", ConfigType.Float)]
    DeadArea,

    /// <summary>
    /// System option with the internal name Alias.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("Alias", ConfigType.String)]
    Alias,

    /// <summary>
    /// System option with the internal name AlwaysInput.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AlwaysInput", ConfigType.UInt)]
    AlwaysInput,

    /// <summary>
    /// System option with the internal name ForceFeedBack.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ForceFeedBack", ConfigType.UInt)]
    ForceFeedBack,

    /// <summary>
    /// System option with the internal name PadPovInput.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadPovInput", ConfigType.UInt)]
    PadPovInput,

    /// <summary>
    /// System option with the internal name PadMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadMode", ConfigType.UInt)]
    PadMode,

    /// <summary>
    /// System option with the internal name PadAvailable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadAvailable", ConfigType.UInt)]
    PadAvailable,

    /// <summary>
    /// System option with the internal name PadReverseConfirmCancel.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadReverseConfirmCancel", ConfigType.UInt)]
    PadReverseConfirmCancel,

    /// <summary>
    /// System option with the internal name PadSelectButtonIcon.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadSelectButtonIcon", ConfigType.UInt)]
    PadSelectButtonIcon,

    /// <summary>
    /// System option with the internal name PadMouseMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadMouseMode", ConfigType.UInt)]
    PadMouseMode,

    /// <summary>
    /// System option with the internal name TextPasteEnable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TextPasteEnable", ConfigType.UInt)]
    TextPasteEnable,

    /// <summary>
    /// System option with the internal name EnablePsFunction.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("EnablePsFunction", ConfigType.UInt)]
    EnablePsFunction,

    /// <summary>
    /// System option with the internal name WaterWet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("WaterWet", ConfigType.UInt)]
    WaterWet,

    /// <summary>
    /// System option with the internal name DisplayObjectLimitType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DisplayObjectLimitType", ConfigType.UInt)]
    DisplayObjectLimitType,

    /// <summary>
    /// System option with the internal name WindowDispNum.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("WindowDispNum", ConfigType.UInt)]
    WindowDispNum,

    /// <summary>
    /// System option with the internal name ScreenShotDir.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("ScreenShotDir", ConfigType.String)]
    ScreenShotDir,

    /// <summary>
    /// System option with the internal name AntiAliasing_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AntiAliasing_DX11", ConfigType.UInt)]
    AntiAliasing_DX11,

    /// <summary>
    /// System option with the internal name TextureFilterQuality_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TextureFilterQuality_DX11", ConfigType.UInt)]
    TextureFilterQuality_DX11,

    /// <summary>
    /// System option with the internal name TextureAnisotropicQuality_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TextureAnisotropicQuality_DX11", ConfigType.UInt)]
    TextureAnisotropicQuality_DX11,

    /// <summary>
    /// System option with the internal name SSAO_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SSAO_DX11", ConfigType.UInt)]
    SSAO_DX11,

    /// <summary>
    /// System option with the internal name Glare_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Glare_DX11", ConfigType.UInt)]
    Glare_DX11,

    /// <summary>
    /// System option with the internal name DistortionWater_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DistortionWater_DX11", ConfigType.UInt)]
    DistortionWater_DX11,

    /// <summary>
    /// System option with the internal name DepthOfField_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DepthOfField_DX11", ConfigType.UInt)]
    DepthOfField_DX11,

    /// <summary>
    /// System option with the internal name RadialBlur_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("RadialBlur_DX11", ConfigType.UInt)]
    RadialBlur_DX11,

    /// <summary>
    /// System option with the internal name Vignetting_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Vignetting_DX11", ConfigType.UInt)]
    Vignetting_DX11,

    /// <summary>
    /// System option with the internal name GrassQuality_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GrassQuality_DX11", ConfigType.UInt)]
    GrassQuality_DX11,

    /// <summary>
    /// System option with the internal name TranslucentQuality_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TranslucentQuality_DX11", ConfigType.UInt)]
    TranslucentQuality_DX11,

    /// <summary>
    /// System option with the internal name ShadowSoftShadowType_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowSoftShadowType_DX11", ConfigType.UInt)]
    ShadowSoftShadowType_DX11,

    /// <summary>
    /// System option with the internal name ShadowTextureSizeType_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowTextureSizeType_DX11", ConfigType.UInt)]
    ShadowTextureSizeType_DX11,

    /// <summary>
    /// System option with the internal name ShadowCascadeCountType_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowCascadeCountType_DX11", ConfigType.UInt)]
    ShadowCascadeCountType_DX11,

    /// <summary>
    /// System option with the internal name LodType_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LodType_DX11", ConfigType.UInt)]
    LodType_DX11,

    /// <summary>
    /// System option with the internal name OcclusionCulling_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("OcclusionCulling_DX11", ConfigType.UInt)]
    OcclusionCulling_DX11,

    /// <summary>
    /// System option with the internal name ShadowLOD_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowLOD_DX11", ConfigType.UInt)]
    ShadowLOD_DX11,

    /// <summary>
    /// System option with the internal name MapResolution_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapResolution_DX11", ConfigType.UInt)]
    MapResolution_DX11,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeSelf_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeSelf_DX11", ConfigType.UInt)]
    ShadowVisibilityTypeSelf_DX11,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeParty_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeParty_DX11", ConfigType.UInt)]
    ShadowVisibilityTypeParty_DX11,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeOther_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeOther_DX11", ConfigType.UInt)]
    ShadowVisibilityTypeOther_DX11,

    /// <summary>
    /// System option with the internal name ShadowVisibilityTypeEnemy_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ShadowVisibilityTypeEnemy_DX11", ConfigType.UInt)]
    ShadowVisibilityTypeEnemy_DX11,

    /// <summary>
    /// System option with the internal name PhysicsTypeSelf_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeSelf_DX11", ConfigType.UInt)]
    PhysicsTypeSelf_DX11,

    /// <summary>
    /// System option with the internal name PhysicsTypeParty_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeParty_DX11", ConfigType.UInt)]
    PhysicsTypeParty_DX11,

    /// <summary>
    /// System option with the internal name PhysicsTypeOther_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeOther_DX11", ConfigType.UInt)]
    PhysicsTypeOther_DX11,

    /// <summary>
    /// System option with the internal name PhysicsTypeEnemy_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PhysicsTypeEnemy_DX11", ConfigType.UInt)]
    PhysicsTypeEnemy_DX11,

    /// <summary>
    /// System option with the internal name ReflectionType_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ReflectionType_DX11", ConfigType.UInt)]
    ReflectionType_DX11,

    /// <summary>
    /// System option with the internal name WaterWet_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("WaterWet_DX11", ConfigType.UInt)]
    WaterWet_DX11,

    /// <summary>
    /// System option with the internal name ParallaxOcclusion_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ParallaxOcclusion_DX11", ConfigType.UInt)]
    ParallaxOcclusion_DX11,

    /// <summary>
    /// System option with the internal name Tessellation_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Tessellation_DX11", ConfigType.UInt)]
    Tessellation_DX11,

    /// <summary>
    /// System option with the internal name GlareRepresentation_DX11.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GlareRepresentation_DX11", ConfigType.UInt)]
    GlareRepresentation_DX11,

    /// <summary>
    /// System option with the internal name UiSystemEnlarge.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("UiSystemEnlarge", ConfigType.UInt)]
    UiSystemEnlarge,

    /// <summary>
    /// System option with the internal name SoundPadSeType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundPadSeType", ConfigType.UInt)]
    SoundPadSeType,

    /// <summary>
    /// System option with the internal name SoundPad.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundPad", ConfigType.UInt)]
    SoundPad,

    /// <summary>
    /// System option with the internal name IsSoundPad.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSoundPad", ConfigType.UInt)]
    IsSoundPad,

    /// <summary>
    /// System option with the internal name TouchPadMouse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TouchPadMouse", ConfigType.UInt)]
    TouchPadMouse,

    /// <summary>
    /// System option with the internal name TouchPadCursorSpeed.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TouchPadCursorSpeed", ConfigType.UInt)]
    TouchPadCursorSpeed,

    /// <summary>
    /// System option with the internal name TouchPadButtonExtension.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TouchPadButtonExtension", ConfigType.UInt)]
    TouchPadButtonExtension,

    /// <summary>
    /// System option with the internal name TouchPadButton_Left.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TouchPadButton_Left", ConfigType.UInt)]
    TouchPadButton_Left,

    /// <summary>
    /// System option with the internal name TouchPadButton_Right.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TouchPadButton_Right", ConfigType.UInt)]
    TouchPadButton_Right,

    /// <summary>
    /// System option with the internal name RemotePlayRearTouchpadEnable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("RemotePlayRearTouchpadEnable", ConfigType.UInt)]
    RemotePlayRearTouchpadEnable,

    /// <summary>
    /// System option with the internal name SupportButtonAutorunEnable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SupportButtonAutorunEnable", ConfigType.UInt)]
    SupportButtonAutorunEnable,

    /// <summary>
    /// System option with the internal name R3ButtonWindowScalingEnable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("R3ButtonWindowScalingEnable", ConfigType.UInt)]
    R3ButtonWindowScalingEnable,

    /// <summary>
    /// System option with the internal name AutoAfkSwitchingTime.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoAfkSwitchingTime", ConfigType.UInt)]
    AutoAfkSwitchingTime,

    /// <summary>
    /// System option with the internal name AutoChangeCameraMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoChangeCameraMode", ConfigType.UInt)]
    AutoChangeCameraMode,

    /// <summary>
    /// System option with the internal name AccessibilitySoundVisualEnable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AccessibilitySoundVisualEnable", ConfigType.UInt)]
    AccessibilitySoundVisualEnable,

    /// <summary>
    /// System option with the internal name AccessibilitySoundVisualDispSize.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AccessibilitySoundVisualDispSize", ConfigType.UInt)]
    AccessibilitySoundVisualDispSize,

    /// <summary>
    /// System option with the internal name AccessibilitySoundVisualPermeabilityRate.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AccessibilitySoundVisualPermeabilityRate", ConfigType.UInt)]
    AccessibilitySoundVisualPermeabilityRate,

    /// <summary>
    /// System option with the internal name AccessibilityColorBlindFilterEnable.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AccessibilityColorBlindFilterEnable", ConfigType.UInt)]
    AccessibilityColorBlindFilterEnable,

    /// <summary>
    /// System option with the internal name AccessibilityColorBlindFilterType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AccessibilityColorBlindFilterType", ConfigType.UInt)]
    AccessibilityColorBlindFilterType,

    /// <summary>
    /// System option with the internal name AccessibilityColorBlindFilterStrength.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AccessibilityColorBlindFilterStrength", ConfigType.UInt)]
    AccessibilityColorBlindFilterStrength,

    /// <summary>
    /// System option with the internal name MouseAutoFocus.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseAutoFocus", ConfigType.UInt)]
    MouseAutoFocus,

    /// <summary>
    /// System option with the internal name FPSDownAFK.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FPSDownAFK", ConfigType.UInt)]
    FPSDownAFK,

    /// <summary>
    /// System option with the internal name IdlingCameraAFK.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IdlingCameraAFK", ConfigType.UInt)]
    IdlingCameraAFK,

    /// <summary>
    /// System option with the internal name MouseSpeed.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("MouseSpeed", ConfigType.Float)]
    MouseSpeed,

    /// <summary>
    /// System option with the internal name CameraZoom.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CameraZoom", ConfigType.UInt)]
    CameraZoom,

    /// <summary>
    /// System option with the internal name DynamicRezoType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DynamicRezoType", ConfigType.UInt)]
    DynamicRezoType,

    /// <summary>
    /// System option with the internal name Is3DAudio.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("Is3DAudio", ConfigType.UInt)]
    Is3DAudio,

    /// <summary>
    /// System option with the internal name BattleEffect.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("BattleEffect", ConfigType.UInt)]
    BattleEffect,

    /// <summary>
    /// System option with the internal name BGEffect.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("BGEffect", ConfigType.UInt)]
    BGEffect,

    /// <summary>
    /// System option with the internal name ColorThemeType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ColorThemeType", ConfigType.UInt)]
    ColorThemeType,

    /// <summary>
    /// System option with the internal name SystemMouseOperationSoftOn.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SystemMouseOperationSoftOn", ConfigType.UInt)]
    SystemMouseOperationSoftOn,

    /// <summary>
    /// System option with the internal name SystemMouseOperationTrajectory.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SystemMouseOperationTrajectory", ConfigType.UInt)]
    SystemMouseOperationTrajectory,

    /// <summary>
    /// System option with the internal name SystemMouseOperationCursorScaling.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SystemMouseOperationCursorScaling", ConfigType.UInt)]
    SystemMouseOperationCursorScaling,

    /// <summary>
    /// System option with the internal name HardwareCursorSize.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HardwareCursorSize", ConfigType.UInt)]
    HardwareCursorSize,

    /// <summary>
    /// System option with the internal name UiAssetType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("UiAssetType", ConfigType.UInt)]
    UiAssetType,

    /// <summary>
    /// System option with the internal name FellowshipShowNewNotice.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FellowshipShowNewNotice", ConfigType.UInt)]
    FellowshipShowNewNotice,

    /// <summary>
    /// System option with the internal name CutsceneMovieVoice.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CutsceneMovieVoice", ConfigType.UInt)]
    CutsceneMovieVoice,

    /// <summary>
    /// System option with the internal name CutsceneMovieCaption.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CutsceneMovieCaption", ConfigType.UInt)]
    CutsceneMovieCaption,

    /// <summary>
    /// System option with the internal name CutsceneMovieOpening.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CutsceneMovieOpening", ConfigType.UInt)]
    CutsceneMovieOpening,

    /// <summary>
    /// System option with the internal name SoundMaster.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundMaster", ConfigType.UInt)]
    SoundMaster,

    /// <summary>
    /// System option with the internal name SoundBgm.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundBgm", ConfigType.UInt)]
    SoundBgm,

    /// <summary>
    /// System option with the internal name SoundSe.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundSe", ConfigType.UInt)]
    SoundSe,

    /// <summary>
    /// System option with the internal name SoundVoice.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundVoice", ConfigType.UInt)]
    SoundVoice,

    /// <summary>
    /// System option with the internal name SoundEnv.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundEnv", ConfigType.UInt)]
    SoundEnv,

    /// <summary>
    /// System option with the internal name SoundSystem.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundSystem", ConfigType.UInt)]
    SoundSystem,

    /// <summary>
    /// System option with the internal name SoundPerform.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundPerform", ConfigType.UInt)]
    SoundPerform,

    /// <summary>
    /// System option with the internal name SoundPlayer.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundPlayer", ConfigType.UInt)]
    SoundPlayer,

    /// <summary>
    /// System option with the internal name SoundParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundParty", ConfigType.UInt)]
    SoundParty,

    /// <summary>
    /// System option with the internal name SoundOther.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundOther", ConfigType.UInt)]
    SoundOther,

    /// <summary>
    /// System option with the internal name IsSndMaster.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSndMaster", ConfigType.UInt)]
    IsSndMaster,

    /// <summary>
    /// System option with the internal name IsSndBgm.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSndBgm", ConfigType.UInt)]
    IsSndBgm,

    /// <summary>
    /// System option with the internal name IsSndSe.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSndSe", ConfigType.UInt)]
    IsSndSe,

    /// <summary>
    /// System option with the internal name IsSndVoice.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSndVoice", ConfigType.UInt)]
    IsSndVoice,

    /// <summary>
    /// System option with the internal name IsSndEnv.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSndEnv", ConfigType.UInt)]
    IsSndEnv,

    /// <summary>
    /// System option with the internal name IsSndSystem.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSndSystem", ConfigType.UInt)]
    IsSndSystem,

    /// <summary>
    /// System option with the internal name IsSndPerform.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("IsSndPerform", ConfigType.UInt)]
    IsSndPerform,

    /// <summary>
    /// System option with the internal name SoundDolby.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundDolby", ConfigType.UInt)]
    SoundDolby,

    /// <summary>
    /// System option with the internal name SoundMicpos.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundMicpos", ConfigType.UInt)]
    SoundMicpos,

    /// <summary>
    /// System option with the internal name SoundChocobo.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundChocobo", ConfigType.UInt)]
    SoundChocobo,

    /// <summary>
    /// System option with the internal name SoundFieldBattle.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundFieldBattle", ConfigType.UInt)]
    SoundFieldBattle,

    /// <summary>
    /// System option with the internal name SoundCfTimeCount.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundCfTimeCount", ConfigType.UInt)]
    SoundCfTimeCount,

    /// <summary>
    /// System option with the internal name SoundHousing.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundHousing", ConfigType.UInt)]
    SoundHousing,

    /// <summary>
    /// System option with the internal name SoundEqualizerType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SoundEqualizerType", ConfigType.UInt)]
    SoundEqualizerType,

    /// <summary>
    /// System option with the internal name PadButton_L2.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_L2", ConfigType.String)]
    PadButton_L2,

    /// <summary>
    /// System option with the internal name PadButton_R2.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_R2", ConfigType.String)]
    PadButton_R2,

    /// <summary>
    /// System option with the internal name PadButton_L1.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_L1", ConfigType.String)]
    PadButton_L1,

    /// <summary>
    /// System option with the internal name PadButton_R1.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_R1", ConfigType.String)]
    PadButton_R1,

    /// <summary>
    /// System option with the internal name PadButton_Triangle.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_Triangle", ConfigType.String)]
    PadButton_Triangle,

    /// <summary>
    /// System option with the internal name PadButton_Circle.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_Circle", ConfigType.String)]
    PadButton_Circle,

    /// <summary>
    /// System option with the internal name PadButton_Cross.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_Cross", ConfigType.String)]
    PadButton_Cross,

    /// <summary>
    /// System option with the internal name PadButton_Square.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_Square", ConfigType.String)]
    PadButton_Square,

    /// <summary>
    /// System option with the internal name PadButton_Select.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_Select", ConfigType.String)]
    PadButton_Select,

    /// <summary>
    /// System option with the internal name PadButton_Start.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_Start", ConfigType.String)]
    PadButton_Start,

    /// <summary>
    /// System option with the internal name PadButton_LS.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_LS", ConfigType.String)]
    PadButton_LS,

    /// <summary>
    /// System option with the internal name PadButton_RS.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_RS", ConfigType.String)]
    PadButton_RS,

    /// <summary>
    /// System option with the internal name PadButton_L3.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_L3", ConfigType.String)]
    PadButton_L3,

    /// <summary>
    /// System option with the internal name PadButton_R3.
    /// This option is a String.
    /// </summary>
    [GameConfigOption("PadButton_R3", ConfigType.String)]
    PadButton_R3,
}
