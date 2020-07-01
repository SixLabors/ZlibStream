// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Compression state for zlib.
    /// </summary>
    internal enum CompressionState
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
