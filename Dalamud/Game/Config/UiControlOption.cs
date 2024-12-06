namespace Dalamud.Game.Config;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

/// <summary>
/// Config options in the UiControl section.
/// </summary>
public enum UiControlOption
{
    /// <summary>
    /// UiControl option with the internal name AutoChangePointOfView.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoChangePointOfView", ConfigType.UInt)]
    AutoChangePointOfView,

    /// <summary>
    /// UiControl option with the internal name KeyboardCameraInterpolationType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("KeyboardCameraInterpolationType", ConfigType.UInt)]
    KeyboardCameraInterpolationType,

    /// <summary>
    /// UiControl option with the internal name KeyboardCameraVerticalInterpolation.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("KeyboardCameraVerticalInterpolation", ConfigType.UInt)]
    KeyboardCameraVerticalInterpolation,

    /// <summary>
    /// UiControl option with the internal name TiltOffset.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("TiltOffset", ConfigType.Float)]
    TiltOffset,

    /// <summary>
    /// UiControl option with the internal name KeyboardSpeed.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("KeyboardSpeed", ConfigType.Float)]
    KeyboardSpeed,

    /// <summary>
    /// UiControl option with the internal name PadSpeed.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("PadSpeed", ConfigType.Float)]
    PadSpeed,

    /// <summary>
    /// UiControl option with the internal name PadFpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadFpsXReverse", ConfigType.UInt)]
    PadFpsXReverse,

    /// <summary>
    /// UiControl option with the internal name PadFpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadFpsYReverse", ConfigType.UInt)]
    PadFpsYReverse,

    /// <summary>
    /// UiControl option with the internal name PadTpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadTpsXReverse", ConfigType.UInt)]
    PadTpsXReverse,

    /// <summary>
    /// UiControl option with the internal name PadTpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadTpsYReverse", ConfigType.UInt)]
    PadTpsYReverse,

    /// <summary>
    /// UiControl option with the internal name MouseFpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseFpsXReverse", ConfigType.UInt)]
    MouseFpsXReverse,

    /// <summary>
    /// UiControl option with the internal name MouseFpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseFpsYReverse", ConfigType.UInt)]
    MouseFpsYReverse,

    /// <summary>
    /// UiControl option with the internal name MouseTpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseTpsXReverse", ConfigType.UInt)]
    MouseTpsXReverse,

    /// <summary>
    /// UiControl option with the internal name MouseTpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseTpsYReverse", ConfigType.UInt)]
    MouseTpsYReverse,

    /// <summary>
    /// UiControl option with the internal name MouseCharaViewRotYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewRotYReverse", ConfigType.UInt)]
    MouseCharaViewRotYReverse,

    /// <summary>
    /// UiControl option with the internal name MouseCharaViewRotXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewRotXReverse", ConfigType.UInt)]
    MouseCharaViewRotXReverse,

    /// <summary>
    /// UiControl option with the internal name MouseCharaViewMoveYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewMoveYReverse", ConfigType.UInt)]
    MouseCharaViewMoveYReverse,

    /// <summary>
    /// UiControl option with the internal name MouseCharaViewMoveXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewMoveXReverse", ConfigType.UInt)]
    MouseCharaViewMoveXReverse,

    /// <summary>
    /// UiControl option with the internal name PADCharaViewRotYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewRotYReverse", ConfigType.UInt)]
    PADCharaViewRotYReverse,

    /// <summary>
    /// UiControl option with the internal name PADCharaViewRotXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewRotXReverse", ConfigType.UInt)]
    PADCharaViewRotXReverse,

    /// <summary>
    /// UiControl option with the internal name PADCharaViewMoveYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewMoveYReverse", ConfigType.UInt)]
    PADCharaViewMoveYReverse,

    /// <summary>
    /// UiControl option with the internal name PADCharaViewMoveXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewMoveXReverse", ConfigType.UInt)]
    PADCharaViewMoveXReverse,

    /// <summary>
    /// UiControl option with the internal name FlyingControlType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FlyingControlType", ConfigType.UInt)]
    FlyingControlType,

    /// <summary>
    /// UiControl option with the internal name FlyingLegacyAutorun.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FlyingLegacyAutorun", ConfigType.UInt)]
    FlyingLegacyAutorun,

    /// <summary>
    /// UiControl option with the internal name AutoFaceTargetOnAction.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoFaceTargetOnAction", ConfigType.UInt)]
    AutoFaceTargetOnAction,

    /// <summary>
    /// UiControl option with the internal name SelfClick.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SelfClick", ConfigType.UInt)]
    SelfClick,

    /// <summary>
    /// UiControl option with the internal name NoTargetClickCancel.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("NoTargetClickCancel", ConfigType.UInt)]
    NoTargetClickCancel,

    /// <summary>
    /// UiControl option with the internal name AutoTarget.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoTarget", ConfigType.UInt)]
    AutoTarget,

    /// <summary>
    /// UiControl option with the internal name TargetTypeSelect.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetTypeSelect", ConfigType.UInt)]
    TargetTypeSelect,

    /// <summary>
    /// UiControl option with the internal name AutoLockOn.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoLockOn", ConfigType.UInt)]
    AutoLockOn,

    /// <summary>
    /// UiControl option with the internal name CircleBattleModeAutoChange.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBattleModeAutoChange", ConfigType.UInt)]
    CircleBattleModeAutoChange,

    /// <summary>
    /// UiControl option with the internal name CircleIsCustom.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleIsCustom", ConfigType.UInt)]
    CircleIsCustom,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnIsActive", ConfigType.UInt)]
    CircleSwordDrawnIsActive,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnNonPartyPc", ConfigType.UInt)]
    CircleSwordDrawnNonPartyPc,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnParty", ConfigType.UInt)]
    CircleSwordDrawnParty,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnEnemy", ConfigType.UInt)]
    CircleSwordDrawnEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnAggro", ConfigType.UInt)]
    CircleSwordDrawnAggro,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnNpcOrObject", ConfigType.UInt)]
    CircleSwordDrawnNpcOrObject,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnMinion", ConfigType.UInt)]
    CircleSwordDrawnMinion,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnDutyEnemy", ConfigType.UInt)]
    CircleSwordDrawnDutyEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnPet", ConfigType.UInt)]
    CircleSwordDrawnPet,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnAlliance", ConfigType.UInt)]
    CircleSwordDrawnAlliance,

    /// <summary>
    /// UiControl option with the internal name CircleSwordDrawnMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnMark", ConfigType.UInt)]
    CircleSwordDrawnMark,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedIsActive", ConfigType.UInt)]
    CircleSheathedIsActive,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedNonPartyPc", ConfigType.UInt)]
    CircleSheathedNonPartyPc,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedParty", ConfigType.UInt)]
    CircleSheathedParty,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedEnemy", ConfigType.UInt)]
    CircleSheathedEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedAggro", ConfigType.UInt)]
    CircleSheathedAggro,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedNpcOrObject", ConfigType.UInt)]
    CircleSheathedNpcOrObject,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedMinion", ConfigType.UInt)]
    CircleSheathedMinion,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedDutyEnemy", ConfigType.UInt)]
    CircleSheathedDutyEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedPet", ConfigType.UInt)]
    CircleSheathedPet,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedAlliance", ConfigType.UInt)]
    CircleSheathedAlliance,

    /// <summary>
    /// UiControl option with the internal name CircleSheathedMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedMark", ConfigType.UInt)]
    CircleSheathedMark,

    /// <summary>
    /// UiControl option with the internal name CircleClickIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickIsActive", ConfigType.UInt)]
    CircleClickIsActive,

    /// <summary>
    /// UiControl option with the internal name CircleClickNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickNonPartyPc", ConfigType.UInt)]
    CircleClickNonPartyPc,

    /// <summary>
    /// UiControl option with the internal name CircleClickParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickParty", ConfigType.UInt)]
    CircleClickParty,

    /// <summary>
    /// UiControl option with the internal name CircleClickEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickEnemy", ConfigType.UInt)]
    CircleClickEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleClickAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickAggro", ConfigType.UInt)]
    CircleClickAggro,

    /// <summary>
    /// UiControl option with the internal name CircleClickNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickNpcOrObject", ConfigType.UInt)]
    CircleClickNpcOrObject,

    /// <summary>
    /// UiControl option with the internal name CircleClickMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickMinion", ConfigType.UInt)]
    CircleClickMinion,

    /// <summary>
    /// UiControl option with the internal name CircleClickDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickDutyEnemy", ConfigType.UInt)]
    CircleClickDutyEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleClickPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickPet", ConfigType.UInt)]
    CircleClickPet,

    /// <summary>
    /// UiControl option with the internal name CircleClickAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickAlliance", ConfigType.UInt)]
    CircleClickAlliance,

    /// <summary>
    /// UiControl option with the internal name CircleClickMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickMark", ConfigType.UInt)]
    CircleClickMark,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonIsActive", ConfigType.UInt)]
    CircleXButtonIsActive,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonNonPartyPc", ConfigType.UInt)]
    CircleXButtonNonPartyPc,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonParty", ConfigType.UInt)]
    CircleXButtonParty,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonEnemy", ConfigType.UInt)]
    CircleXButtonEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonAggro", ConfigType.UInt)]
    CircleXButtonAggro,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonNpcOrObject", ConfigType.UInt)]
    CircleXButtonNpcOrObject,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonMinion", ConfigType.UInt)]
    CircleXButtonMinion,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonDutyEnemy", ConfigType.UInt)]
    CircleXButtonDutyEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonPet", ConfigType.UInt)]
    CircleXButtonPet,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonAlliance", ConfigType.UInt)]
    CircleXButtonAlliance,

    /// <summary>
    /// UiControl option with the internal name CircleXButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonMark", ConfigType.UInt)]
    CircleXButtonMark,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonIsActive", ConfigType.UInt)]
    CircleYButtonIsActive,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonNonPartyPc", ConfigType.UInt)]
    CircleYButtonNonPartyPc,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonParty", ConfigType.UInt)]
    CircleYButtonParty,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonEnemy", ConfigType.UInt)]
    CircleYButtonEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonAggro", ConfigType.UInt)]
    CircleYButtonAggro,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonNpcOrObject", ConfigType.UInt)]
    CircleYButtonNpcOrObject,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonMinion", ConfigType.UInt)]
    CircleYButtonMinion,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonDutyEnemy", ConfigType.UInt)]
    CircleYButtonDutyEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonPet", ConfigType.UInt)]
    CircleYButtonPet,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonAlliance", ConfigType.UInt)]
    CircleYButtonAlliance,

    /// <summary>
    /// UiControl option with the internal name CircleYButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonMark", ConfigType.UInt)]
    CircleYButtonMark,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonIsActive", ConfigType.UInt)]
    CircleBButtonIsActive,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonNonPartyPc", ConfigType.UInt)]
    CircleBButtonNonPartyPc,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonParty", ConfigType.UInt)]
    CircleBButtonParty,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonEnemy", ConfigType.UInt)]
    CircleBButtonEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonAggro", ConfigType.UInt)]
    CircleBButtonAggro,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonNpcOrObject", ConfigType.UInt)]
    CircleBButtonNpcOrObject,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonMinion", ConfigType.UInt)]
    CircleBButtonMinion,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonDutyEnemy", ConfigType.UInt)]
    CircleBButtonDutyEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonPet", ConfigType.UInt)]
    CircleBButtonPet,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonAlliance", ConfigType.UInt)]
    CircleBButtonAlliance,

    /// <summary>
    /// UiControl option with the internal name CircleBButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonMark", ConfigType.UInt)]
    CircleBButtonMark,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonIsActive", ConfigType.UInt)]
    CircleAButtonIsActive,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonNonPartyPc", ConfigType.UInt)]
    CircleAButtonNonPartyPc,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonParty", ConfigType.UInt)]
    CircleAButtonParty,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonEnemy", ConfigType.UInt)]
    CircleAButtonEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonAggro", ConfigType.UInt)]
    CircleAButtonAggro,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonNpcOrObject", ConfigType.UInt)]
    CircleAButtonNpcOrObject,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonMinion", ConfigType.UInt)]
    CircleAButtonMinion,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonDutyEnemy", ConfigType.UInt)]
    CircleAButtonDutyEnemy,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonPet", ConfigType.UInt)]
    CircleAButtonPet,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonAlliance", ConfigType.UInt)]
    CircleAButtonAlliance,

    /// <summary>
    /// UiControl option with the internal name CircleAButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonMark", ConfigType.UInt)]
    CircleAButtonMark,

    /// <summary>
    /// UiControl option with the internal name GroundTargetType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GroundTargetType", ConfigType.UInt)]
    GroundTargetType,

    /// <summary>
    /// UiControl option with the internal name GroundTargetCursorSpeed.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GroundTargetCursorSpeed", ConfigType.UInt)]
    GroundTargetCursorSpeed,

    /// <summary>
    /// UiControl option with the internal name TargetCircleType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetCircleType", ConfigType.UInt)]
    TargetCircleType,

    /// <summary>
    /// UiControl option with the internal name TargetLineType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetLineType", ConfigType.UInt)]
    TargetLineType,

    /// <summary>
    /// UiControl option with the internal name LinkLineType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LinkLineType", ConfigType.UInt)]
    LinkLineType,

    /// <summary>
    /// UiControl option with the internal name ObjectBorderingType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ObjectBorderingType", ConfigType.UInt)]
    ObjectBorderingType,

    /// <summary>
    /// UiControl option with the internal name MoveMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MoveMode", ConfigType.UInt)]
    MoveMode,

    /// <summary>
    /// UiControl option with the internal name HotbarDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDisp", ConfigType.UInt)]
    HotbarDisp,

    /// <summary>
    /// UiControl option with the internal name HotbarEmptyVisible.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarEmptyVisible", ConfigType.UInt)]
    HotbarEmptyVisible,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp01.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp01", ConfigType.UInt)]
    HotbarNoneSlotDisp01,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp02.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp02", ConfigType.UInt)]
    HotbarNoneSlotDisp02,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp03.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp03", ConfigType.UInt)]
    HotbarNoneSlotDisp03,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp04.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp04", ConfigType.UInt)]
    HotbarNoneSlotDisp04,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp05.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp05", ConfigType.UInt)]
    HotbarNoneSlotDisp05,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp06.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp06", ConfigType.UInt)]
    HotbarNoneSlotDisp06,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp07.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp07", ConfigType.UInt)]
    HotbarNoneSlotDisp07,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp08.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp08", ConfigType.UInt)]
    HotbarNoneSlotDisp08,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp09.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp09", ConfigType.UInt)]
    HotbarNoneSlotDisp09,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDisp10.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp10", ConfigType.UInt)]
    HotbarNoneSlotDisp10,

    /// <summary>
    /// UiControl option with the internal name HotbarNoneSlotDispEX.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDispEX", ConfigType.UInt)]
    HotbarNoneSlotDispEX,

    /// <summary>
    /// UiControl option with the internal name ExHotbarSetting.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ExHotbarSetting", ConfigType.UInt)]
    ExHotbarSetting,

    /// <summary>
    /// UiControl option with the internal name HotbarExHotbarUseSetting.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarExHotbarUseSetting", ConfigType.UInt)]
    HotbarExHotbarUseSetting,

    /// <summary>
    /// UiControl option with the internal name HotbarCrossUseEx.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossUseEx", ConfigType.UInt)]
    HotbarCrossUseEx,

    /// <summary>
    /// UiControl option with the internal name HotbarCrossUseExDirection.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossUseExDirection", ConfigType.UInt)]
    HotbarCrossUseExDirection,

    /// <summary>
    /// UiControl option with the internal name HotbarCrossDispType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossDispType", ConfigType.UInt)]
    HotbarCrossDispType,

    /// <summary>
    /// UiControl option with the internal name PartyListSoloOff.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PartyListSoloOff", ConfigType.UInt)]
    PartyListSoloOff,

    /// <summary>
    /// UiControl option with the internal name HowTo.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HowTo", ConfigType.UInt)]
    HowTo,

    /// <summary>
    /// UiControl option with the internal name HousingFurnitureBindConfirm.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HousingFurnitureBindConfirm", ConfigType.UInt)]
    HousingFurnitureBindConfirm,

    /// <summary>
    /// UiControl option with the internal name DirectChat.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DirectChat", ConfigType.UInt)]
    DirectChat,

    /// <summary>
    /// UiControl option with the internal name CharaParamDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CharaParamDisp", ConfigType.UInt)]
    CharaParamDisp,

    /// <summary>
    /// UiControl option with the internal name LimitBreakGaugeDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LimitBreakGaugeDisp", ConfigType.UInt)]
    LimitBreakGaugeDisp,

    /// <summary>
    /// UiControl option with the internal name ScenarioTreeDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScenarioTreeDisp", ConfigType.UInt)]
    ScenarioTreeDisp,

    /// <summary>
    /// UiControl option with the internal name ScenarioTreeCompleteDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScenarioTreeCompleteDisp", ConfigType.UInt)]
    ScenarioTreeCompleteDisp,

    /// <summary>
    /// UiControl option with the internal name HotbarCrossDispAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossDispAlways", ConfigType.UInt)]
    HotbarCrossDispAlways,

    /// <summary>
    /// UiControl option with the internal name ExpDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ExpDisp", ConfigType.UInt)]
    ExpDisp,

    /// <summary>
    /// UiControl option with the internal name InventryStatusDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InventryStatusDisp", ConfigType.UInt)]
    InventryStatusDisp,

    /// <summary>
    /// UiControl option with the internal name DutyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DutyListDisp", ConfigType.UInt)]
    DutyListDisp,

    /// <summary>
    /// UiControl option with the internal name NaviMapDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("NaviMapDisp", ConfigType.UInt)]
    NaviMapDisp,

    /// <summary>
    /// UiControl option with the internal name GilStatusDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GilStatusDisp", ConfigType.UInt)]
    GilStatusDisp,

    /// <summary>
    /// UiControl option with the internal name InfoSettingDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InfoSettingDisp", ConfigType.UInt)]
    InfoSettingDisp,

    /// <summary>
    /// UiControl option with the internal name InfoSettingDispType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InfoSettingDispType", ConfigType.UInt)]
    InfoSettingDispType,

    /// <summary>
    /// UiControl option with the internal name TargetInfoDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetInfoDisp", ConfigType.UInt)]
    TargetInfoDisp,

    /// <summary>
    /// UiControl option with the internal name EnemyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("EnemyListDisp", ConfigType.UInt)]
    EnemyListDisp,

    /// <summary>
    /// UiControl option with the internal name FocusTargetDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FocusTargetDisp", ConfigType.UInt)]
    FocusTargetDisp,

    /// <summary>
    /// UiControl option with the internal name ItemDetailDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ItemDetailDisp", ConfigType.UInt)]
    ItemDetailDisp,

    /// <summary>
    /// UiControl option with the internal name ActionDetailDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ActionDetailDisp", ConfigType.UInt)]
    ActionDetailDisp,

    /// <summary>
    /// UiControl option with the internal name DetailTrackingType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DetailTrackingType", ConfigType.UInt)]
    DetailTrackingType,

    /// <summary>
    /// UiControl option with the internal name ToolTipDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ToolTipDisp", ConfigType.UInt)]
    ToolTipDisp,

    /// <summary>
    /// UiControl option with the internal name MapPermeationRate.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapPermeationRate", ConfigType.UInt)]
    MapPermeationRate,

    /// <summary>
    /// UiControl option with the internal name MapOperationType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapOperationType", ConfigType.UInt)]
    MapOperationType,

    /// <summary>
    /// UiControl option with the internal name PartyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PartyListDisp", ConfigType.UInt)]
    PartyListDisp,

    /// <summary>
    /// UiControl option with the internal name PartyListNameType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PartyListNameType", ConfigType.UInt)]
    PartyListNameType,

    /// <summary>
    /// UiControl option with the internal name FlyTextDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FlyTextDisp", ConfigType.UInt)]
    FlyTextDisp,

    /// <summary>
    /// UiControl option with the internal name MapPermeationMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapPermeationMode", ConfigType.UInt)]
    MapPermeationMode,

    /// <summary>
    /// UiControl option with the internal name AllianceList1Disp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AllianceList1Disp", ConfigType.UInt)]
    AllianceList1Disp,

    /// <summary>
    /// UiControl option with the internal name AllianceList2Disp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AllianceList2Disp", ConfigType.UInt)]
    AllianceList2Disp,

    /// <summary>
    /// UiControl option with the internal name TargetInfoSelfBuff.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetInfoSelfBuff", ConfigType.UInt)]
    TargetInfoSelfBuff,

    /// <summary>
    /// UiControl option with the internal name PopUpTextDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PopUpTextDisp", ConfigType.UInt)]
    PopUpTextDisp,

    /// <summary>
    /// UiControl option with the internal name ContentsInfoDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ContentsInfoDisp", ConfigType.UInt)]
    ContentsInfoDisp,

    /// <summary>
    /// UiControl option with the internal name DutyListHideWhenCntInfoDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DutyListHideWhenCntInfoDisp", ConfigType.UInt)]
    DutyListHideWhenCntInfoDisp,

    /// <summary>
    /// UiControl option with the internal name DutyListNumDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DutyListNumDisp", ConfigType.UInt)]
    DutyListNumDisp,

    /// <summary>
    /// UiControl option with the internal name InInstanceContentDutyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InInstanceContentDutyListDisp", ConfigType.UInt)]
    InInstanceContentDutyListDisp,

    /// <summary>
    /// UiControl option with the internal name InPublicContentDutyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InPublicContentDutyListDisp", ConfigType.UInt)]
    InPublicContentDutyListDisp,

    /// <summary>
    /// UiControl option with the internal name ContentsInfoJoiningRequestDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ContentsInfoJoiningRequestDisp", ConfigType.UInt)]
    ContentsInfoJoiningRequestDisp,

    /// <summary>
    /// UiControl option with the internal name ContentsInfoJoiningRequestSituationDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ContentsInfoJoiningRequestSituationDisp", ConfigType.UInt)]
    ContentsInfoJoiningRequestSituationDisp,

    /// <summary>
    /// UiControl option with the internal name HotbarDispSetNum.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispSetNum", ConfigType.UInt)]
    HotbarDispSetNum,

    /// <summary>
    /// UiControl option with the internal name HotbarDispSetChangeType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispSetChangeType", ConfigType.UInt)]
    HotbarDispSetChangeType,

    /// <summary>
    /// UiControl option with the internal name HotbarDispSetDragType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispSetDragType", ConfigType.UInt)]
    HotbarDispSetDragType,

    /// <summary>
    /// UiControl option with the internal name MainCommandType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MainCommandType", ConfigType.UInt)]
    MainCommandType,

    /// <summary>
    /// UiControl option with the internal name MainCommandDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MainCommandDisp", ConfigType.UInt)]
    MainCommandDisp,

    /// <summary>
    /// UiControl option with the internal name MainCommandDragShortcut.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MainCommandDragShortcut", ConfigType.UInt)]
    MainCommandDragShortcut,

    /// <summary>
    /// UiControl option with the internal name HotbarDispLookNum.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispLookNum", ConfigType.UInt)]
    HotbarDispLookNum,
}
