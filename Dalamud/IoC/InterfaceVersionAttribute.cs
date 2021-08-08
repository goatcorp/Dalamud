using System;

namespace Dalamud.IoC
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    internal class InterfaceVersionAttribute : Attribute
    {
        public readonly Version Version;

        public InterfaceVersionAttribute(string version) => this.Version = new(version);
    }
}
