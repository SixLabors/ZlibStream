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
Copyright (c) 2000,2001,2002,2003 ymnk, JCraft,Inc. All rights reserved.

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
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
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
    using System;

    public sealed class ZStream
    {
        private const int MAXWBITS = 15; // 32K LZ77 window
        private const int ZNOFLUSH = 0;
        private const int ZPARTIALFLUSH = 1;
        private const int ZSYNCFLUSH = 2;
        private const int ZFULLFLUSH = 3;
        private const int ZFINISH = 4;

        private const int MAXMEMLEVEL = 9;

        private const int ZOK = 0;
        private const int ZSTREAMEND = 1;
        private const int ZNEEDDICT = 2;
        private const int ZERRNO = -1;
        private const int ZSTREAMERROR = -2;
        private const int ZDATAERROR = -3;
        private const int ZMEMERROR = -4;
        private const int ZBUFERROR = -5;
        private const int ZVERSIONERROR = -6;
        private static readonly int DEFWBITS = MAXWBITS;

        public byte[] NextIn { get; set; } // next input byte

        public int NextInIndex { get; set; }

        public int AvailIn { get; set; } // number of bytes available at next_in

        public long TotalIn { get; set; } // total nb of input bytes read so far

        public byte[] NextOut { get; set; } // next output byte should be put there

        public int NextOutIndex { get; set; }

        public int AvailOut { get; set; } // remaining free space at next_out

        public long TotalOut { get; set; } // total nb of bytes output so far

        public string Msg { get; set; }

        /// <summary>
        /// Gets or sets the Stream Data's Adler32 checksum.
        /// </summary>
        public long Adler { get; set; }

        internal Deflate Dstate { get; set; }

        internal Inflate Istate { get; private set; }

        /// <summary>
        /// Gets or sets the data type to this instance of this class.
        /// </summary>
        internal int DataType { get; set; } // best guess about the data type: ascii or binary

        /// <summary>
        /// Gets the adler32 class instance to this instance of this class.
        /// </summary>
        internal Adler32 Adler32 { get; private set; } = new Adler32();

        public int InflateInit() => this.InflateInit(DEFWBITS);

        public int InflateInit(int w)
        {
            this.Istate = new Inflate();
            return this.Istate.InflateInit(this, w);
        }

        public int Inflate(int f) => this.Istate == null ? ZSTREAMERROR : this.Istate.Decompress(this, f);

        public int InflateEnd()
        {
            if (this.Istate == null)
            {
                return ZSTREAMERROR;
            }

            var ret = this.Istate.InflateEnd(this);
            this.Istate = null;
            return ret;
        }

        public int InflateSync() => this.Istate == null ? ZSTREAMERROR : this.Istate.InflateSync(this);

        public int InflateSetDictionary(byte[] dictionary, int dictLength) => this.Istate == null ? ZSTREAMERROR : this.Istate.InflateSetDictionary(this, dictionary, dictLength);

        public int DeflateInit(int level) => this.DeflateInit(level, MAXWBITS);

        public int DeflateInit(int level, int bits)
        {
            this.Dstate = new Deflate();
            return this.Dstate.DeflateInit(this, level, bits);
        }

        public int Deflate(int flush) => this.Dstate == null ? ZSTREAMERROR : this.Dstate.Compress(this, flush);

        public int DeflateEnd()
        {
            if (this.Dstate == null)
            {
                return ZSTREAMERROR;
            }

            var ret = this.Dstate.DeflateEnd();
            this.Dstate = null;
            return ret;
        }

        public int DeflateParams(int level, int strategy) => this.Dstate == null ? ZSTREAMERROR : this.Dstate.DeflateParams(this, level, strategy);

        public int DeflateSetDictionary(byte[] dictionary, int dictLength) => this.Dstate == null ? ZSTREAMERROR : this.Dstate.DeflateSetDictionary(this, dictionary, dictLength);

        public void Free()
        {
            this.NextIn = null;
            this.NextOut = null;
            this.Msg = null;
            this.Adler32 = null;
        }

        // Flush as much pending output as possible. All deflate() output goes
        // through this function so some applications may wish to modify it
        // to avoid allocating a large strm->next_out buffer and copying into it.
        // (See also read_buf()).
        internal void Flush_pending()
        {
            var len = this.Dstate.Pending;

            if (len > this.AvailOut)
            {
                len = this.AvailOut;
            }

            if (len == 0)
            {
                return;
            }

            if (this.Dstate.PendingBuf.Length <= this.Dstate.PendingOut || this.NextOut.Length <= this.NextOutIndex || this.Dstate.PendingBuf.Length < (this.Dstate.PendingOut + len) || this.NextOut.Length < (this.NextOutIndex + len))
            {
                // System.Console.Out.WriteLine(dstate.pending_buf.Length + ", " + dstate.pending_out + ", " + next_out.Length + ", " + next_out_index + ", " + len);
                // System.Console.Out.WriteLine("avail_out=" + avail_out);
            }

            Array.Copy(this.Dstate.PendingBuf, this.Dstate.PendingOut, this.NextOut, this.NextOutIndex, len);

            this.NextOutIndex += len;
            this.Dstate.PendingOut += len;
            this.TotalOut += len;
            this.AvailOut -= len;
            this.Dstate.Pending -= len;
            if (this.Dstate.Pending == 0)
            {
                this.Dstate.PendingOut = 0;
            }
        }

        // Read a new buffer from the current input stream, update the adler32
        // and total number of bytes read.  All deflate() input goes through
        // this function so some applications may wish to modify it to avoid
        // allocating a large strm->next_in buffer and copying from it.
        // (See also flush_pending()).
        internal int Read_buf(byte[] buf, int start, int size)
        {
            var len = this.AvailIn;

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
                this.Adler = this.Adler32.Calculate(this.Adler, this.NextIn, this.NextInIndex, len);
            }

            Array.Copy(this.NextIn, this.NextInIndex, buf, start, len);
            this.NextInIndex += len;
            this.TotalIn += len;
            return len;
        }
    }
}