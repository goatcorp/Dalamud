using System.Security.Principal;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace Dalamud.Broker.Win32;

internal sealed class ProcessLaunchContext
{
    public required string ApplicationPath { get; set; }
    
    public IEnumerable<string>? Arguments { get; set; }
    
    public PROCESS_CREATION_FLAGS CreationFlags { get; set; }
    
    public string? WorkingDirectory { get; set; }
    
    public AppContainer? AppContainer { get; set; }
    
    public (SecurityIdentifier, int)[]? Capabilities { get; set; }
}
