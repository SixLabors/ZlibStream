// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
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
        /// huffman compression strategy.
        /// </summary>
        ZHUFFMANONLY,
    }
}
