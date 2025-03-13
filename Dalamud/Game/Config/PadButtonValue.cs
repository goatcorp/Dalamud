namespace Dalamud.Game.Config;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo

/// <summary>Valid values for PadButton options under <see cref="SystemConfigOption"/>.</summary>
/// <remarks>Names are the valid part. Enum values are exclusively for use with current Dalamud version.</remarks>
public enum PadButtonValue
{
    /// <summary>Auto-run.</summary>
    Autorun_Support,

    /// <summary>Change Hotbar Set.</summary>
    Hotbar_Set_Change,

    /// <summary>Highlight Left Hotbar.</summary>
    XHB_Left_Start,

    /// <summary>Highlight Right Hotbar.</summary>
    XHB_Right_Start,

    /// <summary>Not directly referenced by Gamepad button customization window.</summary>
    Cursor_Operation,

    /// <summary>Draw Weapon/Lock On.</summary>
    Lockon_and_Sword,

    /// <summary>Sit/Lock On.</summary>
    Lockon_and_Sit,

    /// <summary>Change Camera.</summary>
    Camera_Modechange,

    /// <summary>Reset Camera Position.</summary>
    Camera_Reset,

    /// <summary>Draw/Sheathe Weapon.</summary>
    Drawn_Sword,

    /// <summary>Lock On.</summary>
    Camera_Lockononly,

    /// <summary>Face Target.</summary>
    FaceTarget,

    /// <summary>Assist Target.</summary>
    AssistTarget,

    /// <summary>Face Camera.</summary>
    LookCamera,

    /// <summary>Execute Macro #98 (Exclusive).</summary>
    Macro98,

    /// <summary>Execute Macro #99 (Exclusive).</summary>
    Macro99,

    /// <summary>Not Assigned.</summary>
    Notset,

    /// <summary>Jump/Cancel Casting.</summary>
    Jump,

    /// <summary>Select Target/Confirm.</summary>
    Accept,

    /// <summary>Cancel.</summary>
    Cancel,

    /// <summary>Open Map/Subcommands.</summary>
    Map_Sub,

    /// <summary>Open Main Menu.</summary>
    MainCommand,

    /// <summary>Select HUD.</summary>
    HUD_Select,

    /// <summary>Move Character.</summary>
    Move_Operation,

    /// <summary>Move Camera.</summary>
    Camera_Operation,
}
