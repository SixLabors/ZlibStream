// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Compression strategy for zlib.
    /// </summary>
    public enum ZlibCompressionStrategy
    {
        /// <summary>
        /// The default compression strategy.
        /// </summary>
        ZDEFAULTSTRATEGY,

        /// <summary>
        /// Filtered compression strategy.
        /// </summary>
        ZFILTERED,

        /// <summary>
        /// Huffman compression strategy.
        /// </summary>
        ZHUFFMANONLY,

        /// <summary>
        /// Run Length Encoded
        /// </summary>
        ZRLE
    }
}
