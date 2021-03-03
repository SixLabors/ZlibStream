// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides enumeration of flushing modes.
    /// </summary>
    public enum FlushMode
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
