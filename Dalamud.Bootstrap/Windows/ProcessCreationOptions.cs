namespace Dalamud.Bootstrap.Windows
{
    internal sealed class ProcessCreationOptions
    {
        public string ImagePath { get; set; } = null!;

        public string? CommandLine { get; set; } = null;

        public bool CreateSuspended { get; set; } = true;
    }
}
