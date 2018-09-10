// Copyright (c) 2018, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Els_Kom.Compression.Libs.Zlib
{
    /// <summary>
    /// Class that holds the contant values to zlib.
    /// </summary>
    public sealed class ZlibConst
    {
        // compression levels

        /// <summary>
        /// No compression.
        /// </summary>
        public const int ZNOCOMPRESSION = 0;

        /// <summary>
        /// best speed compression level.
        /// </summary>
        public const int ZBESTSPEED = 1;

        /// <summary>
        /// the best compression level.
        /// </summary>
        public const int ZBESTCOMPRESSION = 9;

        /// <summary>
        /// The default compression level.
        /// </summary>
        public const int ZDEFAULTCOMPRESSION = -1;

        // compression strategy

        /// <summary>
        /// Filtered compression strategy.
        /// </summary>
        public const int ZFILTERED = 1;

        /// <summary>
        /// huffman compression strategy.
        /// </summary>
        public const int ZHUFFMANONLY = 2;

        /// <summary>
        /// The default compression strategy.
        /// </summary>
        public const int ZDEFAULTSTRATEGY = 0;

        /// <summary>
        /// No flush.
        /// </summary>
        public const int ZNOFLUSH = 0;

        /// <summary>
        /// Partial flush.
        /// </summary>
        public const int ZPARTIALFLUSH = 1;

        /// <summary>
        /// Sync flush.
        /// </summary>
        public const int ZSYNCFLUSH = 2;

        /// <summary>
        /// Full flush.
        /// </summary>
        public const int ZFULLFLUSH = 3;

        /// <summary>
        /// Finish compression or decompression.
        /// </summary>
        public const int ZFINISH = 4;

        /// <summary>
        /// All is ok.
        /// </summary>
        public const int ZOK = 0;

        /// <summary>
        /// Stream ended early.
        /// </summary>
        public const int ZSTREAMEND = 1;

        /// <summary>
        /// Need compression dictionary.
        /// </summary>
        public const int ZNEEDDICT = 2;

        /// <summary>
        /// Some other error.
        /// </summary>
        public const int ZERRNO = -1;

        /// <summary>
        /// Stream error.
        /// </summary>
        public const int ZSTREAMERROR = -2;

        /// <summary>
        /// Data error.
        /// </summary>
        public const int ZDATAERROR = -3;

        /// <summary>
        /// Memory error.
        /// </summary>
        public const int ZMEMERROR = -4;

        /// <summary>
        /// Buffer error.
        /// </summary>
        public const int ZBUFERROR = -5;

        /// <summary>
        /// Zlib version error.
        /// </summary>
        public const int ZVERSIONERROR = -6;

        /// <summary>
        /// Gets the version to zlib.net.
        /// </summary>
        /// <returns>The version string to this version of zlib.net.</returns>
        public static string Version() => typeof(ZlibConst).Assembly.GetName().Version.ToString(3);
    }
}