// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors
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
        [Obsolete("Use MemoryZlib.Compress(byte[], out byte[], ZlibCompression) instead. This will be removed in a future release.")]
        public static void CompressData(byte[] inData, out byte[] outData, ZlibCompression level)
            => Compress(inData, out outData, level);

        /// <summary>
        /// Compresses data using an specific compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <param name="level">The compression level to use.</param>
        /// <param name="adler32">The output adler32 of the data.</param>
        /// <exception cref="NotPackableException">Thrown when the stream Errors in any way.</exception>
        [Obsolete("Use MemoryZlib.Compress(byte[], out byte[], ZlibCompression, out int) instead. This will be removed in a future release.")]
        public static void CompressData(byte[] inData, out byte[] outData, ZlibCompression level, out int adler32)
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
            => Compress(inData, out outData, ZlibCompression.ZDEFAULTCOMPRESSION, out adler32);

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
            => Compress(File.ReadAllBytes(path), out outData, ZlibCompression.ZDEFAULTCOMPRESSION, out adler32);

        /// <summary>
        /// Compresses data using the default compression level.
        /// </summary>
        /// <param name="inData">The original input data.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(byte[] inData, out byte[] outData)
            => Compress(inData, out outData, ZlibCompression.ZDEFAULTCOMPRESSION);

        /// <summary>
        /// Compresses a file using the default compression level.
        /// </summary>
        /// <param name="path">The file to compress.</param>
        /// <param name="outData">The compressed output data.</param>
        /// <exception cref="NotPackableException">
        /// Thrown when the internal compression stream errors in any way.
        /// </exception>
        public static void Compress(string path, out byte[] outData)
            => Compress(File.ReadAllBytes(path), out outData, ZlibCompression.ZDEFAULTCOMPRESSION);

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
        public static void Compress(byte[] inData, out byte[] outData, ZlibCompression level)
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
        public static void Compress(string path, out byte[] outData, ZlibCompression level)
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
        public static void Compress(byte[] inData, out byte[] outData, ZlibCompression level, out int adler32)
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
                catch (Exception ex)
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
        public static void Compress(string path, out byte[] outData, ZlibCompression level, out int adler32)
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
                catch (Exception ex)
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

        /// <summary>
        /// Check data for compression by zlib.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <returns>Returns <see langword="true" /> if data is compressed by zlib, else <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="stream"/> is <see langword="null" />.</exception>
        public static bool IsCompressedByZlib(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var data = new byte[2];
            stream.Read(data, 0, 2);
            stream.Seek(-2, SeekOrigin.Current);
            return IsCompressedByZlib(data);
        }

        /// <summary>
        /// Check data for compression by zlib.
        /// </summary>
        /// <param name="path">The file to check on if it is compressed by zlib.</param>
        /// <returns>Returns <see langword="true" /> if data is compressed by zlib, else <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="path"/> is <see langword="null" /> or <see cref="string.Empty"/>.</exception>
        public static bool IsCompressedByZlib(string path)
            => IsCompressedByZlib(File.ReadAllBytes(path));

        /// <summary>
        /// Check data for compression by zlib.
        /// </summary>
        /// <param name="data">Input array.</param>
        /// <returns>Returns <see langword="true" /> if data is compressed by zlib, else <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="data"/> is <see langword="null" />.</exception>
        public static bool IsCompressedByZlib(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length < 2)
            {
                return false;
            }

            if (data[0] == 0x78)
            {
                if (data[1] == 0x01 || data[1] == 0x5E || data[1] == 0x9C || data[1] == 0xDA)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
