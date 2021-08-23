using System;
using System.Linq;

namespace Dalamud.Plugin
{
    /// <summary>
    /// This exception is thrown when an IPC method is checked out, but the type does not match what was previously registered.
    /// </summary>
    public class IpcTypeMismatchError : Exception
    {
        private readonly string message;

        /// <summary>
        /// Initializes a new instance of the <see cref="IpcTypeMismatchError"/> class.
        /// </summary>
        /// <param name="name">Name of the IPC method.</param>
        /// <param name="requestedTypes">The types requested when checking out the IPC.</param>
        /// <param name="actualTypes">The types registered by the IPC.</param>
        public IpcTypeMismatchError(string name, Type[] requestedTypes, Type[] actualTypes)
        {
            this.Name = name;
            this.RequestedTypes = requestedTypes;
            this.ActualTypes = actualTypes;

            var t1 = string.Join(", ", this.RequestedTypes.Select(t => t.Name));
            var t2 = string.Join(", ", this.ActualTypes.Select(t => t.Name));
            this.message = $"IPC method {this.Name} has a different type than was requested. [ {t1} ] != [ {t2} ]";
        }

        /// <summary>
        /// Gets the name of the IPC that was invoked.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the types that were requested.
        /// </summary>
        public Type[] RequestedTypes { get; }

        /// <summary>
        /// Gets the types that were previously registered.
        /// </summary>
        public Type[] ActualTypes { get; }

        /// <inheritdoc/>
        public override string Message => this.message;
    }
}
