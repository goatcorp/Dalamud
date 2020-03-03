using System;
using CommandLine;
using Dalamud.Injector.Windows;

namespace Dalamud.Injector
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var shit = Process.Open(12732);
            var cmd = shit.ReadCommandLine();
            /*Parser.Default.ParseArguments<InjectOptions>(args)
                .WithParsed<InjectOptions>(opt =>
                {
                    
                });*/
        }
    }
}
