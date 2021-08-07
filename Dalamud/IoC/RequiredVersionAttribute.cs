using System;

namespace Dalamud.IoC
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RequiredVersionAttribute : Attribute
    {
        public readonly Version Version;

        public RequiredVersionAttribute(string version) => this.Version = new(version);
    }
}
