// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    using System;
    using System.IO;

    /// <summary>
    /// Zlib Memory Compression and Decompression Helper Class.
    /// </summary>
    public static class MemoryZlib
    {
        /// <summary>
        /// Compresses data using the default compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        [Obsolete("Use MemoryZlib.Compress(byte[], out byte[], out int) instead. This will be removed in a future release.")]
        public static void CompressData(byte[] inData, out byte[] outData, out int adler32)
            => Compress(inData, out outData, out adler32);

        /// <summary>
        /// Compresses data using the default compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        [Obsolete("Use MemoryZlib.Compress(byte[], out byte[]) instead. This will be removed in a future release.")]
        public static void CompressData(byte[] inData, out byte[] outData)
            => Compress(inData, out outData);

        /// <summary>
        /// Compresses data using an specific compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        [Obsolete("Use MemoryZlib.Compress(byte[], out byte[], int) instead. This will be removed in a future release.")]
        public static void CompressData(byte[] inData, out byte[] outData, int level)
            => Compress(inData, out outData, level);

        /// <summary>
        /// Compresses data using an specific compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        [Obsolete("Use MemoryZlib.Compress(byte[], out byte[], int, out int) instead. This will be removed in a future release.")]
        public static void CompressData(byte[] inData, out byte[] outData, int level, out int adler32)
            => Compress(inData, out outData, level, out adler32);

        /// <summary>
        /// Decompresses data.
        /// </summary>
        /// <param name="inData">The compressed input data.</param>
        /// <param name="outData">The decompressed output data.</param>
        /// <exception cref="NotUnpackableException">Thrown when the stream Errors in any way.</exception>
        [Obsolete("Use MemoryZlib.Decompress(byte[], out byte[]) instead. This will be removed in a future release.")]
        public static void DecompressData(byte[] inData, out byte[] outData)
            => Decompress(inData, out outData);

        // NEW: Now there are shortcut methods for compressing a file using the fully qualified path.

        /// <summary>
        /// Compresses data using the default compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(byte[] inData, out byte[] outData, out int adler32)
            => Compress(inData, out outData, ZlibConst.ZDEFAULTCOMPRESSION, out adler32);

        /// <summary>
        /// Compresses a file using the default compression level.
        /// </summary>
        /// <param name="path">The file to compress.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(string path, out byte[] outData, out int adler32)
            => Compress(File.ReadAllBytes(path), out outData, ZlibConst.ZDEFAULTCOMPRESSION, out adler32);

        /// <summary>
        /// Compresses data using the default compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(byte[] inData, out byte[] outData)
            => Compress(inData, out outData, ZlibConst.ZDEFAULTCOMPRESSION);

        /// <summary>
        /// Compresses a file using the default compression level.
        /// </summary>
        /// <param name="path">The file to compress.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(string path, out byte[] outData)
            => Compress(File.ReadAllBytes(path), out outData, ZlibConst.ZDEFAULTCOMPRESSION);

        /// <summary>
        /// Compresses data using an specific compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        // discard returned adler32. The caller does not want it.
        public static void Compress(byte[] inData, out byte[] outData, int level)
            => Compress(inData, out outData, level, out var adler32);

        /// <summary>
        /// Compresses a file using the default compression level.
        /// </summary>
        /// <param name="path">The file to compress.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        // discard returned adler32. The caller does not want it.
        public static void Compress(string path, out byte[] outData, int level)
            => Compress(File.ReadAllBytes(path), out outData, level, out var adler32);

        /// <summary>
        /// Compresses data using an specific compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(byte[] inData, out byte[] outData, int level, out int adler32)
        {
            using (var outMemoryStream = new MemoryStream())
            using (var outZStream = new ZOutputStream(outMemoryStream, level))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                try
                {
                    inMemoryStream.CopyTo(outZStream);
                }
                catch (ZStreamException)
                {
                    // the compression or decompression failed.
                }

                try
                {
                    outZStream.Flush();
                }
                catch (StackOverflowException ex)
                {
                    throw new NotPackableException("Compression Failed due to a stack overflow.", ex);
                }

                try
                {
                    outZStream.Finish();
                }
                catch (ZStreamException ex)
                {
                    throw new NotPackableException("Compression Failed.", ex);
                }

                outData = outMemoryStream.ToArray();
                adler32 = (int)(outZStream.Z.Adler & 0xffff);
            }
        }

        /// <summary>
        /// Compresses a file using an specific compression level.
        /// </summary>
        /// <param name="path">The file to compress.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(string path, out byte[] outData, int level, out int adler32)
            => Compress(File.ReadAllBytes(path), out outData, level, out adler32);

        /// <summary>
        /// Decompresses data.
        /// </summary>
        /// <param name="inData">The compressed input data.</param>
        /// <param name="outData">The decompressed output data.</param>
        /// <exception cref="NotUnpackableException">
        /// Thrown when the internal decompression stream errors in any way.
        /// </exception>
        public static void Decompress(byte[] inData, out byte[] outData)
        {
            using (var outMemoryStream = new MemoryStream())
            using (var outZStream = new ZOutputStream(outMemoryStream))
            using (Stream inMemoryStream = new MemoryStream(inData))
            {
                try
                {
                    inMemoryStream.CopyTo(outZStream);
                }
                catch (ZStreamException)
                {
                    // the compression or decompression failed.
                }

                try
                {
                    outZStream.Flush();
                }
                catch (StackOverflowException ex)
                {
                    throw new NotPackableException("Decompression Failed due to a stack overflow.", ex);
                }

                try
                {
                    outZStream.Finish();
                }
                catch (ZStreamException ex)
                {
                    throw new NotUnpackableException("Decompression Failed.", ex);
                }

                outData = outMemoryStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses a file.
        /// </summary>
        /// <param name="path">The file to decompress.</param>
        /// <param name="outData">The decompressed output data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal decompression stream errors in any way.
        /// </exception>
        public static void Decompress(string path, out byte[] outData)
            => Decompress(File.ReadAllBytes(path), out outData);
    }
}
