using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Configuration
{
    public interface IPluginConfiguration
    {
        int Version { get; set; }
    }
}
