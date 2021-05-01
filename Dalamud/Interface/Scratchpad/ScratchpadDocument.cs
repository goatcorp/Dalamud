using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Interface.Scratchpad
{
    class ScratchpadDocument
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Content = "INITIALIZE:\n\tPluginLog.Information(\"Loaded!\");\nEND;\n\nDISPOSE:\n\tPluginLog.Information(\"Disposed!\");\nEND;\n";

        public string Title { get; set; } = "New Document";

        public bool HasUnsaved { get; set; }

        public bool IsOpen { get; set; }

        public ScratchLoadStatus Status { get; set; }

        public bool IsMacro = true;
    }
}
