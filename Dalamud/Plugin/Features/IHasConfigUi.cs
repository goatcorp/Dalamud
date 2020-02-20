using System;
using Dalamud.Interface;

namespace Dalamud.Plugin.Features
{   
    /// <summary>
    /// This interface represents a Dalamud plugin that has a configuration UI which can be triggered.
    /// </summary>
    public interface IHasConfigUi : IHasUi  
    {
        /// <summary>
        /// An event handler that is fired when the plugin should show its configuration interface.
        /// </summary>
        EventHandler OpenConfigUi { get; }
    }
}
