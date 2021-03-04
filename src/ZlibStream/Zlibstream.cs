// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// The zlib stream class.
    /// </summary>
    internal sealed unsafe class ZLibStream : IDisposable
    {
        private const int MAXWBITS = 15; // 32K LZ77 window
        private const int DEFWBITS = MAXWBITS;
        private bool isDisposed;

        public ZLibStream(ZlibOptions options)
        {
            if (options.CompressionLevel is null)
            {
                this.InflateInit();
            }
            else
            {
                this.Compress = true;
                this.DeflateInit(options);
            }
        }

        /// <summary>
        /// Gets or sets the next input bytes.
        /// </summary>
        public byte[] NextIn { get; set; }

        /// <summary>
        /// Gets or sets the next output bytes.
        /// </summary>
        public byte[] NextOut { get; set; }

        /// <summary>
        /// Gets or sets the next input byte index.
        /// </summary>
        public int NextInIndex { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes available at <see cref="NextIn"/>.
        /// </summary>
        public int AvailableIn { get; set; }

        /// <summary>
        /// Gets or sets the total number of input bytes read so far.
        /// </summary>
        public long TotalIn { get; set; }

        /// <summary>
        /// Gets or sets the next output byte index.
        /// </summary>
        public int NextOutIndex { get; set; }

        /// <summary>
        /// Gets or sets the remaining free space at <see cref="NextOut"/>.
        /// </summary>
        public int AvailableOut { get; set; }

        /// <summary>
        /// Gets or sets the total number of bytes output so far.
        /// </summary>
        public long TotalOut { get; set; }

        /// <summary>
        /// Gets or sets the stream's error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the stream data <see cref="Adler32"/> checksum.
        /// </summary>
        public uint Adler { get; set; }

        /// <summary>
        /// Gets the current deflate instance for this class.
        /// </summary>
        public Deflate DeflateState { get; private set; }

        /// <summary>
        /// Gets the current inflate instance for this class.
        /// </summary>
        public Inflate InflateState { get; private set; }

        /// <summary>
        /// Gets or sets the data type to this instance of this class.
        /// </summary>
        public int DataType { get; set; } // best guess about the data type: ascii or binary

        /// <summary>
        /// Gets a value indicating whether compression is taking place.
        /// </summary>
        public bool Compress { get; }

        /// <summary>
        /// Initializes decompression.
        /// </summary>
        public void InflateInit()
            => this.InflateInit(DEFWBITS);

        /// <summary>
        /// Initializes decompression.
        /// </summary>
        /// <param name="windowBits">The window size in bits.</param>
        public void InflateInit(int windowBits)
            => this.InflateState = new Inflate(this, windowBits);

        /// <summary>
        /// Decompresses data.
        /// </summary>
        /// <param name="strategy">The flush mode to use.</param>
        /// <returns>The zlib status state.</returns>
        public CompressionState Inflate(FlushMode strategy)
            => this.InflateState == null
            ? CompressionState.ZSTREAMERROR
            : ZlibStream.Inflate.Decompress(this, strategy);

        /// <summary>
        /// Syncs inflate.
        /// </summary>
        /// <returns>The zlib status state.</returns>
        public CompressionState InflateSync()
            => this.InflateState == null
            ? CompressionState.ZSTREAMERROR
            : ZlibStream.Inflate.InflateSync(this);

        /// <summary>
        /// Sets the inflate dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to use.</param>
        /// <param name="dictLength">The dictionary length.</param>
        /// <returns>The zlib status state.</returns>
        public CompressionState InflateSetDictionary(byte[] dictionary, int dictLength)
            => this.InflateState == null
            ? CompressionState.ZSTREAMERROR
            : ZlibStream.Inflate.InflateSetDictionary(this, dictionary, dictLength);

        /// <summary>
        /// Initializes compression.
        /// </summary>
        /// <param name="options">The options.</param>
        public void DeflateInit(ZlibOptions options)
            => this.DeflateInit(options, MAXWBITS);

        /// <summary>
        /// Initializes compression.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="windowBits">The window bits to use.</param>
        public void DeflateInit(ZlibOptions options, int windowBits)
            => this.DeflateState = new Deflate(this, options, windowBits);

        /// <summary>
        /// Compress data.
        /// </summary>
        /// <param name="flush">The flush mode to use on the data.</param>
        /// <returns>The zlib status state.</returns>
        public CompressionState Deflate(FlushMode flush)
            => this.DeflateState == null
            ? CompressionState.ZSTREAMERROR
            : this.DeflateState.Compress(this, flush);

        /// <summary>
        /// Sets the compression parameters.
        /// </summary>
        /// <param name="level">The compression level to use.</param>
        /// <param name="strategy">The strategy to use for compression.</param>
        /// <returns>The zlib status state.</returns>
        public CompressionState DeflateParams(CompressionLevel level, CompressionStrategy strategy)
            => this.DeflateState == null
            ? CompressionState.ZSTREAMERROR
            : this.DeflateState.DeflateParams(this, level, strategy);

        /// <summary>
        /// Sets the deflate dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to use.</param>
        /// <param name="dictLength">The dictionary length.</param>
        /// <returns>The zlib status state.</returns>
        public CompressionState DeflateSetDictionary(byte[] dictionary, int dictLength)
            => this.DeflateState == null
            ? CompressionState.ZSTREAMERROR
            : this.DeflateState.DeflateSetDictionary(this, dictionary, dictLength);

        // Read a new buffer from the current input stream, update the adler32
        // and total number of bytes read.  All deflate() input goes through
        // this function so some applications may wish to modify it to avoid
        // allocating a large strm->next_in buffer and copying from it.
        // (See also flush_pending()).
        [MethodImpl(InliningOptions.ShortMethod)]
        public int ReadBuffer(byte[] buffer, int start, int size)
        {
            int len = this.AvailableIn;

            if (len > size)
            {
                len = size;
            }

            if (len == 0)
            {
                return 0;
            }

            this.AvailableIn -= len;

            if (this.DeflateState.NoHeader == 0)
            {
                this.Adler = Adler32.Calculate(this.Adler, this.NextIn.AsSpan(this.NextInIndex, len));
            }

            Buffer.BlockCopy(this.NextIn, this.NextInIndex, buffer, start, len);
            this.NextInIndex += len;
            this.TotalIn += len;
            return len;
        }

        /// <inheritdoc/>
        public void Dispose() => this.Dispose(true);

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    if (this.Compress)
                    {
                        this.DeflateState?.Dispose();
                    }
                    else
                    {
                        this.InflateState?.Dispose();
                    }
                }

                this.isDisposed = true;
            }
        }
    }
}
