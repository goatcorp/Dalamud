using System;
using CommandLine;


namespace Dalamud.Injector
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<InjectOptions>(args)
                .WithParsed<InjectOptions>(opt =>
                {
                    
                });
        }
    }
}
