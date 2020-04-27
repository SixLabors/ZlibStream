// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors
{
    using System;
    using System.IO;
#if SUPPORTS_SERIALIZATION
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// The exception that is thrown when an zlib error occurs.
    /// </summary>
    [Serializable]
    public class ZStreamException : IOException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZStreamException"/> class.
        /// </summary>
        public ZStreamException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZStreamException"/> class with its message
        /// string set to message, its HRESULT set to COR_E_IO, and its inner exception set to null.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        public ZStreamException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZStreamException"/> class with a specified
        /// error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the innerException
        /// parameter is not null, the current exception is raised in a catch block that handles the inner exception.
        /// </param>
        public ZStreamException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if SUPPORTS_SERIALIZATION
        /// <summary>
        /// Initializes a new instance of the <see cref="ZStreamException"/> class
        /// with the specified serialization and context information.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object.</param>
        /// <param name="context">The source and destination for the object.</param>
        protected ZStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
