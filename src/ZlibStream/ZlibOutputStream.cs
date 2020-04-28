// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    /// <summary>
    /// Provides methods and properties for compressing and decompressing output streams by
    /// using the Zlib algorithm.
    /// </summary>
    public sealed class ZlibOutputStream : Stream
    {
        // TODO: Zlib appears to allow configuring this value.
        private const int BufferSize = 4096;
        private byte[] chunkBuffer;
        private readonly byte[] byteBuffer = new byte[1];
        private readonly bool compress;
        private bool isFinished;
        private ZStream zStream = new ZStream();
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public ZlibOutputStream(Stream output)
        {
            this.BaseStream = output;
            this.InitBlock();
            _ = this.zStream.InflateInit();
            this.compress = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        /// <param name="level">The compression level for the data to compress.</param>
        public ZlibOutputStream(Stream output, ZlibCompressionLevel level)
        {
            this.BaseStream = output;
            this.InitBlock();
            _ = this.zStream.DeflateInit(level);
            this.compress = true;
        }

        /// <summary>
        /// Gets a reference to the underlying stream.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Gets or sets the flush mode for this stream.
        /// </summary>
        public ZlibFlushStrategy FlushMode { get; set; }

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
        public override void WriteByte(byte value)
        {
            this.byteBuffer[0] = value;
            this.Write(this.byteBuffer, 0, 1);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count == 0)
            {
                return;
            }

            ZlibCompressionState err;
            this.zStream.INextIn = buffer;
            this.zStream.NextInIndex = offset;
            this.zStream.AvailIn = count;
            do
            {
                this.zStream.INextOut = this.chunkBuffer;
                this.zStream.NextOutIndex = 0;
                this.zStream.AvailOut = BufferSize;
                err = this.compress ? this.zStream.Deflate(this.FlushMode) : this.zStream.Inflate(this.FlushMode);

                if (err != ZlibCompressionState.ZOK && err != ZlibCompressionState.ZSTREAMEND)
                {
                    throw new ZStreamException((this.compress ? "de" : "in") + "flating: " + this.zStream.Msg);
                }

                this.BaseStream.Write(this.chunkBuffer, 0, BufferSize - this.zStream.AvailOut);
                if (!this.compress && this.zStream.AvailIn == 0 && this.zStream.AvailOut == 0)
                {
                    break;
                }

                if (err == ZlibCompressionState.ZSTREAMEND)
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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This method should not throw any exceptions.")]
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
