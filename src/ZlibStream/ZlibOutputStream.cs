// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides methods and properties for compressing and decompressing output streams by
    /// using the Zlib algorithm.
    /// </summary>
    public sealed class ZlibOutputStream : Stream
    {
        private readonly int bufferSize;
        private byte[] chunkBuffer;
        private readonly byte[] byteBuffer = new byte[1];
        private readonly bool compress;
        private bool isFinished;
        private ZStream zStream;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public ZlibOutputStream(Stream output)
        {
            this.bufferSize = 512;
            this.compress = false;
            this.BaseStream = output;
            this.InitBlock();
            this.zStream = new ZStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        /// <param name="level">The compression level for the data to compress.</param>
        public ZlibOutputStream(Stream output, CompressionLevel level)
        {
            this.bufferSize = 512;
            this.compress = true;
            this.BaseStream = output;
            this.InitBlock();
            this.zStream = new ZStream(level);
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
        [MethodImpl(InliningOptions.ShortMethod)]
        public override void WriteByte(byte value)
        {
            this.byteBuffer[0] = value;
            this.Write(this.byteBuffer, 0, 1);
        }

        /// <inheritdoc/>
        [MethodImpl(InliningOptions.ShortMethod)]
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
            {
                ThrowHelper.ThrowNullException(nameof(buffer));
            }

            if (count == 0)
            {
                return;
            }

            CompressionState state;
            this.zStream.INextIn = buffer;
            this.zStream.NextInIndex = offset;
            this.zStream.AvailIn = count;
            do
            {
                this.zStream.INextOut = this.chunkBuffer;
                this.zStream.NextOutIndex = 0;
                this.zStream.AvailOut = this.bufferSize;
                state = this.compress
                    ? this.zStream.Deflate(this.FlushMode)
                    : this.zStream.Inflate(this.FlushMode);

                if (state != CompressionState.ZOK && state != CompressionState.ZSTREAMEND)
                {
                    ThrowHelper.ThrowCompressionException(this.compress, this.zStream.Msg);
                }

                this.BaseStream.Write(this.chunkBuffer, 0, this.bufferSize - this.zStream.AvailOut);
                if (!this.compress && this.zStream.AvailIn == 0 && this.zStream.AvailOut == 0)
                {
                    break;
                }

                if (state == CompressionState.ZSTREAMEND)
                {
                    break;
                }
            }
            while (this.zStream.AvailIn > 0 || this.zStream.AvailOut == 0);
        }

        /// <inheritdoc/>
        public override void Flush() => this.BaseStream.Flush();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value)
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
        [MethodImpl(InliningOptions.ShortMethod)]
        private void Finish()
        {
            if (!this.isFinished)
            {
                CompressionState state;
                do
                {
                    this.zStream.INextOut = this.chunkBuffer;
                    this.zStream.NextOutIndex = 0;
                    this.zStream.AvailOut = this.bufferSize;
                    state = this.compress
                        ? this.zStream.Deflate(FlushStrategy.Finish)
                        : this.zStream.Inflate(FlushStrategy.Finish);

                    if (state != CompressionState.ZSTREAMEND && state != CompressionState.ZOK)
                    {
                        ThrowHelper.ThrowCompressionException(this.compress, this.zStream.Msg);
                    }

                    if (this.bufferSize - this.zStream.AvailOut > 0)
                    {
                        this.BaseStream.Write(this.chunkBuffer, 0, this.bufferSize - this.zStream.AvailOut);
                    }

                    if (state == CompressionState.ZSTREAMEND)
                    {
                        break;
                    }
                }
                while (this.zStream.AvailIn > 0 || this.zStream.AvailOut == 0);

                try
                {
                    this.Flush();
                }
                finally
                {
                    this.isFinished = true;
                }
            }
        }

        private void InitBlock()
        {
            this.FlushMode = FlushStrategy.NoFlush;
            this.chunkBuffer = ArrayPool<byte>.Shared.Rent(this.bufferSize);
        }
    }
}
