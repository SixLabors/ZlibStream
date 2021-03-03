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
        /// The default compression. Used for normal data.
        /// </summary>
        DefaultStrategy = 0,

        /// <summary>
        /// Filtered compression. Used for data produced by a filter (or predictor).
        /// </summary>
        Filtered = 1,

        /// <summary>
        /// Force Huffman encoding only (no string match).
        /// </summary>
        HuffmanOnly = 2,

        /// <summary>
        /// Run Length Encoded. Designed to be almost as fast as <see cref="HuffmanOnly"/>,
        /// but give better compression for PNG image data.
        /// </summary>
        Rle = 3,

        /// <summary>
        /// Prevents the use of dynamic Huffman codes, allowing for a simpler
        /// decoder for special applications.
        /// </summary>
        Fixed = 4
    }
}
