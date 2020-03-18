// Copyright (c) 2018-2020, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
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
            this.Z.NextIn = this.Buf;
            this.Z.NextInIndex = 0;
            this.Z.AvailIn = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZInputStream"/> class.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="level">The compression level for the data to compress.</param>
        public ZInputStream(Stream input, int level)
        {
            this.BaseStream = input;
            this.InitBlock();
            _ = this.Z.DeflateInit(level);
            this.Compress = true;
            this.Z.NextIn = this.Buf;
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
        public virtual int FlushMode { get; set; }

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
        public bool Moreinput { get; set; }

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
        protected IEnumerable<byte> Buf { get; private set; }

        /// <summary>
        /// Gets the stream's single byte buffer value.
        /// For reading 1 byte at a time.
        /// </summary>
        protected IEnumerable<byte> Buf1 { get; private set; } = new byte[1];

        /// <summary>
        /// Gets a value indicating whether this stream is setup for compression.
        /// </summary>
        protected bool Compress { get; private set; }

        /// <inheritdoc/>
        public override int ReadByte() => this.Read(this.Buf1.ToArray(), 0, 1) == -1 ? -1 : this.Buf1.ToArray()[0] & 0xFF;

        /// <inheritdoc/>
        public override int Read(byte[] b, int off, int len)
        {
            if (len == 0)
            {
                return 0;
            }

            int err;
            this.Z.NextOut = b;
            this.Z.NextOutIndex = off;
            this.Z.AvailOut = len;
            do
            {
                if ((this.Z.AvailIn == 0) && (!this.Moreinput))
                {
                    // if buffer is empty and more input is avaiable, refill it
                    this.Z.NextInIndex = 0;
                    this.Z.AvailIn = SupportClass.ReadInput(this.BaseStream, this.Buf.ToArray(), 0, this.Bufsize); // (bufsize<z.avail_out ? bufsize : z.avail_out));
                    if (this.Z.AvailIn == -1)
                    {
                        this.Z.AvailIn = 0;
                        this.Moreinput = true;
                    }
                }

                err = this.Compress ? this.Z.Deflate(this.FlushMode) : this.Z.Inflate(this.FlushMode);

                if (this.Moreinput && (err == ZlibConst.ZBUFERROR))
                {
                    return -1;
                }

                if (err != ZlibConst.ZOK && err != ZlibConst.ZSTREAMEND)
                {
                    throw new ZStreamException((this.Compress ? "de" : "in") + "flating: " + this.Z.Msg);
                }

                if (this.Moreinput && (this.Z.AvailOut == len))
                {
                    return -1;
                }
            }
            while (this.Z.AvailOut == len && err == ZlibConst.ZOK);

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
                int err;
                do
                {
                    this.Z.NextOut = this.Buf;
                    this.Z.NextOutIndex = 0;
                    this.Z.AvailOut = this.Bufsize;
                    err = this.Compress ? this.Z.Deflate(ZlibConst.ZFINISH) : this.Z.Inflate(ZlibConst.ZFINISH);

                    if (err != ZlibConst.ZSTREAMEND && err != ZlibConst.ZOK)
                    {
                        throw new ZStreamException((this.Compress ? "de" : "in") + "flating: " + this.Z.Msg);
                    }

                    if (this.Bufsize - this.Z.AvailOut > 0)
                    {
                        this.BaseStream.Write(this.Buf.ToArray(), 0, this.Bufsize - this.Z.AvailOut);
                    }

                    if (err == ZlibConst.ZSTREAMEND)
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
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This method should not throw any exceptions.")]
        public override void Close()
        {
            try
            {
                try
                {
                    this.Finish();
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                this.EndStream();
                this.BaseStream.Close();
            }
        }

        /// <inheritdoc/>
        public override void Flush() => this.BaseStream.Flush();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => 0;

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        private void InitBlock()
        {
            this.FlushMode = ZlibConst.ZNOFLUSH;
            this.Buf = new byte[this.Bufsize];
        }
    }
}
