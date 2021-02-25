// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides methods and properties for compressing and decompressing input streams by
    /// using the Zlib algorithm.
    /// </summary>
    public sealed class ZlibInputStream : Stream
    {
        private readonly int bufferSize;
        private byte[] chunkBuffer;
        private readonly byte[] byteBuffer = new byte[1];
        private readonly bool compress;
        private bool noMoreinput;
        private bool isFinished;
        private ZStream zStream;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibInputStream"/> class.
        /// </summary>
        /// <param name="input">The input stream.</param>
        public ZlibInputStream(Stream input)
        {
            this.bufferSize = 8192;
            this.compress = false;
            this.BaseStream = input;
            this.InitBlock();
            this.zStream = new ZStream
            {
                NextIn = this.chunkBuffer,
                NextInIndex = 0,
                AvailableIn = 0
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibInputStream"/> class.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="level">The compression level for the data to compress.</param>
        public ZlibInputStream(Stream input, CompressionLevel level)
        {
            this.bufferSize = 8192;
            this.compress = true;
            this.BaseStream = input;

            this.InitBlock();
            this.zStream = new ZStream(level)
            {
                NextIn = this.chunkBuffer,
                NextInIndex = 0,
                AvailableIn = 0
            };
        }

        /// <summary>
        /// Gets a reference to the underlying stream.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Gets or sets the flush mode for this stream.
        /// </summary>
        public FlushStrategy FlushMode { get; set; }

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
        [MethodImpl(InliningOptions.ShortMethod)]
        public override int ReadByte()
            => this.Read(this.byteBuffer, 0, 1) == -1
            ? -1
            : this.byteBuffer[0] & 0xFF;

        /// <inheritdoc/>
        [MethodImpl(InliningOptions.ShortMethod)]
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return 0;
            }

            CompressionState state;
            this.zStream.NextOut = buffer;
            this.zStream.NextOutIndex = offset;
            this.zStream.AvailableOut = count;
            do
            {
                if ((this.zStream.AvailableIn == 0) && (!this.noMoreinput))
                {
                    // If buffer is empty and more input is available, refill it
                    this.zStream.NextInIndex = 0;
                    this.zStream.AvailableIn = this.BaseStream.Read(this.chunkBuffer, 0, this.bufferSize);

                    if (this.zStream.AvailableIn == -1)
                    {
                        this.zStream.AvailableIn = 0;
                        this.noMoreinput = true;
                    }
                }

                state = this.compress
                    ? this.zStream.Deflate(this.FlushMode)
                    : this.zStream.Inflate(this.FlushMode);

                if (this.noMoreinput && (state == CompressionState.ZBUFERROR))
                {
                    return -1;
                }

                if (state != CompressionState.ZOK && state != CompressionState.ZSTREAMEND)
                {
                    ThrowHelper.ThrowCompressionException(this.compress, this.zStream.Message);
                }

                if (this.noMoreinput && (this.zStream.AvailableOut == count))
                {
                    return -1;
                }
            }
            while (this.zStream.AvailableOut > 0 && state == CompressionState.ZOK);

            return count - this.zStream.AvailableOut;
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
                        this.zStream.Dispose();
                        this.zStream = null;
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
                CompressionState state;
                do
                {
                    this.zStream.NextOut = this.chunkBuffer;
                    this.zStream.NextOutIndex = 0;
                    this.zStream.AvailableOut = this.bufferSize;
                    state = this.compress
                        ? this.zStream.Deflate(FlushStrategy.Finish)
                        : this.zStream.Inflate(FlushStrategy.Finish);

                    if (state != CompressionState.ZSTREAMEND && state != CompressionState.ZOK)
                    {
                        ThrowHelper.ThrowCompressionException(this.compress, this.zStream.Message);
                    }

                    if (this.bufferSize - this.zStream.AvailableOut > 0)
                    {
                        this.BaseStream.Write(this.chunkBuffer, 0, this.bufferSize - this.zStream.AvailableOut);
                    }

                    if (state == CompressionState.ZSTREAMEND)
                    {
                        break;
                    }
                }
                while (this.zStream.AvailableIn > 0 || this.zStream.AvailableOut == 0);

                this.isFinished = true;
            }
        }

        private void InitBlock()
        {
            this.FlushMode = FlushStrategy.NoFlush;
            this.chunkBuffer = ArrayPool<byte>.Shared.Rent(this.bufferSize);
        }
    }
}
