using System.Security.AccessControl;
using Windows.Win32.Security;

namespace Dalamud.Broker.Commands;

internal partial class SetupCommand
{
    private record struct PolicyEntry
    {
        public string Path { get; init; }
        
        public FileSystemRights Access { get; init; }
        
        public AccessControlType Outcome { get; init; }
        
        public WELL_KNOWN_SID_TYPE? Integrity { get; init; }
    }
}
