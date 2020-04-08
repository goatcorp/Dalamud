using System.Collections.Generic;

namespace Dalamud.Bootstrap.Windows
{
    internal sealed class ProcessCreationOptions
    {
        public string ImagePath { get; set; } = null!;

        public string? CommandLine { get; set; } = null;

        public IDictionary<string, string>? Environments { get; set; } = null;

        public bool CreateSuspended { get; set; }
    }
}
