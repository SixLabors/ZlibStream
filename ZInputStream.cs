/*
Copyright (c) 2006, ComponentAce
http://www.componentace.com
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in
the documentation and/or other materials provided with the distribution.

Neither the name of ComponentAce nor the names of its contributors
may be used to endorse or promote products derived from this
software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
/*
Copyright (c) 2001 Lapo Luchini.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in
the documentation and/or other materials provided with the distribution.

3. The names of the authors may not be used to endorse or promote products
derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS
OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
/*
* This program is based on zlib-1.1.3, so all credit should go authors
* Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
* and contributors of zlib.
*/

namespace ComponentAce.Compression.Libs.Zlib
{
    using System.IO;

    public class ZInputStream : BinaryReader
    {
        private readonly Stream inRenamed = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZInputStream"/> class.
        /// </summary>
        /// <param name="in_Renamed">The input stream.</param>
        public ZInputStream(Stream in_Renamed)
            : base(in_Renamed)
        {
            this.InitBlock();
            this.inRenamed = in_Renamed;
            this.Z.InflateInit();
            this.Compress = false;
            this.Z.NextIn = this.Buf;
            this.Z.NextInIndex = 0;
            this.Z.AvailIn = 0;
        }

        public ZInputStream(Stream in_Renamed, int level)
            : base(in_Renamed)
        {
            this.InitBlock();
            this.inRenamed = in_Renamed;
            this.Z.DeflateInit(level);
            this.Compress = true;
            this.Z.NextIn = this.Buf;
            this.Z.NextInIndex = 0;
            this.Z.AvailIn = 0;
        }

        public ZStream Z { get; private set; } = new ZStream();

        public virtual int FlushMode { get; set; }

        /// <summary> Gets the total number of bytes input so far.</summary>
        public virtual long TotalIn => this.Z.TotalIn;

        /// <summary> Gets the total number of bytes output so far.</summary>
        public virtual long TotalOut => this.Z.TotalOut;

        public bool Moreinput { get; set; }

        protected int Bufsize { get; private set; } = 512;

        protected byte[] Buf { get; private set; }

        protected byte[] Buf1 { get; private set; } = new byte[1];

        protected bool Compress { get; private set; }

        /*public int available() throws IOException {
        return inf.finished() ? 0 : 1;
        }*/

        public override int Read() => this.Read(this.Buf1, 0, 1) == -1 ? -1 : this.Buf1[0] & 0xFF;

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
                    this.Z.AvailIn = SupportClass.ReadInput(this.inRenamed, this.Buf, 0, this.Bufsize); // (bufsize<z.avail_out ? bufsize : z.avail_out));
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

        public override void Close() => this.inRenamed.Close();

        private void InitBlock()
        {
            this.FlushMode = ZlibConst.ZNOFLUSH;
            this.Buf = new byte[this.Bufsize];
        }
    }
}