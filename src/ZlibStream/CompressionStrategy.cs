// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Compression strategy for zlib.
    /// </summary>
    public enum CompressionStrategy
    {
        /// <summary>
        /// The default compression strategy.
        /// </summary>
        DefaultStrategy,

        /// <summary>
        /// Filtered compression strategy.
        /// </summary>
        Filtered,

        /// <summary>
        /// Huffman compression strategy.
        /// </summary>
        HuffmanOnly,

        /// <summary>
        /// Run Length Encoded
        /// </summary>
        Rle
    }
}
