// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The zlib stream class.
    /// </summary>
    internal sealed class ZStream
    {
        private const int MAXWBITS = 15; // 32K LZ77 window
        private const int DEFWBITS = MAXWBITS;

        /// <summary>
        /// Gets or sets the next input bytes.
        /// </summary>
        public byte[] INextIn { get; set; }

        /// <summary>
        /// Gets or sets the next output bytes.
        /// </summary>
        public byte[] INextOut { get; set; }

        /// <summary>
        /// Gets or sets the next input byte index.
        /// </summary>
        public int NextInIndex { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes available at next_in.
        /// </summary>
        public int AvailIn { get; set; }

        /// <summary>
        /// Gets or sets the total number of input bytes read so far.
        /// </summary>
        public long TotalIn { get; set; }

        /// <summary>
        /// Gets or sets the next output byte index.
        /// </summary>
        public int NextOutIndex { get; set; }

        /// <summary>
        /// Gets or sets the remaining free space at next_out.
        /// </summary>
        public int AvailOut { get; set; }

        /// <summary>
        /// Gets or sets the total number of bytes output so far.
        /// </summary>
        public long TotalOut { get; set; }

        /// <summary>
        /// Gets or sets the stream's error message.
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// Gets or sets the Stream Data's Adler32 checksum.
        /// </summary>
        public uint Adler { get; set; }

        /// <summary>
        /// Gets or sets the current Deflate instance for this class.
        /// </summary>
        public Deflate Dstate { get; internal set; }

        /// <summary>
        /// Gets the current Inflate instance for this class.
        /// </summary>
        public Inflate Istate { get; private set; }

        /// <summary>
        /// Gets or sets the data type to this instance of this class.
        /// </summary>
        public int DataType { get; set; } // best guess about the data type: ascii or binary

        /// <summary>
        /// Initializes decompression.
        /// </summary>
        /// <returns>The state.</returns>
        public ZlibCompressionState InflateInit()
            => this.InflateInit(DEFWBITS);

        /// <summary>
        /// Initializes decompression.
        /// </summary>
        /// <param name="w">The window size.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState InflateInit(int w)
        {
            this.Istate = new Inflate();
            return this.Istate.InflateInit(this, w);
        }

        /// <summary>
        /// Decompresses data.
        /// </summary>
        /// <param name="f">The flush mode to use.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState Inflate(ZlibFlushStrategy f)
            => this.Istate == null
            ? ZlibCompressionState.ZSTREAMERROR
            : ZlibStream.Inflate.Decompress(this, f);

        /// <summary>
        /// Ends decompression.
        /// </summary>
        /// <returns>The zlib status state.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        public ZlibCompressionState InflateEnd()
        {
            if (this.Istate == null)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            ZlibCompressionState ret = this.Istate.InflateEnd(this);
            this.Istate = null;
            return ret;
        }

        /// <summary>
        /// Syncs inflate.
        /// </summary>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState InflateSync()
            => this.Istate == null
            ? ZlibCompressionState.ZSTREAMERROR
            : ZlibStream.Inflate.InflateSync(this);

        /// <summary>
        /// Sets the inflate dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to use.</param>
        /// <param name="dictLength">The dictionary length.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState InflateSetDictionary(byte[] dictionary, int dictLength)
            => this.Istate == null
            ? ZlibCompressionState.ZSTREAMERROR
            : ZlibStream.Inflate.InflateSetDictionary(this, dictionary, dictLength);

        /// <summary>
        /// Initializes compression.
        /// </summary>
        /// <param name="level">The compression level to use.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState DeflateInit(ZlibCompressionLevel level)
            => this.DeflateInit(level, MAXWBITS);

        /// <summary>
        /// Initializes compression.
        /// </summary>
        /// <param name="level">The compression level to use.</param>
        /// <param name="bits">The window bits to use.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState DeflateInit(ZlibCompressionLevel level, int bits)
        {
            this.Dstate = new Deflate();
            return this.Dstate.DeflateInit(this, level, bits);
        }

        /// <summary>
        /// Compress data.
        /// </summary>
        /// <param name="flush">The flush mode to use on the data.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState Deflate(ZlibFlushStrategy flush)
            => this.Dstate == null ? ZlibCompressionState.ZSTREAMERROR : this.Dstate.Compress(this, flush);

        /// <summary>
        /// Ends compression.
        /// </summary>
        /// <returns>The zlib status state.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        public ZlibCompressionState DeflateEnd()
        {
            if (this.Dstate == null)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            ZlibCompressionState ret = this.Dstate.DeflateEnd();
            this.Dstate = null;
            return ret;
        }

        /// <summary>
        /// Sets the compression paramiters.
        /// </summary>
        /// <param name="level">The compression level to use.</param>
        /// <param name="strategy">The strategy to use for compression.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState DeflateParams(ZlibCompressionLevel level, ZlibCompressionStrategy strategy)
            => this.Dstate == null
            ? ZlibCompressionState.ZSTREAMERROR
            : this.Dstate.DeflateParams(this, level, strategy);

        /// <summary>
        /// Sets the deflate dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to use.</param>
        /// <param name="dictLength">The dictionary length.</param>
        /// <returns>The zlib status state.</returns>
        public ZlibCompressionState DeflateSetDictionary(byte[] dictionary, int dictLength)
            => this.Dstate == null
            ? ZlibCompressionState.ZSTREAMERROR
            : this.Dstate.DeflateSetDictionary(this, dictionary, dictLength);

        /// <summary>
        /// Frees everything.
        /// </summary>
        public void Free()
        {
            this.INextIn = null;
            this.INextOut = null;
            this.Msg = null;
        }

        // Read a new buffer from the current input stream, update the adler32
        // and total number of bytes read.  All deflate() input goes through
        // this function so some applications may wish to modify it to avoid
        // allocating a large strm->next_in buffer and copying from it.
        // (See also flush_pending()).
        [MethodImpl(InliningOptions.ShortMethod)]
        public int Read_buf(byte[] buf, int start, int size)
        {
            int len = this.AvailIn;

            if (len > size)
            {
                len = size;
            }

            if (len == 0)
            {
                return 0;
            }

            this.AvailIn -= len;

            if (this.Dstate.Noheader == 0)
            {
                this.Adler = Adler32.Calculate(this.Adler, this.INextIn, this.NextInIndex, len);
            }

            Buffer.BlockCopy(this.INextIn, this.NextInIndex, buf, start, len);
            this.NextInIndex += len;
            this.TotalIn += len;
            return len;
        }
    }
}
