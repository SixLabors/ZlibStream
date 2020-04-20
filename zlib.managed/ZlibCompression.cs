// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    /// <summary>
    /// Compression levels for zlib.
    /// </summary>
    public enum ZlibCompression
    {
        /// <summary>
        /// The default compression level.
        /// </summary>
        ZDEFAULTCOMPRESSION = -1,

        /// <summary>
        /// No compression.
        /// </summary>
        ZNOCOMPRESSION,

        /// <summary>
        /// best speed compression level.
        /// </summary>
        ZBESTSPEED,

        /// <summary>
        /// the best compression level.
        /// </summary>
        ZBESTCOMPRESSION = 9,
    }
}
