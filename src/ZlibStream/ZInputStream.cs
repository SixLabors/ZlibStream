// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Class that provices a zlib input stream that supports
    /// compression and decompression.
    /// </summary>
    public class ZInputStream : Stream
    {
        private byte[] pBuf;
        private byte[] pBuf1 = new byte[1];
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZInputStream"/> class.
        /// </summary>
        /// <param name="input">The input stream.</param>
        public ZInputStream(Stream input)
        {
            this.BaseStream = input;
            this.InitBlock();
            _ = this.Z.InflateInit();
            this.Compress = false;
            this.Z.INextIn = this.pBuf;
            this.Z.NextInIndex = 0;
            this.Z.AvailIn = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZInputStream"/> class.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="level">The compression level for the data to compress.</param>
        public ZInputStream(Stream input, ZlibCompression level)
        {
            this.BaseStream = input;
            this.InitBlock();
            _ = this.Z.DeflateInit(level);
            this.Compress = true;
            this.Z.INextIn = this.pBuf;
            this.Z.NextInIndex = 0;
            this.Z.AvailIn = 0;
        }

        /// <summary>
        /// Gets the base stream that this stream contains.
        /// </summary>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// Gets the base zlib stream.
        /// </summary>
        public ZStream Z { get; private set; } = new ZStream();

        /// <summary>
        /// Gets or sets the flush mode for this stream.
        /// </summary>
        public virtual ZlibFlushStrategy FlushMode { get; set; }

        /// <summary>
        /// Gets the total number of bytes input so far.
        /// </summary>
        public virtual long TotalIn => this.Z.TotalIn;

        /// <summary>
        /// Gets the total number of bytes output so far.
        /// </summary>
        public virtual long TotalOut => this.Z.TotalOut;

        /// <summary>
        /// Gets or sets a value indicating whether there is more input.
        /// </summary>
        public bool NoMoreinput { get; set; }

        /// <summary>
        /// Gets a value indicating whether the stream is finished.
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => this.BaseStream.Length;

        /// <inheritdoc/>
        public override long Position { get => this.BaseStream.Position; set => this.BaseStream.Position = value; }

        /// <summary>
        /// Gets the stream's buffer size.
        /// </summary>
        protected int Bufsize { get; private set; } = 512;

        /// <summary>
        /// Gets the stream's buffer.
        /// </summary>
        protected List<byte> Buf => this.pBuf.ToList();

        /// <summary>
        /// Gets the stream's single byte buffer value.
        /// For reading 1 byte at a time.
        /// </summary>
        protected List<byte> Buf1 => this.pBuf1.ToList();

        /// <summary>
        /// Gets a value indicating whether this stream is setup for compression.
        /// </summary>
        protected bool Compress { get; private set; }

        /// <inheritdoc/>
        public override int ReadByte()
            => this.Read(this.pBuf1, 0, 1) == -1
            ? -1
            : this.pBuf1[0] & 0xFF;

        /// <inheritdoc/>
        public override int Read(byte[] b, int off, int len)
        {
            if (len == 0)
            {
                return 0;
            }

            ZlibCompressionState err;
            this.Z.INextOut = b;
            this.Z.NextOutIndex = off;
            this.Z.AvailOut = len;
            do
            {
                if ((this.Z.AvailIn == 0) && (!this.NoMoreinput))
                {
                    // if buffer is empty and more input is avaiable, refill it
                    this.Z.NextInIndex = 0;
                    this.Z.AvailIn = SupportClass.ReadInput(this.BaseStream, this.pBuf, 0, this.Bufsize); // (bufsize<z.avail_out ? bufsize : z.avail_out));
                    if (this.Z.AvailIn == -1)
                    {
                        this.Z.AvailIn = 0;
                        this.NoMoreinput = true;
                    }
                }

                err = this.Compress
                    ? this.Z.Deflate(this.FlushMode)
                    : this.Z.Inflate(this.FlushMode);

                if (this.NoMoreinput && (err == ZlibCompressionState.ZBUFERROR))
                {
                    return -1;
                }

                if (err != ZlibCompressionState.ZOK && err != ZlibCompressionState.ZSTREAMEND)
                {
                    throw new ZStreamException((this.Compress ? "de" : "in") + "flating: " + this.Z.Msg);
                }

                if (this.NoMoreinput && (this.Z.AvailOut == len))
                {
                    return -1;
                }
            }
            while (this.Z.AvailOut > 0 && err == ZlibCompressionState.ZOK);

            // System.err.print("("+(len-z.avail_out)+")");
            return len - this.Z.AvailOut;
        }

        /// <summary>
        /// Skips a certin amount of data.
        /// </summary>
        /// <param name="n">The amount to skip.</param>
        /// <returns>
        /// less than or equal to count depending on the data available
        /// in the source Stream or -1 if the end of the stream is
        /// reached.
        /// </returns>
        public long Skip(long n)
        {
            var len = 512;
            if (n < len)
            {
                len = (int)n;
            }

            var tmp = new byte[len];
            return SupportClass.ReadInput(this.BaseStream, tmp, 0, tmp.Length);
        }

        /// <summary>
        /// Finishes the stream.
        /// </summary>
        public virtual void Finish()
        {
            if (!this.IsFinished)
            {
                ZlibCompressionState err;
                do
                {
                    this.Z.INextOut = this.pBuf;
                    this.Z.NextOutIndex = 0;
                    this.Z.AvailOut = this.Bufsize;
                    err = this.Compress ? this.Z.Deflate(ZlibFlushStrategy.ZFINISH) : this.Z.Inflate(ZlibFlushStrategy.ZFINISH);

                    if (err != ZlibCompressionState.ZSTREAMEND && err != ZlibCompressionState.ZOK)
                    {
                        throw new ZStreamException((this.Compress ? "de" : "in") + "flating: " + this.Z.Msg);
                    }

                    if (this.Bufsize - this.Z.AvailOut > 0)
                    {
                        this.BaseStream.Write(this.pBuf, 0, this.Bufsize - this.Z.AvailOut);
                    }

                    if (err == ZlibCompressionState.ZSTREAMEND)
                    {
                        break;
                    }
                }
                while (this.Z.AvailIn > 0 || this.Z.AvailOut == 0);

                this.IsFinished = true;
            }
        }

        /// <summary>
        /// Ends the compression or decompression on the stream.
        /// </summary>
        public virtual void EndStream()
        {
            _ = this.Compress ? this.Z.DeflateEnd() : this.Z.InflateEnd();

            this.Z.Free();
            this.Z = null;
        }

        /// <inheritdoc/>
        public override void Flush() => this.BaseStream.Flush();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => 0;

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        try
                        {
                            this.Finish();
                        }
                        catch
                        {
                        }
                    }
                    finally
                    {
                        this.EndStream();
                    }
                }

                this.isDisposed = true;
                base.Dispose(disposing);
            }
        }

        private void InitBlock()
        {
            this.FlushMode = ZlibFlushStrategy.ZNOFLUSH;
            this.pBuf = new byte[this.Bufsize];
        }
    }
}
