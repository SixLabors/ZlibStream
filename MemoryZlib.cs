// Copyright (c) 2018, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
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
        public static void CompressData(byte[] inData, out byte[] outData, out int adler32)
            => CompressData(inData, out outData, ZlibConst.ZDEFAULTCOMPRESSION, out adler32);

        /// <summary>
        /// Compresses data using the default compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        public static void CompressData(byte[] inData, out byte[] outData)
            => CompressData(inData, out outData, ZlibConst.ZDEFAULTCOMPRESSION);

        /// <summary>
        /// Compresses data using an specific compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        // discard returned adler32. The caller does not want it.
        public static void CompressData(byte[] inData, out byte[] outData, int level)
            => CompressData(inData, out outData, level, out var adler32);

        /// <summary>
        /// Compresses data using an specific compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        public static void CompressData(byte[] inData, out byte[] outData, int level, out int adler32)
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

                outZStream.Flush();
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
        /// Decompresses data.
        /// </summary>
        /// <param name="inData">The compressed input data.</param>
        /// <param name="outData">The decompressed output data.</param>
        /// <exception cref="NotUnpackableException">Thrown when the stream Errors in any way.</exception>
        public static void DecompressData(byte[] inData, out byte[] outData)
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

                outZStream.Flush();
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
    }
}
