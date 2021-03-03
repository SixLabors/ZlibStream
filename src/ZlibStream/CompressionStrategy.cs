// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides enumeration for the various compression strategies.
    /// </summary>
    public enum CompressionStrategy
    {
        /// <summary>
        /// The default compression.
        /// </summary>
        DefaultStrategy = 0,

        /// <summary>
        /// Filtered compression.
        /// </summary>
        Filtered = 1,

        /// <summary>
        /// Huffman compression.
        /// </summary>
        HuffmanOnly = 2,

        /// <summary>
        /// Run Length Encoded
        /// </summary>
        Rle = 3
    }
}
