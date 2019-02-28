// Copyright (c) 2018-2019, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// An input buffer customised for use by <see cref="ZInputStream"/>.
    /// </summary>
    /// <remarks>
    /// The buffer supports decryption of incoming data.
    /// </remarks>
    public class InflaterInputBuffer
    {
        private readonly Stream inputStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="InflaterInputBuffer"/> class with a default buffer size.
        /// </summary>
        /// <param name="stream">The stream to buffer.</param>
        public InflaterInputBuffer(Stream stream)
            : this(stream, 4096)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InflaterInputBuffer"/> class with a custom buffer size.
        /// </summary>
        /// <param name="stream">The stream to buffer.</param>
        /// <param name="bufferSize">The size to use for the buffer.</param>
        /// <remarks>A minimum buffer size of 1KB is permitted.  Lower sizes are treated as 1KB.</remarks>
        public InflaterInputBuffer(Stream stream, int bufferSize)
        {
            this.inputStream = stream;
            if (bufferSize < 1024)
            {
                bufferSize = 1024;
            }

            this.RawData = new byte[bufferSize];
            this.ClearText = this.RawData;
        }

        /// <summary>
        /// Gets the length of bytes bytes in the <see cref="RawData"/>.
        /// </summary>
        public int RawLength { get; private set; }

        /// <summary>
        /// Gets the contents of the raw data buffer.
        /// </summary>
        /// <remarks>This may contain encrypted data.</remarks>
        public IEnumerable<byte> RawData { get; private set; }

        /// <summary>
        /// Gets the number of useable bytes in <see cref="ClearText"/>.
        /// </summary>
        public int ClearTextLength { get; private set; }

        /// <summary>
        /// Gets the contents of the clear text buffer.
        /// </summary>
        public IEnumerable<byte> ClearText { get; private set; }

        /// <summary>
        /// Gets or Sets the number of bytes available.
        /// </summary>
        public int Available { get; set; }

        /// <summary>
        /// Call <see cref="ZInputStream.Read(byte[], int, int)"/> passing the current clear text buffer contents.
        /// </summary>
        /// <param name="zinput">The stream for which to call Read.</param>
        public void SetInflaterInput(ZInputStream zinput)
        {
            if (this.Available > 0)
            {
                // I think this should read.
                zinput.Read(this.ClearText.ToArray(), this.ClearTextLength - this.Available, this.Available);

                // .SetInput(this.ClearText, this.ClearTextLength - this.Available, this.Available);
                this.Available = 0;
            }
        }

        /// <summary>
        /// Fill the buffer from the underlying input stream.
        /// </summary>
        public void Fill()
        {
            this.RawLength = 0;
            var toRead = this.RawData.ToArray().Length;

            while (toRead > 0)
            {
                var count = this.inputStream.Read(this.RawData.ToArray(), this.RawLength, toRead);
                if (count <= 0)
                {
                    break;
                }

                this.RawLength += count;
                toRead -= count;
            }

            this.ClearTextLength = this.RawLength;

            this.Available = this.ClearTextLength;
        }

        /// <summary>
        /// Read a buffer directly from the input stream.
        /// </summary>
        /// <param name="buffer">The buffer to fill.</param>
        /// <returns>Returns the number of bytes read.</returns>
        public int ReadRawBuffer(byte[] buffer)
            => this.ReadRawBuffer(buffer, 0, buffer.Length);

        /// <summary>
        /// Read a buffer directly from the input stream.
        /// </summary>
        /// <param name="outBuffer">The buffer to read into.</param>
        /// <param name="offset">The offset to start reading data into.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>Returns the number of bytes read.</returns>
        public int ReadRawBuffer(byte[] outBuffer, int offset, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var currentOffset = offset;
            var currentLength = length;

            while (currentLength > 0)
            {
                if (this.Available <= 0)
                {
                    this.Fill();
                    if (this.Available <= 0)
                    {
                        return 0;
                    }
                }

                var toCopy = Math.Min(currentLength, this.Available);
                Array.Copy(this.RawData.ToArray(), this.RawLength - this.Available, outBuffer, currentOffset, toCopy);
                currentOffset += toCopy;
                currentLength -= toCopy;
                this.Available -= toCopy;
            }

            return length;
        }

        /// <summary>
        /// Read clear text data from the input stream.
        /// </summary>
        /// <param name="outBuffer">The buffer to add data to.</param>
        /// <param name="offset">The offset to start adding data at.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>Returns the number of bytes actually read.</returns>
        public int ReadClearTextBuffer(byte[] outBuffer, int offset, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var currentOffset = offset;
            var currentLength = length;

            while (currentLength > 0)
            {
                if (this.Available <= 0)
                {
                    this.Fill();
                    if (this.Available <= 0)
                    {
                        return 0;
                    }
                }

                var toCopy = Math.Min(currentLength, this.Available);
                Array.Copy(this.ClearText.ToArray(), this.ClearTextLength - this.Available, outBuffer, currentOffset, toCopy);
                currentOffset += toCopy;
                currentLength -= toCopy;
                this.Available -= toCopy;
            }

            return length;
        }

        /// <summary>
        /// Read a <see cref="byte"/> from the input stream.
        /// </summary>
        /// <returns>Returns the byte read.</returns>
        public int ReadLeByte()
        {
            if (this.Available <= 0)
            {
                this.Fill();
                if (this.Available <= 0)
                {
                    throw new Exception("EOF in header");
                }
            }

            var result = this.RawData.ToArray()[this.RawLength - this.Available];
            this.Available -= 1;
            return result;
        }

        /// <summary>
        /// Read an <see cref="short"/> in little endian byte order.
        /// </summary>
        /// <returns>The short value read case to an int.</returns>
        public int ReadLeShort()
            => this.ReadLeByte() | (this.ReadLeByte() << 8);

        /// <summary>
        /// Read an <see cref="int"/> in little endian byte order.
        /// </summary>
        /// <returns>The int value read.</returns>
        public int ReadLeInt()
            => this.ReadLeShort() | (this.ReadLeShort() << 16);

        /// <summary>
        /// Read a <see cref="long"/> in little endian byte order.
        /// </summary>
        /// <returns>The long value read.</returns>
        public long ReadLeLong()
            => (uint)this.ReadLeInt() | ((long)this.ReadLeInt() << 32);
    }
}
