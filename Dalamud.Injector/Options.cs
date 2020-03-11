using CommandLine;

namespace Dalamud.Injector
{
    [Verb("inject")]
    internal sealed class InjectOptions
    {
        [Option('p', "pid", Required = true, HelpText = "A target process id to inject.")]
        public uint Pid { get; set; }

        [Option("root", Required = false)]
        public string? RootDirectory { get; set; }

        [Option("bin", Required = false)]
        public string? BinaryDirectory { get; set; }
    }

    [Verb("launch")]
    internal sealed class LaunchOptions
    {
        [Value(0)]
        public string ExecutablePath { get; set; } = null!;

        [Value(1, Required = false)]
        public string? CommandLine { get; set; }

        [Option("root", Required = false)]
        public string? RootDirectory { get; set; }

        [Option("bin", Required = false)]
        public string? BinaryDirectory { get; set; }
    }
}
