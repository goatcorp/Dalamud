using System.Collections.Generic;

namespace Dalamud.Bootstrap.Windows
{
    internal sealed class ProcessCreationOptions
    {
        public string ImagePath { get; set; } = null!;

        public string? CommandLine { get; set; } = null;

        public IEnumerable<KeyValuePair<string, string>>? Environments { get; set; } = null;

        public bool CreateSuspended { get; set; }
    }
}
