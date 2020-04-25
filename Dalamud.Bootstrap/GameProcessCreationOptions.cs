using System;
using System.Collections.Generic;
using System.Text;

namespace Dalamud.Bootstrap
{
    public sealed class GameProcessCreationOptions
    {
        public string ImagePath { get; set; } = null!;

        public IDictionary<string, string> Arguments { get; set; }

        public IDictionary<string, string>? Environments { get; set; }

        public bool CreateSuspended { get; set; }
    }
}
