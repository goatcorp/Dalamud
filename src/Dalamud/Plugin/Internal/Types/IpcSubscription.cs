using System;
using System.Dynamic;

namespace Dalamud.Plugin.Internal.Types
{
    /// <summary>
    /// This class represents an IPC subscription between two plugins.
    /// </summary>
    internal record IpcSubscription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IpcSubscription"/> class.
        /// </summary>
        /// <param name="sourcePluginName">The source plugin name.</param>
        /// <param name="subPluginName">The name of the plugin being subscribed to.</param>
        /// <param name="subAction">The subscription action.</param>
        public IpcSubscription(string sourcePluginName, string subPluginName, Action<ExpandoObject> subAction)
        {
            this.SourcePluginName = sourcePluginName;
            this.SubPluginName = subPluginName;
            this.SubAction = subAction;
        }

        /// <summary>
        /// Gets the name of the plugin requesting the subscription.
        /// </summary>
        public string SourcePluginName { get; }

        /// <summary>
        /// Gets the name of the plugin being subscribed to.
        /// </summary>
        public string SubPluginName { get; }

        /// <summary>
        /// Gets the subscription action.
        /// </summary>
        public Action<ExpandoObject> SubAction { get; }
    }
}
