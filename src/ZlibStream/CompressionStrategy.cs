// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Compression strategy for zlib.
    /// </summary>
    public enum CompressionStrategy : int
    {
        /// <summary>
        /// The default compression strategy.
        /// </summary>
        DefaultStrategy = 0,

        /// <summary>
        /// Filtered compression strategy.
        /// </summary>
        Filtered = 1,

        /// <summary>
        /// Huffman compression strategy.
        /// </summary>
        HuffmanOnly = 2,

        /// <summary>
        /// Run Length Encoded
        /// </summary>
        Rle = 3
    }
}
