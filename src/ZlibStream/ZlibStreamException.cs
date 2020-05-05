// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    using System;
    using System.IO;
#if SUPPORTS_SERIALIZATION
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// The exception that is thrown when an Zlib error occurs.
    /// </summary>
    [Serializable]
    public sealed class ZlibStreamException : IOException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibStreamException"/> class with no argrument.
        /// </summary>
        public ZlibStreamException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibStreamException" /> class with
        /// its message string set to <paramref name="message" />, its HRESULT set to COR_E_IO,
        /// and its inner exception set to <see langword="null" />.</summary>
        /// <param name="message">A <see cref="string" /> that describes the error. The content of <paramref name="message" /> is intended to be understood by humans. The caller of this constructor is required to ensure that this string has been localized for the current system culture.</param>
        public ZlibStreamException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibStreamException" /> class with
        /// a specified error message and a reference to the inner exception that is the cause
        /// of this exception.</summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception.
        /// If the <paramref name="innerException" /> parameter is not <see langword="null" />, the
        /// current exception is raised in a <see langword="catch" /> block that handles the inner
        /// exception.
        /// </param>
        public ZlibStreamException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibStreamException" /> class with its message
        /// string set to <paramref name="message" /> and its HRESULT user-defined.
        /// </summary>
        /// <param name="message">
        /// A <see cref="string" /> that describes the error.
        /// The content of <paramref name="message" /> is intended to be understood by humans.
        /// The caller of this constructor is required to ensure that this string has been localized
        /// for the current system culture.
        /// </param>
        /// <param name="hresult">An integer identifying the error that has occurred.</param>
        public ZlibStreamException(string message, int hresult)
            : base(message, hresult)
        {
        }

#if SUPPORTS_SERIALIZATION
        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibStreamException"/> class
        /// with the specified serialization and context information.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object.</param>
        /// <param name="context">The source and destination for the object.</param>
        private ZlibStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
