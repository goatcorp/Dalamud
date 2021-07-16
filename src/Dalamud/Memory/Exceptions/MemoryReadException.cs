using System;
using System.Runtime.Serialization;

namespace Dalamud.Memory.Exceptions
{
    /// <summary>
    /// An exception thrown when ReadProcessMemory fails.
    /// </summary>
    public class MemoryReadException : MemoryException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryReadException"/> class.
        /// </summary>
        public MemoryReadException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryReadException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MemoryReadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryReadException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MemoryReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryReadException"/> class.
        /// </summary>
        /// <param name="info">The object that holds the serialized data about the exception being thrown.</param>
        /// <param name="context">The object that contains contextual information about the source or destination.</param>
        protected MemoryReadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
