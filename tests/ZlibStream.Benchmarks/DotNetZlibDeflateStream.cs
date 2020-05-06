// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ZlibStream.Benchmarks
{
    /// <summary>
    /// Provides methods and properties for compressing streams by using the Zlib Deflate algorithm.
    /// </summary>
    internal sealed class DotNetZlibDeflateStream : Stream
    {
        /// <summary>
        /// The raw stream containing the uncompressed image data.
        /// </summary>
        private readonly Stream rawStream;

        /// <summary>
        /// Computes the checksum for the data stream.
        /// </summary>
        private readonly Adler32 adler32 = new Adler32();

        /// <summary>
        /// A value indicating whether this instance of the given entity has been disposed.
        /// </summary>
        /// <value><see langword="true"/> if this instance has been disposed; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        /// If the entity is disposed, it must not be disposed a second
        /// time. The isDisposed field is set the first time the entity
        /// is disposed. If the isDisposed field is true, then the Dispose()
        /// method will not dispose again. This help not to prolong the entity's
        /// life in the Garbage Collector.
        /// </remarks>
        private bool isDisposed;

        /// <summary>
        /// The stream responsible for compressing the input stream.
        /// </summary>
        private DeflateStream deflateStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotNetZlibDeflateStream"/> class.
        /// </summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="compressionLevel">The compression level.</param>
        public DotNetZlibDeflateStream(Stream stream, int compressionLevel)
        {
            this.rawStream = stream;

            // Write the zlib header : http://tools.ietf.org/html/rfc1950
            // CMF(Compression Method and flags)
            // This byte is divided into a 4 - bit compression method and a
            // 4-bit information field depending on the compression method.
            // bits 0 to 3  CM Compression method
            // bits 4 to 7  CINFO Compression info
            //
            //   0   1
            // +---+---+
            // |CMF|FLG|
            // +---+---+
            int cmf = 0x78;
            int flg = 218;

            // http://stackoverflow.com/a/2331025/277304
            if (compressionLevel >= 5 && compressionLevel <= 6)
            {
                flg = 156;
            }
            else if (compressionLevel >= 3 && compressionLevel <= 4)
            {
                flg = 94;
            }
            else if (compressionLevel <= 2)
            {
                flg = 1;
            }

            // Just in case
            flg -= ((cmf * 256) + flg) % 31;

            if (flg < 0)
            {
                flg += 31;
            }

            this.rawStream.WriteByte((byte)cmf);
            this.rawStream.WriteByte((byte)flg);

            // Initialize the deflate Stream.
            CompressionLevel level = CompressionLevel.Optimal;

            if (compressionLevel >= 1 && compressionLevel <= 5)
            {
                level = CompressionLevel.Fastest;
            }
            else if (compressionLevel == 0)
            {
                level = CompressionLevel.NoCompression;
            }

            this.deflateStream = new DeflateStream(this.rawStream, level, true);
        }

        /// <inheritdoc/>
        public override bool CanRead => false;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            this.deflateStream.Flush();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.deflateStream.Write(buffer, offset, count);
            this.adler32.Update(buffer.AsSpan(offset, count));
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // dispose managed resources
                this.deflateStream.Dispose();
                this.deflateStream = null;

                // Add the crc
                uint crc = (uint)this.adler32.Value;
                this.rawStream.WriteByte((byte)((crc >> 24) & 0xFF));
                this.rawStream.WriteByte((byte)((crc >> 16) & 0xFF));
                this.rawStream.WriteByte((byte)((crc >> 8) & 0xFF));
                this.rawStream.WriteByte((byte)(crc & 0xFF));
            }

            base.Dispose(disposing);

            // Call the appropriate methods to clean up
            // unmanaged resources here.
            // Note disposing is done.
            this.isDisposed = true;
        }

        /// <summary>
        /// Computes Adler32 checksum for a stream of data. An Adler32
        /// checksum is not as reliable as a CRC32 checksum, but a lot faster to
        /// compute.
        /// </summary>
        /// <remarks>
        /// The specification for Adler32 may be found in RFC 1950.
        /// ZLIB Compressed Data Format Specification version 3.3)
        ///
        ///
        /// From that document:
        ///
        ///      "ADLER32 (Adler-32 checksum)
        ///       This contains a checksum value of the uncompressed data
        ///       (excluding any dictionary data) computed according to Adler-32
        ///       algorithm. This algorithm is a 32-bit extension and improvement
        ///       of the Fletcher algorithm, used in the ITU-T X.224 / ISO 8073
        ///       standard.
        ///
        ///       Adler-32 is composed of two sums accumulated per byte: s1 is
        ///       the sum of all bytes, s2 is the sum of all s1 values. Both sums
        ///       are done modulo 65521. s1 is initialized to 1, s2 to zero.  The
        ///       Adler-32 checksum is stored as s2*65536 + s1 in most-
        ///       significant-byte first (network) order."
        ///
        ///  "8.2. The Adler-32 algorithm
        ///
        ///    The Adler-32 algorithm is much faster than the CRC32 algorithm yet
        ///    still provides an extremely low probability of undetected errors.
        ///
        ///    The modulo on unsigned long accumulators can be delayed for 5552
        ///    bytes, so the modulo operation time is negligible.  If the bytes
        ///    are a, b, c, the second sum is 3a + 2b + c + 3, and so is position
        ///    and order sensitive, unlike the first sum, which is just a
        ///    checksum.  That 65521 is prime is important to avoid a possible
        ///    large class of two-byte errors that leave the check unchanged.
        ///    (The Fletcher checksum uses 255, which is not prime and which also
        ///    makes the Fletcher check insensitive to single byte changes 0 -
        ///    255.)
        ///
        ///    The sum s1 is initialized to 1 instead of zero to make the length
        ///    of the sequence part of s2, so that the length does not have to be
        ///    checked separately. (Any sequence of zeroes has a Fletcher
        ///    checksum of zero.)"
        /// </remarks>
        /// <see cref="ZlibInflateStream"/>
        /// <see cref="DotNetZlibDeflateStream"/>
        private sealed class Adler32
        {
            /// <summary>
            /// largest prime smaller than 65536
            /// </summary>
            private const uint Base = 65521;

            /// <summary>
            /// The checksum calculated to far.
            /// </summary>
            private uint checksum;

            /// <summary>
            /// Initializes a new instance of the <see cref="Adler32"/> class.
            /// The checksum starts off with a value of 1.
            /// </summary>
            public Adler32()
            {
                this.Reset();
            }

            public long Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.checksum;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                this.checksum = 1;
            }

            /// <summary>
            /// Updates the checksum with a byte value.
            /// </summary>
            /// <param name="value">
            /// The data value to add. The high byte of the int is ignored.
            /// </param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(int value)
            {
                // We could make a length 1 byte array and call update again, but I
                // would rather not have that overhead
                uint s1 = this.checksum & 0xFFFF;
                uint s2 = this.checksum >> 16;

                s1 = (s1 + ((uint)value & 0xFF)) % Base;
                s2 = (s1 + s2) % Base;

                this.checksum = (s2 << 16) + s1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(ReadOnlySpan<byte> data)
            {
                ref byte dataRef = ref MemoryMarshal.GetReference(data);
                uint s1 = this.checksum & 0xFFFF;
                uint s2 = this.checksum >> 16;

                int count = data.Length;
                int offset = 0;

                while (count > 0)
                {
                    // We can defer the modulo operation:
                    // s1 maximally grows from 65521 to 65521 + 255 * 3800
                    // s2 maximally grows by 3800 * median(s1) = 2090079800 < 2^31
                    int n = 3800;
                    if (n > count)
                    {
                        n = count;
                    }

                    count -= n;
                    while (--n >= 0)
                    {
                        s1 += Unsafe.Add(ref dataRef, offset++);
                        s2 += s1;
                    }

                    s1 %= Base;
                    s2 %= Base;
                }

                this.checksum = (s2 << 16) | s1;
            }
        }
    }
}
