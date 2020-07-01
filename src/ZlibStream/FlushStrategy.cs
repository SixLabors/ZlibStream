// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides enumeration of flushing strategies for Zlib.
    /// </summary>
    public enum FlushStrategy
    {
        /// <summary>
        /// No flush.
        /// </summary>
        NoFlush,

        /// <summary>
        /// Partial flush.
        /// </summary>
        PartialFlush,

        /// <summary>
        /// Sync flush.
        /// </summary>
        SyncFlush,

        /// <summary>
        /// Full flush.
        /// </summary>
        FullFlush,

        /// <summary>
        /// Finish compression or decompression.
        /// </summary>
        Finish,
    }
}
