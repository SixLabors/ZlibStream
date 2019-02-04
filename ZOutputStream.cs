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
    /// Class that provices a zlib output stream that supports
    /// compression and decompression.
    /// </summary>
    public class ZOutputStream : BinaryWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        public ZOutputStream(Stream output)
            : base(output)
        {
            this.InitBlock();
            this.Z.InflateInit();
            this.Compress = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZOutputStream"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        /// <param name="level">The compression level for the data to compress.</param>
        public ZOutputStream(Stream output, int level)
            : base(output)
        {
            this.InitBlock();
            this.Z.DeflateInit(level);
            this.Compress = true;
        }

        /// <summary>
        /// Gets the base zlib stream.
        /// </summary>
        public ZStream Z { get; private set; } = new ZStream();

        /// <summary>
        /// Gets or sets the flush mode for this stream.
        /// </summary>
        public virtual int FlushMode { get; set; }

        /// <summary>Gets the total number of bytes input so far.</summary>
        public virtual long TotalIn => this.Z.TotalIn;

        /// <summary>Gets the total number of bytes output so far.</summary>
        public virtual long TotalOut => this.Z.TotalOut;

        /// <summary>
        /// Gets the stream's buffer size.
        /// </summary>
        protected internal int Bufsize { get; private set; } = 4096;

        /// <summary>
        /// Gets the stream's buffer.
        /// </summary>
        protected internal IEnumerable<byte> Buf { get; private set; }

        /// <summary>
        /// Gets the stream's single byte buffer value.
        /// For reading 1 byte at a time.
        /// </summary>
        protected internal IEnumerable<byte> Buf1 { get; private set; } = new byte[1];

        /// <summary>
        /// Gets a value indicating whether this stream is setup for compression.
        /// </summary>
        protected internal bool Compress { get; private set; }

        /// <inheritdoc/>
        public override void Write(byte b) => this.Write((int)b);

        /// <inheritdoc/>
        public override void Write(int b)
        {
            this.Buf1.ToArray()[0] = (byte)b;
            this.Write(this.Buf1.ToArray(), 0, 1);
        }

        /// <inheritdoc/>
        public override void Write(byte[] b1, int off, int len)
        {
            if (len == 0)
            {
                return;
            }

            int err;
            var b = new byte[b1.Length];
            Array.Copy(b1, 0, b, 0, b1.Length);
            this.Z.NextIn = b;
            this.Z.NextInIndex = off;
            this.Z.AvailIn = len;
            do
            {
                this.Z.NextOut = this.Buf;
                this.Z.NextOutIndex = 0;
                this.Z.AvailOut = this.Bufsize;
                err = this.Compress ? this.Z.Deflate(this.FlushMode) : this.Z.Inflate(this.FlushMode);

                if (err != ZlibConst.ZOK && err != ZlibConst.ZSTREAMEND)
                {
                    throw new ZStreamException((this.Compress ? "de" : "in") + "flating: " + this.Z.Msg);
                }

                this.BaseStream.Write(this.Buf.ToArray(), 0, this.Bufsize - this.Z.AvailOut);
                if (!this.Compress && this.Z.AvailIn == 0 && this.Z.AvailOut == 0)
                {
                    break;
                }

                if (err == ZlibConst.ZSTREAMEND)
                {
                    break;
                }
            }
            while (this.Z.AvailIn > 0 || this.Z.AvailOut == 0);
        }

        /// <summary>
        /// Finishes the stream.
        /// </summary>
        public virtual void Finish()
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
            try
            {
                this.Flush();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Ends the compression or decompression on the stream.
        /// </summary>
        public virtual void EndStream()
        {
            if (this.Compress)
            {
                this.Z.DeflateEnd();
            }
            else
            {
                this.Z.InflateEnd();
            }

            this.Z.Free();
            this.Z = null;
        }

        /// <inheritdoc/>
        public override void Close()
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
                this.BaseStream.Close();
            }
        }

        /// <inheritdoc/>
        public override void Flush() => base.Flush();

        private void InitBlock()
        {
            this.FlushMode = ZlibConst.ZNOFLUSH;
            this.Buf = new byte[this.Bufsize];
        }
    }
}
