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
    using System;
    using System.IO;

    public class ZOutputStream : Stream
    {
        private Stream outRenamed;

        public ZOutputStream(Stream out_Renamed)
            : base()
        {
            this.InitBlock();
            this.outRenamed = out_Renamed;
            this.Z.InflateInit();
            this.Compress = false;
        }

        public ZOutputStream(Stream out_Renamed, int level)
            : base()
        {
            this.InitBlock();
            this.outRenamed = out_Renamed;
            this.Z.DeflateInit(level);
            this.Compress = true;
        }

        public ZStream Z { get; private set; } = new ZStream();

        // UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override bool CanRead => false;

        // UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override bool CanSeek => false;

        // UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override bool CanWrite => true;

        // UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override long Length => 0;

        // UPGRADE_TODO: The following property was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override long Position
        {
            get => 0;

            set
            {
            }
        }

        public virtual int FlushMode { get; set; }

        /// <summary>Gets the total number of bytes input so far.</summary>
        public virtual long TotalIn => this.Z.TotalIn;

        /// <summary>Gets the total number of bytes output so far.</summary>
        public virtual long TotalOut => this.Z.TotalOut;

        protected internal int Bufsize { get; private set; } = 4096;

        protected internal byte[] Buf { get; private set; }

        protected internal byte[] Buf1 { get; private set; } = new byte[1];

        protected internal bool Compress { get; private set; }

        public void WriteByte(int b)
        {
            this.Buf1[0] = (byte)b;
            this.Write(this.Buf1, 0, 1);
        }

        // UPGRADE_TODO: The differences in the Expected value  of parameters for method 'WriteByte'  may cause compilation errors.  'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1092_3"'
        public override void WriteByte(byte b) => this.WriteByte(b);

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

                this.outRenamed.Write(this.Buf, 0, this.Bufsize - this.Z.AvailOut);
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
                    this.outRenamed.Write(this.Buf, 0, this.Bufsize - this.Z.AvailOut);
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

        public virtual void End()
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
                this.End();
                this.outRenamed.Dispose();
                this.outRenamed = null;
            }
        }

        public override void Flush() => this.outRenamed.Flush();

        // UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override int Read(byte[] buffer, int offset, int count) => 0;

        // UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override void SetLength(long value)
        {
        }

        // UPGRADE_TODO: The following method was automatically generated and it must be implemented in order to preserve the class logic. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1232_3"'
        public override long Seek(long offset, SeekOrigin origin) => 0;

        private void InitBlock()
        {
            this.FlushMode = ZlibConst.ZNOFLUSH;
            this.Buf = new byte[this.Bufsize];
        }
    }
}
