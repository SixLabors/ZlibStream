// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides enumeration of compression levels for Zlib.
    /// </summary>
    public enum ZlibCompressionLevel
    {
        /// <summary>
        /// The default compression level. Equivalent to <see cref="Six"/>.
        /// </summary>
        ZDEFAULTCOMPRESSION = -1,

        /// <summary>
        /// Level 0. Equivalent to <see cref="ZNOCOMPRESSION"/>.
        /// </summary>
        Zero = 0,

        /// <summary>
        /// No compression. Equivalent to <see cref="Zero"/>.
        /// </summary>
        ZNOCOMPRESSION = Zero,

        /// <summary>
        /// Level 1. Equivalent to <see cref="ZBESTSPEED"/>.
        /// </summary>
        One = 1,

        /// <summary>
        /// Best speed compression level.
        /// </summary>
        ZBESTSPEED = One,

        /// <summary>
        /// Level 2.
        /// </summary>
        Two = 2,

        /// <summary>
        /// Level 3.
        /// </summary>
        Three = 3,

        /// <summary>
        /// Level 4.
        /// </summary>
        Four = 4,

        /// <summary>
        /// Level 5.
        /// </summary>
        Five = 5,

        /// <summary>
        /// Level 6.
        /// </summary>
        Six = 6,

        /// <summary>
        /// Level 7.
        /// </summary>
        Seven = 7,

        /// <summary>
        /// Level 8.
        /// </summary>
        Eight = 8,

        /// <summary>
        /// Level 9. Equivalent to <see cref="ZBESTCOMPRESSION"/>.
        /// </summary>
        Nine = 9,

        /// <summary>
        /// Best compression level. Equivalent to <see cref="Nine"/>.
        /// </summary>
        ZBESTCOMPRESSION = Nine,
    }
}
