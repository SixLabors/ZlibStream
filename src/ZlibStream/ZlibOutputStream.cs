// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Provides methods and properties used to compress and decompress
    /// output streams by using the zlib data format specification.
    /// </summary>
    public sealed class ZlibOutputStream : Stream
    {
        private readonly int bufferSize;
        private readonly byte[] chunkBuffer;
        private readonly byte[] byteBuffer = new byte[1];
        private readonly bool compress;
        private bool isFinished;
        private ZLibStream zStream;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public ZlibOutputStream(Stream output)
           : this(output, new ZlibOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        /// <param name="level">The compression level.</param>
        public ZlibOutputStream(Stream output, CompressionLevel level)
           : this(output, new ZlibOptions() { CompressionLevel = level })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        /// <param name="options">The compression options.</param>
        public ZlibOutputStream(Stream output, ZlibOptions options)
        {
            this.Options = options;
            this.bufferSize = 512;
            this.chunkBuffer = ArrayPool<byte>.Shared.Rent(this.bufferSize);
            this.zStream = new ZLibStream(options);
            this.compress = this.zStream.Compress;
            this.BaseStream = output;
        }

        /// <summary>
        /// Gets a reference to the underlying stream.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        public ZlibOptions Options { get; set; }

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

#if SUPPORTS_SPAN_STREAM

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            // TODO: Write Core using pointers for ZlibStream.NextIn, NextOut.
            // This will require rewriting Adler32 to allow passing a pointer.
            base.Write(buffer);
        }

#endif

        /// <inheritdoc/>
        [MethodImpl(InliningOptions.ShortMethod)]
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(buffer));
            }

            if (count == 0)
            {
                return;
            }

            CompressionState state;
            this.zStream.NextIn = buffer;
            this.zStream.NextInIndex = offset;
            this.zStream.AvailableIn = count;
            do
            {
                this.zStream.NextOut = this.chunkBuffer;
                this.zStream.NextOutIndex = 0;
                this.zStream.AvailableOut = this.bufferSize;
                state = this.compress
                    ? this.zStream.Deflate(this.Options.FlushMode)
                    : this.zStream.Inflate(this.Options.FlushMode);

                if (state != CompressionState.ZOK && state != CompressionState.ZSTREAMEND)
                {
                    ThrowHelper.ThrowCompressionException(this.compress, this.zStream.Message);
                }

                this.BaseStream.Write(this.chunkBuffer, 0, this.bufferSize - this.zStream.AvailableOut);
                if (!this.compress && this.zStream.AvailableIn == 0 && this.zStream.AvailableOut == 0)
                {
                    break;
                }

                if (state == CompressionState.ZSTREAMEND)
                {
                    break;
                }
            }
            while (this.zStream.AvailableIn > 0 || this.zStream.AvailableOut == 0);
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
                    this.zStream.NextOut = this.chunkBuffer;
                    this.zStream.NextOutIndex = 0;
                    this.zStream.AvailableOut = this.bufferSize;
                    state = this.compress
                        ? this.zStream.Deflate(FlushMode.Finish)
                        : this.zStream.Inflate(FlushMode.Finish);

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
    }
}
