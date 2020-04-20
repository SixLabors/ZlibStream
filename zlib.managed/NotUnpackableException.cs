// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    /// <summary>
    /// Zlib Memory Unpacking failure error.
    /// </summary>
    [Serializable]
    public sealed class NotUnpackableException : IOException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotUnpackableException"/> class with no argrument.
        /// </summary>
        public NotUnpackableException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotUnpackableException"/> class with an string argrument.
        /// </summary>
        /// <param name="s">The error string.</param>
        public NotUnpackableException(string s)
            : base(s)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotUnpackableException"/> class with an string argrument
        /// and the exception that cuased this exception.
        /// </summary>
        /// <param name="s">The error string.</param>
        /// <param name="ex">The Exception that caused this Exception.</param>
        public NotUnpackableException(string s, Exception ex)
            : base(s, ex)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotUnpackableException"/> class
        /// with the specified serialization and context information.
        /// </summary>
        /// <param name="info">The data for serializing or deserializing the object.</param>
        /// <param name="context">The source and destination for the object.</param>
        private NotUnpackableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
