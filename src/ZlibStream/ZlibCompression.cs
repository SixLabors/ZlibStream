// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors
{
    /// <summary>
    /// Provides enumeration of general compression levels for Zlib.
    /// </summary>
    public enum ZlibCompressionLevel
    {
        /// <summary>
        /// The default compression level.
        /// </summary>
        ZDEFAULTCOMPRESSION = -1,

        /// <summary>
        /// No compression.
        /// </summary>
        ZNOCOMPRESSION = 0,

        /// <summary>
        /// Best speed compression level.
        /// </summary>
        ZBESTSPEED = 1,

        /// <summary>
        /// Best compression level.
        /// </summary>
        ZBESTCOMPRESSION = 9,
    }
}
