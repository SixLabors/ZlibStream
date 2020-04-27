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
    /// Zlib Memory Packing failure error.
    /// </summary>
    [Serializable]
    public sealed class NotPackableException : IOException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotPackableException"/> class.
        /// </summary>
        public NotPackableException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotPackableException"/> class.
        /// </summary>
        /// <param name="s">The error string.</param>
        public NotPackableException(string s)
            : base(s)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotPackableException"/> class.
        /// </summary>
        /// <param name="s">The error string.</param>
        /// <param name="ex">The Exception that caused this Exception.</param>
        public NotPackableException(string s, Exception ex)
            : base(s, ex)
        {
        }

#if SUPPORTS_SERIALIZATION
        /// <summary>
        /// Initializes a new instance of the <see cref="NotPackableException"/> class
        /// with the specified serialization and context information.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object.</param>
        /// <param name="context">The source and destination for the object.</param>
        private NotPackableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
