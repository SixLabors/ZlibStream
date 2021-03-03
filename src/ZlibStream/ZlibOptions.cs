// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides options for ZLib compression and decompression operations.
    /// </summary>
    public sealed class ZlibOptions
    {
        /// <summary>
        /// Gets or sets the compression level.
        /// </summary>
        public CompressionLevel? CompressionLevel { get; set; }

        /// <summary>
        /// Gets or sets the compression strategy.
        /// </summary>
        public CompressionStrategy CompressionStrategy { get; set; }

        /// <summary>
        /// Gets or sets the flush mode.
        /// </summary>
        public FlushMode FlushMode { get; set; }
    }
}
