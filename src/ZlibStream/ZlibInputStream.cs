// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Buffers;
using System.IO;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides methods and properties for compressing and decompressing input streams by
    /// using the Zlib algorithm.
    /// </summary>
    public sealed class ZlibInputStream : Stream
    {
        private const int BufferSize = 4096;
        private byte[] chunkBuffer;
        private readonly byte[] byteBuffer = new byte[1];
        private readonly bool compress;
        private bool noMoreinput;
        private bool isFinished;
        private ZStream zStream = new ZStream();
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibInputStream"/> class.
        /// </summary>
        /// <param name="input">The input stream.</param>
        public ZlibInputStream(Stream input)
        {
            this.BaseStream = input;
            this.InitBlock();
            _ = this.zStream.InflateInit();
            this.compress = false;
            this.zStream.INextIn = this.chunkBuffer;
            this.zStream.NextInIndex = 0;
            this.zStream.AvailIn = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibInputStream"/> class.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="level">The compression level for the data to compress.</param>
        public ZlibInputStream(Stream input, ZlibCompressionLevel level)
        {
            this.BaseStream = input;
            this.InitBlock();
            _ = this.zStream.DeflateInit(level);
            this.compress = true;
            this.zStream.INextIn = this.chunkBuffer;
            this.zStream.NextInIndex = 0;
            this.zStream.AvailIn = 0;
        }

        /// <summary>
        /// Gets a reference to the underlying stream.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Gets or sets the flush mode for this stream.
        /// </summary>
        public ZlibFlushStrategy FlushMode { get; set; }

        /// <summary>
        /// Gets the total number of bytes input so far.
        /// </summary>
        public long TotalIn => this.zStream.TotalIn;

        /// <summary>
        /// Gets the total number of bytes output so far.
        /// </summary>
        public long TotalOut => this.zStream.TotalOut;

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int ReadByte()
            => this.Read(this.byteBuffer, 0, 1) == -1
            ? -1
            : this.byteBuffer[0] & 0xFF;

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return 0;
            }

            ZlibCompressionState err;
            this.zStream.INextOut = buffer;
            this.zStream.NextOutIndex = offset;
            this.zStream.AvailOut = count;
            do
            {
                if ((this.zStream.AvailIn == 0) && (!this.noMoreinput))
                {
                    // If buffer is empty and more input is available, refill it
                    this.zStream.NextInIndex = 0;
                    this.zStream.AvailIn = this.BaseStream.Read(this.chunkBuffer, 0, BufferSize);

                    if (this.zStream.AvailIn == -1)
                    {
                        this.zStream.AvailIn = 0;
                        this.noMoreinput = true;
                    }
                }

                err = this.compress
                    ? this.zStream.Deflate(this.FlushMode)
                    : this.zStream.Inflate(this.FlushMode);

                if (this.noMoreinput && (err == ZlibCompressionState.ZBUFERROR))
                {
                    return -1;
                }

                if (err != ZlibCompressionState.ZOK && err != ZlibCompressionState.ZSTREAMEND)
                {
                    throw new ZStreamException((this.compress ? "de" : "in") + "flating: " + this.zStream.Msg);
                }

                if (this.noMoreinput && (this.zStream.AvailOut == count))
                {
                    return -1;
                }
            }
            while (this.zStream.AvailOut > 0 && err == ZlibCompressionState.ZOK);

            return count - this.zStream.AvailOut;
        }

        /// <inheritdoc/>
        public override void Flush() => this.BaseStream.Flush();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        this.Finish();
                    }
                    finally
                    {
                        this.EndStream();
                        ArrayPool<byte>.Shared.Return(this.chunkBuffer);
                    }
                }

                this.isDisposed = true;
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Finishes the stream.
        /// </summary>
        private void Finish()
        {
            if (!this.isFinished)
            {
                ZlibCompressionState err;
                do
                {
                    this.zStream.INextOut = this.chunkBuffer;
                    this.zStream.NextOutIndex = 0;
                    this.zStream.AvailOut = BufferSize;
                    err = this.compress ? this.zStream.Deflate(ZlibFlushStrategy.ZFINISH) : this.zStream.Inflate(ZlibFlushStrategy.ZFINISH);

                    if (err != ZlibCompressionState.ZSTREAMEND && err != ZlibCompressionState.ZOK)
                    {
                        throw new ZStreamException((this.compress ? "de" : "in") + "flating: " + this.zStream.Msg);
                    }

                    if (BufferSize - this.zStream.AvailOut > 0)
                    {
                        this.BaseStream.Write(this.chunkBuffer, 0, BufferSize - this.zStream.AvailOut);
                    }

                    if (err == ZlibCompressionState.ZSTREAMEND)
                    {
                        break;
                    }
                }
                while (this.zStream.AvailIn > 0 || this.zStream.AvailOut == 0);

                this.isFinished = true;
            }
        }

        /// <summary>
        /// Ends the compression or decompression on the stream.
        /// </summary>
        private void EndStream()
        {
            _ = this.compress ? this.zStream.DeflateEnd() : this.zStream.InflateEnd();

            this.zStream.Free();
            this.zStream = null;
        }

        private void InitBlock()
        {
            this.FlushMode = ZlibFlushStrategy.ZNOFLUSH;
            this.chunkBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        }
    }
}
