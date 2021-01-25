using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Configuration
{
    class ThirdRepoSetting {
        public string Url { get; set; }
        public bool IsEnabled { get;set; }

        public ThirdRepoSetting Clone() {
            return new ThirdRepoSetting {
                Url = this.Url,
                IsEnabled = this.IsEnabled
            };
        }
    }
}
