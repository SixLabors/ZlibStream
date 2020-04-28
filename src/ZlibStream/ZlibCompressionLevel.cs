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
        /// The default compression level. Equivalent to <see cref="Level6"/>.
        /// </summary>
        ZDEFAULTCOMPRESSION = -1,

        /// <summary>
        /// Level 0. Equivalent to <see cref="ZNOCOMPRESSION"/>.
        /// </summary>
        Level0 = 0,

        /// <summary>
        /// No compression. Equivalent to <see cref="Level0"/>.
        /// </summary>
        ZNOCOMPRESSION = Level0,

        /// <summary>
        /// Level 1. Equivalent to <see cref="ZBESTSPEED"/>.
        /// </summary>
        Level1 = 1,

        /// <summary>
        /// Best speed compression level.
        /// </summary>
        ZBESTSPEED = Level1,

        /// <summary>
        /// Level 2.
        /// </summary>
        Level2 = 2,

        /// <summary>
        /// Level 3.
        /// </summary>
        Level3 = 3,

        /// <summary>
        /// Level 4.
        /// </summary>
        Level4 = 4,

        /// <summary>
        /// Level 5.
        /// </summary>
        Level5 = 5,

        /// <summary>
        /// Level 6.
        /// </summary>
        Level6 = 6,

        /// <summary>
        /// Level 7.
        /// </summary>
        Level7 = 7,

        /// <summary>
        /// Level 8.
        /// </summary>
        Level8 = 8,

        /// <summary>
        /// Level 9. Equivalent to <see cref="ZBESTCOMPRESSION"/>.
        /// </summary>
        Level9 = 9,

        /// <summary>
        /// Best compression level. Equivalent to <see cref="Level9"/>.
        /// </summary>
        ZBESTCOMPRESSION = Level9,
    }
}
