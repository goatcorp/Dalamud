using System;
using CommandLine;

namespace Dalamud.Injector
{
    [Verb("inject")]
    internal sealed class InjectOptions
    {
        [Option('p', "pid", Required = true, HelpText = "A target process id to inject.")]
        public uint Pid { get; set; }

        [Option("root", Required = true, HelpText = "")]
        public string RootDirectory { get; set; } = null!;

        [Option("bin", Required = true, HelpText = "")]
        public string BinaryDirectory { get; set; } = null!;
    }
}
