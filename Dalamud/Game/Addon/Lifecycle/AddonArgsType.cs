namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// Enumeration for available AddonLifecycle arg data.
/// </summary>
public enum AddonArgsType
{
    /// <summary>
    /// Contains argument data for Setup.
    /// </summary>
    Setup,
    
    /// <summary>
    /// Contains argument data for Update.
    /// </summary>
    Update,
      
    /// <summary>
    /// Contains argument data for Draw.
    /// </summary>  
    Draw,
     
    /// <summary>
    /// Contains argument data for Finalize.
    /// </summary>   
    Finalize,
     
    /// <summary>
    /// Contains argument data for RequestedUpdate.
    /// </summary>   
    RequestedUpdate,
     
    /// <summary>
    /// Contains argument data for Refresh.
    /// </summary>   
    Refresh,
    
    /// <summary>
    /// Contains argument data for ReceiveEvent.
    /// </summary>
    ReceiveEvent,
}
