// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    /// <summary>
    /// Compression state for zlib.
    /// </summary>
    public enum ZlibCompressionState
    {
        /// <summary>
        /// Zlib version error.
        /// </summary>
        ZVERSIONERROR = -6,

        /// <summary>
        /// Buffer error.
        /// </summary>
        ZBUFERROR,

        /// <summary>
        /// Memory error.
        /// </summary>
        ZMEMERROR,

        /// <summary>
        /// Data error.
        /// </summary>
        ZDATAERROR,

        /// <summary>
        /// Stream error.
        /// </summary>
        ZSTREAMERROR,

        /// <summary>
        /// Some other error.
        /// </summary>
        ZERRNO,

        /// <summary>
        /// All is ok.
        /// </summary>
        ZOK,

        /// <summary>
        /// Stream ended early.
        /// </summary>
        ZSTREAMEND,

        /// <summary>
        /// Need compression dictionary.
        /// </summary>
        ZNEEDDICT,
    }
}
