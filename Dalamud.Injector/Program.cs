using System;
using System.IO;
using CommandLine;
using Dalamud.Bootstrap;
using static System.Environment;

namespace Dalamud.Injector
{
    internal static class Program
    {
        private static void Main(string[] args)
        {            
            Parser.Default.ParseArguments<InjectOptions, LaunchOptions>(args)
                .WithParsed<InjectOptions>(Inject)
                .WithParsed<LaunchOptions>(Launch);
        }

        private static void Inject(InjectOptions options)
        {
            var binDirectory = options.BinaryDirectory ?? GetDefaultBinaryDirectory();
            var rootDirectory = options.RootDirectory ?? GetDefaultRootDirectory();

            var boot = new Bootstrapper(new BootstrapperOptions
            {
                BinaryDirectory = binDirectory,
                RootDirectory = rootDirectory,
            });

            // ..
            boot.Relaunch(options.Pid);
        }

        private static void Launch(LaunchOptions options)
        {

        }

        private static string GetDefaultBinaryDirectory()
        {
            var root = GetDefaultRootDirectory();

            return Path.Combine(root, "bin");
        }

        private static string GetDefaultRootDirectory()
        {
            var localApp = Environment.GetFolderPath(SpecialFolder.LocalApplicationData);
            
            return Path.Combine(localApp, "Dalamud");
        }
    }
}
