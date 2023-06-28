// ReSharper disable InconsistentNaming // Naming is suppressed so we can replace '_' with ' ' 
namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Enum representing a DataKind for the Data Window.
/// </summary>
internal enum DataKind
{
    /// <summary>
    /// Server Opcode Display.
    /// </summary>
    Server_OpCode,
    
    /// <summary>
    /// Address.
    /// </summary>
    Address,
    
    /// <summary>
    /// Object Table.
    /// </summary>
    Object_Table,
    
    /// <summary>
    /// Fate Table.
    /// </summary>
    Fate_Table,
        
    /// <summary>
    /// SE Font Test.
    /// </summary>
    SE_Font_Test,
        
    /// <summary>
    /// FontAwesome Test.
    /// </summary>
    FontAwesome_Test,
        
    /// <summary>
    /// Party List.
    /// </summary>
    Party_List,
        
    /// <summary>
    /// Buddy List.
    /// </summary>
    Buddy_List,
        
    /// <summary>
    /// Plugin IPC Test.
    /// </summary>
    Plugin_IPC,
        
    /// <summary>
    /// Player Condition.
    /// </summary>
    Condition,
        
    /// <summary>
    /// Gauge.
    /// </summary>
    Gauge,
        
    /// <summary>
    /// Command.
    /// </summary>
    Command,
        
    /// <summary>
    /// Addon.
    /// </summary>
    Addon,
        
    /// <summary>
    /// Addon Inspector.
    /// </summary>
    Addon_Inspector,
        
    /// <summary>
    /// AtkArrayData Browser.
    /// </summary>
    AtkArrayData_Browser,
        
    /// <summary>
    /// StartInfo.
    /// </summary>
    StartInfo,
        
    /// <summary>
    /// Target.
    /// </summary>
    Target,
        
    /// <summary>
    /// Toast.
    /// </summary>
    Toast,
        
    /// <summary>
    /// Fly Text.
    /// </summary>
    FlyText,
        
    /// <summary>
    /// ImGui.
    /// </summary>
    ImGui,
        
    /// <summary>
    /// Tex.
    /// </summary>
    Tex,
        
    /// <summary>
    /// KeyState.
    /// </summary>
    KeyState,
        
    /// <summary>
    /// GamePad.
    /// </summary>
    Gamepad,
        
    /// <summary>
    /// Configuration.
    /// </summary>
    Configuration,
        
    /// <summary>
    /// Task Scheduler.
    /// </summary>
    TaskSched,
        
    /// <summary>
    /// Hook.
    /// </summary>
    Hook,
        
    /// <summary>
    /// Aetherytes.
    /// </summary>
    Aetherytes,
        
    /// <summary>
    /// DTR Bar.
    /// </summary>
    Dtr_Bar,
        
    /// <summary>
    /// UIColor.
    /// </summary>
    UIColor,
        
    /// <summary>
    /// Data Share.
    /// </summary>
    Data_Share,

    /// <summary>
    /// Network Monitor.
    /// </summary>
    Network_Monitor,
}
