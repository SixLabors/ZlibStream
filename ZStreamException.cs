// Copyright (c) 2018, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Els_Kom.Compression.Libs.Zlib
{
    using System.IO;

    /// <summary>
    /// The exception that is thrown when an zlib error occurs.
    /// </summary>
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
        /// Initializes a new instance of the <see cref="ZStreamException"/> class.
        /// </summary>
        /// <param name="s">exception message.</param>
        public ZStreamException(string s)
            : base(s)
        {
        }
    }
}