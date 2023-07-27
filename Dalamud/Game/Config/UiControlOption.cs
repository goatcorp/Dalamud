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
    /// System option with the internal name AutoChangePointOfView.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoChangePointOfView", ConfigType.UInt)]
    AutoChangePointOfView,

    /// <summary>
    /// System option with the internal name KeyboardCameraInterpolationType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("KeyboardCameraInterpolationType", ConfigType.UInt)]
    KeyboardCameraInterpolationType,

    /// <summary>
    /// System option with the internal name KeyboardCameraVerticalInterpolation.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("KeyboardCameraVerticalInterpolation", ConfigType.UInt)]
    KeyboardCameraVerticalInterpolation,

    /// <summary>
    /// System option with the internal name TiltOffset.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("TiltOffset", ConfigType.Float)]
    TiltOffset,

    /// <summary>
    /// System option with the internal name KeyboardSpeed.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("KeyboardSpeed", ConfigType.Float)]
    KeyboardSpeed,

    /// <summary>
    /// System option with the internal name PadSpeed.
    /// This option is a Float.
    /// </summary>
    [GameConfigOption("PadSpeed", ConfigType.Float)]
    PadSpeed,

    /// <summary>
    /// System option with the internal name PadFpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadFpsXReverse", ConfigType.UInt)]
    PadFpsXReverse,

    /// <summary>
    /// System option with the internal name PadFpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadFpsYReverse", ConfigType.UInt)]
    PadFpsYReverse,

    /// <summary>
    /// System option with the internal name PadTpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadTpsXReverse", ConfigType.UInt)]
    PadTpsXReverse,

    /// <summary>
    /// System option with the internal name PadTpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PadTpsYReverse", ConfigType.UInt)]
    PadTpsYReverse,

    /// <summary>
    /// System option with the internal name MouseFpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseFpsXReverse", ConfigType.UInt)]
    MouseFpsXReverse,

    /// <summary>
    /// System option with the internal name MouseFpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseFpsYReverse", ConfigType.UInt)]
    MouseFpsYReverse,

    /// <summary>
    /// System option with the internal name MouseTpsXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseTpsXReverse", ConfigType.UInt)]
    MouseTpsXReverse,

    /// <summary>
    /// System option with the internal name MouseTpsYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseTpsYReverse", ConfigType.UInt)]
    MouseTpsYReverse,

    /// <summary>
    /// System option with the internal name MouseCharaViewRotYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewRotYReverse", ConfigType.UInt)]
    MouseCharaViewRotYReverse,

    /// <summary>
    /// System option with the internal name MouseCharaViewRotXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewRotXReverse", ConfigType.UInt)]
    MouseCharaViewRotXReverse,

    /// <summary>
    /// System option with the internal name MouseCharaViewMoveYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewMoveYReverse", ConfigType.UInt)]
    MouseCharaViewMoveYReverse,

    /// <summary>
    /// System option with the internal name MouseCharaViewMoveXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MouseCharaViewMoveXReverse", ConfigType.UInt)]
    MouseCharaViewMoveXReverse,

    /// <summary>
    /// System option with the internal name PADCharaViewRotYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewRotYReverse", ConfigType.UInt)]
    PADCharaViewRotYReverse,

    /// <summary>
    /// System option with the internal name PADCharaViewRotXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewRotXReverse", ConfigType.UInt)]
    PADCharaViewRotXReverse,

    /// <summary>
    /// System option with the internal name PADCharaViewMoveYReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewMoveYReverse", ConfigType.UInt)]
    PADCharaViewMoveYReverse,

    /// <summary>
    /// System option with the internal name PADCharaViewMoveXReverse.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PADCharaViewMoveXReverse", ConfigType.UInt)]
    PADCharaViewMoveXReverse,

    /// <summary>
    /// System option with the internal name FlyingControlType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FlyingControlType", ConfigType.UInt)]
    FlyingControlType,

    /// <summary>
    /// System option with the internal name FlyingLegacyAutorun.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FlyingLegacyAutorun", ConfigType.UInt)]
    FlyingLegacyAutorun,

    /// <summary>
    /// System option with the internal name AutoFaceTargetOnAction.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoFaceTargetOnAction", ConfigType.UInt)]
    AutoFaceTargetOnAction,

    /// <summary>
    /// System option with the internal name SelfClick.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("SelfClick", ConfigType.UInt)]
    SelfClick,

    /// <summary>
    /// System option with the internal name NoTargetClickCancel.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("NoTargetClickCancel", ConfigType.UInt)]
    NoTargetClickCancel,

    /// <summary>
    /// System option with the internal name AutoTarget.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoTarget", ConfigType.UInt)]
    AutoTarget,

    /// <summary>
    /// System option with the internal name TargetTypeSelect.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetTypeSelect", ConfigType.UInt)]
    TargetTypeSelect,

    /// <summary>
    /// System option with the internal name AutoLockOn.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AutoLockOn", ConfigType.UInt)]
    AutoLockOn,

    /// <summary>
    /// System option with the internal name CircleBattleModeAutoChange.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBattleModeAutoChange", ConfigType.UInt)]
    CircleBattleModeAutoChange,

    /// <summary>
    /// System option with the internal name CircleIsCustom.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleIsCustom", ConfigType.UInt)]
    CircleIsCustom,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnIsActive", ConfigType.UInt)]
    CircleSwordDrawnIsActive,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnNonPartyPc", ConfigType.UInt)]
    CircleSwordDrawnNonPartyPc,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnParty", ConfigType.UInt)]
    CircleSwordDrawnParty,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnEnemy", ConfigType.UInt)]
    CircleSwordDrawnEnemy,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnAggro", ConfigType.UInt)]
    CircleSwordDrawnAggro,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnNpcOrObject", ConfigType.UInt)]
    CircleSwordDrawnNpcOrObject,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnMinion", ConfigType.UInt)]
    CircleSwordDrawnMinion,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnDutyEnemy", ConfigType.UInt)]
    CircleSwordDrawnDutyEnemy,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnPet", ConfigType.UInt)]
    CircleSwordDrawnPet,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnAlliance", ConfigType.UInt)]
    CircleSwordDrawnAlliance,

    /// <summary>
    /// System option with the internal name CircleSwordDrawnMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSwordDrawnMark", ConfigType.UInt)]
    CircleSwordDrawnMark,

    /// <summary>
    /// System option with the internal name CircleSheathedIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedIsActive", ConfigType.UInt)]
    CircleSheathedIsActive,

    /// <summary>
    /// System option with the internal name CircleSheathedNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedNonPartyPc", ConfigType.UInt)]
    CircleSheathedNonPartyPc,

    /// <summary>
    /// System option with the internal name CircleSheathedParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedParty", ConfigType.UInt)]
    CircleSheathedParty,

    /// <summary>
    /// System option with the internal name CircleSheathedEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedEnemy", ConfigType.UInt)]
    CircleSheathedEnemy,

    /// <summary>
    /// System option with the internal name CircleSheathedAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedAggro", ConfigType.UInt)]
    CircleSheathedAggro,

    /// <summary>
    /// System option with the internal name CircleSheathedNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedNpcOrObject", ConfigType.UInt)]
    CircleSheathedNpcOrObject,

    /// <summary>
    /// System option with the internal name CircleSheathedMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedMinion", ConfigType.UInt)]
    CircleSheathedMinion,

    /// <summary>
    /// System option with the internal name CircleSheathedDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedDutyEnemy", ConfigType.UInt)]
    CircleSheathedDutyEnemy,

    /// <summary>
    /// System option with the internal name CircleSheathedPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedPet", ConfigType.UInt)]
    CircleSheathedPet,

    /// <summary>
    /// System option with the internal name CircleSheathedAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedAlliance", ConfigType.UInt)]
    CircleSheathedAlliance,

    /// <summary>
    /// System option with the internal name CircleSheathedMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleSheathedMark", ConfigType.UInt)]
    CircleSheathedMark,

    /// <summary>
    /// System option with the internal name CircleClickIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickIsActive", ConfigType.UInt)]
    CircleClickIsActive,

    /// <summary>
    /// System option with the internal name CircleClickNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickNonPartyPc", ConfigType.UInt)]
    CircleClickNonPartyPc,

    /// <summary>
    /// System option with the internal name CircleClickParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickParty", ConfigType.UInt)]
    CircleClickParty,

    /// <summary>
    /// System option with the internal name CircleClickEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickEnemy", ConfigType.UInt)]
    CircleClickEnemy,

    /// <summary>
    /// System option with the internal name CircleClickAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickAggro", ConfigType.UInt)]
    CircleClickAggro,

    /// <summary>
    /// System option with the internal name CircleClickNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickNpcOrObject", ConfigType.UInt)]
    CircleClickNpcOrObject,

    /// <summary>
    /// System option with the internal name CircleClickMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickMinion", ConfigType.UInt)]
    CircleClickMinion,

    /// <summary>
    /// System option with the internal name CircleClickDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickDutyEnemy", ConfigType.UInt)]
    CircleClickDutyEnemy,

    /// <summary>
    /// System option with the internal name CircleClickPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickPet", ConfigType.UInt)]
    CircleClickPet,

    /// <summary>
    /// System option with the internal name CircleClickAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickAlliance", ConfigType.UInt)]
    CircleClickAlliance,

    /// <summary>
    /// System option with the internal name CircleClickMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleClickMark", ConfigType.UInt)]
    CircleClickMark,

    /// <summary>
    /// System option with the internal name CircleXButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonIsActive", ConfigType.UInt)]
    CircleXButtonIsActive,

    /// <summary>
    /// System option with the internal name CircleXButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonNonPartyPc", ConfigType.UInt)]
    CircleXButtonNonPartyPc,

    /// <summary>
    /// System option with the internal name CircleXButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonParty", ConfigType.UInt)]
    CircleXButtonParty,

    /// <summary>
    /// System option with the internal name CircleXButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonEnemy", ConfigType.UInt)]
    CircleXButtonEnemy,

    /// <summary>
    /// System option with the internal name CircleXButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonAggro", ConfigType.UInt)]
    CircleXButtonAggro,

    /// <summary>
    /// System option with the internal name CircleXButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonNpcOrObject", ConfigType.UInt)]
    CircleXButtonNpcOrObject,

    /// <summary>
    /// System option with the internal name CircleXButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonMinion", ConfigType.UInt)]
    CircleXButtonMinion,

    /// <summary>
    /// System option with the internal name CircleXButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonDutyEnemy", ConfigType.UInt)]
    CircleXButtonDutyEnemy,

    /// <summary>
    /// System option with the internal name CircleXButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonPet", ConfigType.UInt)]
    CircleXButtonPet,

    /// <summary>
    /// System option with the internal name CircleXButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonAlliance", ConfigType.UInt)]
    CircleXButtonAlliance,

    /// <summary>
    /// System option with the internal name CircleXButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleXButtonMark", ConfigType.UInt)]
    CircleXButtonMark,

    /// <summary>
    /// System option with the internal name CircleYButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonIsActive", ConfigType.UInt)]
    CircleYButtonIsActive,

    /// <summary>
    /// System option with the internal name CircleYButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonNonPartyPc", ConfigType.UInt)]
    CircleYButtonNonPartyPc,

    /// <summary>
    /// System option with the internal name CircleYButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonParty", ConfigType.UInt)]
    CircleYButtonParty,

    /// <summary>
    /// System option with the internal name CircleYButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonEnemy", ConfigType.UInt)]
    CircleYButtonEnemy,

    /// <summary>
    /// System option with the internal name CircleYButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonAggro", ConfigType.UInt)]
    CircleYButtonAggro,

    /// <summary>
    /// System option with the internal name CircleYButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonNpcOrObject", ConfigType.UInt)]
    CircleYButtonNpcOrObject,

    /// <summary>
    /// System option with the internal name CircleYButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonMinion", ConfigType.UInt)]
    CircleYButtonMinion,

    /// <summary>
    /// System option with the internal name CircleYButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonDutyEnemy", ConfigType.UInt)]
    CircleYButtonDutyEnemy,

    /// <summary>
    /// System option with the internal name CircleYButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonPet", ConfigType.UInt)]
    CircleYButtonPet,

    /// <summary>
    /// System option with the internal name CircleYButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonAlliance", ConfigType.UInt)]
    CircleYButtonAlliance,

    /// <summary>
    /// System option with the internal name CircleYButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleYButtonMark", ConfigType.UInt)]
    CircleYButtonMark,

    /// <summary>
    /// System option with the internal name CircleBButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonIsActive", ConfigType.UInt)]
    CircleBButtonIsActive,

    /// <summary>
    /// System option with the internal name CircleBButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonNonPartyPc", ConfigType.UInt)]
    CircleBButtonNonPartyPc,

    /// <summary>
    /// System option with the internal name CircleBButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonParty", ConfigType.UInt)]
    CircleBButtonParty,

    /// <summary>
    /// System option with the internal name CircleBButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonEnemy", ConfigType.UInt)]
    CircleBButtonEnemy,

    /// <summary>
    /// System option with the internal name CircleBButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonAggro", ConfigType.UInt)]
    CircleBButtonAggro,

    /// <summary>
    /// System option with the internal name CircleBButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonNpcOrObject", ConfigType.UInt)]
    CircleBButtonNpcOrObject,

    /// <summary>
    /// System option with the internal name CircleBButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonMinion", ConfigType.UInt)]
    CircleBButtonMinion,

    /// <summary>
    /// System option with the internal name CircleBButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonDutyEnemy", ConfigType.UInt)]
    CircleBButtonDutyEnemy,

    /// <summary>
    /// System option with the internal name CircleBButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonPet", ConfigType.UInt)]
    CircleBButtonPet,

    /// <summary>
    /// System option with the internal name CircleBButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonAlliance", ConfigType.UInt)]
    CircleBButtonAlliance,

    /// <summary>
    /// System option with the internal name CircleBButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleBButtonMark", ConfigType.UInt)]
    CircleBButtonMark,

    /// <summary>
    /// System option with the internal name CircleAButtonIsActive.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonIsActive", ConfigType.UInt)]
    CircleAButtonIsActive,

    /// <summary>
    /// System option with the internal name CircleAButtonNonPartyPc.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonNonPartyPc", ConfigType.UInt)]
    CircleAButtonNonPartyPc,

    /// <summary>
    /// System option with the internal name CircleAButtonParty.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonParty", ConfigType.UInt)]
    CircleAButtonParty,

    /// <summary>
    /// System option with the internal name CircleAButtonEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonEnemy", ConfigType.UInt)]
    CircleAButtonEnemy,

    /// <summary>
    /// System option with the internal name CircleAButtonAggro.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonAggro", ConfigType.UInt)]
    CircleAButtonAggro,

    /// <summary>
    /// System option with the internal name CircleAButtonNpcOrObject.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonNpcOrObject", ConfigType.UInt)]
    CircleAButtonNpcOrObject,

    /// <summary>
    /// System option with the internal name CircleAButtonMinion.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonMinion", ConfigType.UInt)]
    CircleAButtonMinion,

    /// <summary>
    /// System option with the internal name CircleAButtonDutyEnemy.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonDutyEnemy", ConfigType.UInt)]
    CircleAButtonDutyEnemy,

    /// <summary>
    /// System option with the internal name CircleAButtonPet.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonPet", ConfigType.UInt)]
    CircleAButtonPet,

    /// <summary>
    /// System option with the internal name CircleAButtonAlliance.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonAlliance", ConfigType.UInt)]
    CircleAButtonAlliance,

    /// <summary>
    /// System option with the internal name CircleAButtonMark.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CircleAButtonMark", ConfigType.UInt)]
    CircleAButtonMark,

    /// <summary>
    /// System option with the internal name GroundTargetType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GroundTargetType", ConfigType.UInt)]
    GroundTargetType,

    /// <summary>
    /// System option with the internal name GroundTargetCursorSpeed.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GroundTargetCursorSpeed", ConfigType.UInt)]
    GroundTargetCursorSpeed,

    /// <summary>
    /// System option with the internal name TargetCircleType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetCircleType", ConfigType.UInt)]
    TargetCircleType,

    /// <summary>
    /// System option with the internal name TargetLineType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetLineType", ConfigType.UInt)]
    TargetLineType,

    /// <summary>
    /// System option with the internal name LinkLineType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LinkLineType", ConfigType.UInt)]
    LinkLineType,

    /// <summary>
    /// System option with the internal name ObjectBorderingType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ObjectBorderingType", ConfigType.UInt)]
    ObjectBorderingType,

    /// <summary>
    /// System option with the internal name MoveMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MoveMode", ConfigType.UInt)]
    MoveMode,

    /// <summary>
    /// System option with the internal name HotbarDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDisp", ConfigType.UInt)]
    HotbarDisp,

    /// <summary>
    /// System option with the internal name HotbarEmptyVisible.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarEmptyVisible", ConfigType.UInt)]
    HotbarEmptyVisible,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp01.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp01", ConfigType.UInt)]
    HotbarNoneSlotDisp01,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp02.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp02", ConfigType.UInt)]
    HotbarNoneSlotDisp02,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp03.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp03", ConfigType.UInt)]
    HotbarNoneSlotDisp03,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp04.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp04", ConfigType.UInt)]
    HotbarNoneSlotDisp04,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp05.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp05", ConfigType.UInt)]
    HotbarNoneSlotDisp05,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp06.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp06", ConfigType.UInt)]
    HotbarNoneSlotDisp06,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp07.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp07", ConfigType.UInt)]
    HotbarNoneSlotDisp07,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp08.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp08", ConfigType.UInt)]
    HotbarNoneSlotDisp08,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp09.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp09", ConfigType.UInt)]
    HotbarNoneSlotDisp09,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDisp10.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDisp10", ConfigType.UInt)]
    HotbarNoneSlotDisp10,

    /// <summary>
    /// System option with the internal name HotbarNoneSlotDispEX.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarNoneSlotDispEX", ConfigType.UInt)]
    HotbarNoneSlotDispEX,

    /// <summary>
    /// System option with the internal name ExHotbarSetting.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ExHotbarSetting", ConfigType.UInt)]
    ExHotbarSetting,

    /// <summary>
    /// System option with the internal name HotbarExHotbarUseSetting.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarExHotbarUseSetting", ConfigType.UInt)]
    HotbarExHotbarUseSetting,

    /// <summary>
    /// System option with the internal name HotbarCrossUseEx.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossUseEx", ConfigType.UInt)]
    HotbarCrossUseEx,

    /// <summary>
    /// System option with the internal name HotbarCrossUseExDirection.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossUseExDirection", ConfigType.UInt)]
    HotbarCrossUseExDirection,

    /// <summary>
    /// System option with the internal name HotbarCrossDispType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossDispType", ConfigType.UInt)]
    HotbarCrossDispType,

    /// <summary>
    /// System option with the internal name PartyListSoloOff.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PartyListSoloOff", ConfigType.UInt)]
    PartyListSoloOff,

    /// <summary>
    /// System option with the internal name HowTo.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HowTo", ConfigType.UInt)]
    HowTo,

    /// <summary>
    /// System option with the internal name HousingFurnitureBindConfirm.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HousingFurnitureBindConfirm", ConfigType.UInt)]
    HousingFurnitureBindConfirm,

    /// <summary>
    /// System option with the internal name DirectChat.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DirectChat", ConfigType.UInt)]
    DirectChat,

    /// <summary>
    /// System option with the internal name CharaParamDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("CharaParamDisp", ConfigType.UInt)]
    CharaParamDisp,

    /// <summary>
    /// System option with the internal name LimitBreakGaugeDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("LimitBreakGaugeDisp", ConfigType.UInt)]
    LimitBreakGaugeDisp,

    /// <summary>
    /// System option with the internal name ScenarioTreeDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScenarioTreeDisp", ConfigType.UInt)]
    ScenarioTreeDisp,

    /// <summary>
    /// System option with the internal name ScenarioTreeCompleteDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ScenarioTreeCompleteDisp", ConfigType.UInt)]
    ScenarioTreeCompleteDisp,

    /// <summary>
    /// System option with the internal name HotbarCrossDispAlways.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarCrossDispAlways", ConfigType.UInt)]
    HotbarCrossDispAlways,

    /// <summary>
    /// System option with the internal name ExpDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ExpDisp", ConfigType.UInt)]
    ExpDisp,

    /// <summary>
    /// System option with the internal name InventryStatusDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InventryStatusDisp", ConfigType.UInt)]
    InventryStatusDisp,

    /// <summary>
    /// System option with the internal name DutyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DutyListDisp", ConfigType.UInt)]
    DutyListDisp,

    /// <summary>
    /// System option with the internal name NaviMapDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("NaviMapDisp", ConfigType.UInt)]
    NaviMapDisp,

    /// <summary>
    /// System option with the internal name GilStatusDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("GilStatusDisp", ConfigType.UInt)]
    GilStatusDisp,

    /// <summary>
    /// System option with the internal name InfoSettingDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InfoSettingDisp", ConfigType.UInt)]
    InfoSettingDisp,

    /// <summary>
    /// System option with the internal name InfoSettingDispType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InfoSettingDispType", ConfigType.UInt)]
    InfoSettingDispType,

    /// <summary>
    /// System option with the internal name TargetInfoDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetInfoDisp", ConfigType.UInt)]
    TargetInfoDisp,

    /// <summary>
    /// System option with the internal name EnemyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("EnemyListDisp", ConfigType.UInt)]
    EnemyListDisp,

    /// <summary>
    /// System option with the internal name FocusTargetDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FocusTargetDisp", ConfigType.UInt)]
    FocusTargetDisp,

    /// <summary>
    /// System option with the internal name ItemDetailDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ItemDetailDisp", ConfigType.UInt)]
    ItemDetailDisp,

    /// <summary>
    /// System option with the internal name ActionDetailDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ActionDetailDisp", ConfigType.UInt)]
    ActionDetailDisp,

    /// <summary>
    /// System option with the internal name DetailTrackingType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DetailTrackingType", ConfigType.UInt)]
    DetailTrackingType,

    /// <summary>
    /// System option with the internal name ToolTipDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ToolTipDisp", ConfigType.UInt)]
    ToolTipDisp,

    /// <summary>
    /// System option with the internal name MapPermeationRate.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapPermeationRate", ConfigType.UInt)]
    MapPermeationRate,

    /// <summary>
    /// System option with the internal name MapOperationType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapOperationType", ConfigType.UInt)]
    MapOperationType,

    /// <summary>
    /// System option with the internal name PartyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PartyListDisp", ConfigType.UInt)]
    PartyListDisp,

    /// <summary>
    /// System option with the internal name PartyListNameType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PartyListNameType", ConfigType.UInt)]
    PartyListNameType,

    /// <summary>
    /// System option with the internal name FlyTextDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("FlyTextDisp", ConfigType.UInt)]
    FlyTextDisp,

    /// <summary>
    /// System option with the internal name MapPermeationMode.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MapPermeationMode", ConfigType.UInt)]
    MapPermeationMode,

    /// <summary>
    /// System option with the internal name AllianceList1Disp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AllianceList1Disp", ConfigType.UInt)]
    AllianceList1Disp,

    /// <summary>
    /// System option with the internal name AllianceList2Disp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("AllianceList2Disp", ConfigType.UInt)]
    AllianceList2Disp,

    /// <summary>
    /// System option with the internal name TargetInfoSelfBuff.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("TargetInfoSelfBuff", ConfigType.UInt)]
    TargetInfoSelfBuff,

    /// <summary>
    /// System option with the internal name PopUpTextDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("PopUpTextDisp", ConfigType.UInt)]
    PopUpTextDisp,

    /// <summary>
    /// System option with the internal name ContentsInfoDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ContentsInfoDisp", ConfigType.UInt)]
    ContentsInfoDisp,

    /// <summary>
    /// System option with the internal name DutyListHideWhenCntInfoDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DutyListHideWhenCntInfoDisp", ConfigType.UInt)]
    DutyListHideWhenCntInfoDisp,

    /// <summary>
    /// System option with the internal name DutyListNumDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("DutyListNumDisp", ConfigType.UInt)]
    DutyListNumDisp,

    /// <summary>
    /// System option with the internal name InInstanceContentDutyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InInstanceContentDutyListDisp", ConfigType.UInt)]
    InInstanceContentDutyListDisp,

    /// <summary>
    /// System option with the internal name InPublicContentDutyListDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("InPublicContentDutyListDisp", ConfigType.UInt)]
    InPublicContentDutyListDisp,

    /// <summary>
    /// System option with the internal name ContentsInfoJoiningRequestDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ContentsInfoJoiningRequestDisp", ConfigType.UInt)]
    ContentsInfoJoiningRequestDisp,

    /// <summary>
    /// System option with the internal name ContentsInfoJoiningRequestSituationDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("ContentsInfoJoiningRequestSituationDisp", ConfigType.UInt)]
    ContentsInfoJoiningRequestSituationDisp,

    /// <summary>
    /// System option with the internal name HotbarDispSetNum.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispSetNum", ConfigType.UInt)]
    HotbarDispSetNum,

    /// <summary>
    /// System option with the internal name HotbarDispSetChangeType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispSetChangeType", ConfigType.UInt)]
    HotbarDispSetChangeType,

    /// <summary>
    /// System option with the internal name HotbarDispSetDragType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispSetDragType", ConfigType.UInt)]
    HotbarDispSetDragType,

    /// <summary>
    /// System option with the internal name MainCommandType.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MainCommandType", ConfigType.UInt)]
    MainCommandType,

    /// <summary>
    /// System option with the internal name MainCommandDisp.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MainCommandDisp", ConfigType.UInt)]
    MainCommandDisp,

    /// <summary>
    /// System option with the internal name MainCommandDragShortcut.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("MainCommandDragShortcut", ConfigType.UInt)]
    MainCommandDragShortcut,

    /// <summary>
    /// System option with the internal name HotbarDispLookNum.
    /// This option is a UInt.
    /// </summary>
    [GameConfigOption("HotbarDispLookNum", ConfigType.UInt)]
    HotbarDispLookNum,
}
