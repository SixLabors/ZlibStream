// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    /// <summary>
    /// Flush Strategy for zlib.
    /// </summary>
    public enum ZlibFlushStrategy
    {
        /// <summary>
        /// No flush.
        /// </summary>
        ZNOFLUSH,

        /// <summary>
        /// Partial flush.
        /// </summary>
        ZPARTIALFLUSH,

        /// <summary>
        /// Sync flush.
        /// </summary>
        ZSYNCFLUSH,

        /// <summary>
        /// Full flush.
        /// </summary>
        ZFULLFLUSH,

        /// <summary>
        /// Finish compression or decompression.
        /// </summary>
        ZFINISH,
    }
}
