// Copyright (c) 2014-2018, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: MIT, see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    using System;
    using System.IO;

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
    }
}
