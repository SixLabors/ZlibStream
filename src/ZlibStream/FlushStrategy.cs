// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides enumeration of flushing strategies for Zlib.
    /// </summary>
    public enum FlushStrategy : int
    {
        /// <summary>
        /// No flush.
        /// </summary>
        NoFlush = 0,

        /// <summary>
        /// Partial flush.
        /// </summary>
        PartialFlush = 1,

        /// <summary>
        /// Sync flush.
        /// </summary>
        SyncFlush = 2,

        /// <summary>
        /// Full flush.
        /// </summary>
        FullFlush = 3,

        /// <summary>
        /// Finish compression or decompression.
        /// </summary>
        Finish = 4,
    }
}
