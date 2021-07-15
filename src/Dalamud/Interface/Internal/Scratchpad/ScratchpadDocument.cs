using System;

namespace Dalamud.Interface.Internal.Scratchpad
{
    /// <summary>
    /// This class represents a single document in the ScratchPad.
    /// </summary>
    internal class ScratchpadDocument
    {
        /// <summary>
        /// Gets or sets the guid ID of the document.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the document content.
        /// </summary>
        public string Content { get; set; } = "INITIALIZE:\n\tPluginLog.Information(\"Loaded!\");\nEND;\n\nDISPOSE:\n\tPluginLog.Information(\"Disposed!\");\nEND;\n";

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string Title { get; set; } = "New Document";

        /// <summary>
        /// Gets or sets a value indicating whether the document has unsaved content.
        /// </summary>
        public bool HasUnsaved { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the document is open.
        /// </summary>
        public bool IsOpen { get; set; }

        /// <summary>
        /// Gets or sets the load status of the document.
        /// </summary>
        public ScratchLoadStatus Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this document is a macro.
        /// </summary>
        public bool IsMacro { get; set; } = true;
    }
}
