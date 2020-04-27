// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors
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
