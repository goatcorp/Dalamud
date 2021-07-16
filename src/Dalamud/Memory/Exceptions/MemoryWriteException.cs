using System;
using System.Runtime.Serialization;

namespace Dalamud.Memory.Exceptions
{
    /// <summary>
    /// An exception thrown when WriteProcessMemory fails.
    /// </summary>
    public class MemoryWriteException : MemoryException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryWriteException"/> class.
        /// </summary>
        public MemoryWriteException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryWriteException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MemoryWriteException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryWriteException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MemoryWriteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryWriteException"/> class.
        /// </summary>
        /// <param name="info">The object that holds the serialized data about the exception being thrown.</param>
        /// <param name="context">The object that contains contextual information about the source or destination.</param>
        protected MemoryWriteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
