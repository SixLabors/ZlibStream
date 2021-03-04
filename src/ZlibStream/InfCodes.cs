// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.ZlibStream
{
    internal sealed class InfCodes
    {
        // waiting for "i:"=input,
        //             "o:"=output,
        //             "x:"=nothing
        private const int START = 0; // x: set up for LEN
        private const int LEN = 1; // i: get length/literal/eob next
        private const int LENEXT = 2; // i: getting length extra (have base)
        private const int DIST = 3; // i: get distance next
        private const int DISTEXT = 4; // i: getting distance extra
        private const int COPY = 5; // o: copying bytes in window, waiting for space
        private const int LIT = 6; // o: got literal, waiting for output space
        private const int WASH = 7; // o: got eob, possibly still output waiting
        private const int END = 8; // x: got eob and all data flushed
        private const int BADCODE = 9; // x: got error

        private static readonly int[] InflateMask = new int[]
        {
            0b0,
            0b1,
            0b11,
            0b111,
            0b1111,
            0b11111,
            0b111111,
            0b1111111,
            0b11111111,
            0b111111111,
            0b1111111111,
            0b11111111111,
            0b111111111111,
            0b1111111111111,
            0b11111111111111,
            0b111111111111111,
            0b1111111111111111,
        };

        internal InfCodes(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index)
        {
            this.Mode = START;
            this.Lbits = (byte)bl;
            this.Dbits = (byte)bd;
            this.Ltree = tl;
            this.LtreeIndex = tl_index;
            this.Dtree = td;
            this.DtreeIndex = td_index;
        }

        internal InfCodes(int bl, int bd, int[] tl, int[] td)
        {
            this.Mode = START;
            this.Lbits = (byte)bl;
            this.Dbits = (byte)bd;
            this.Ltree = tl;
            this.LtreeIndex = 0;
            this.Dtree = td;
            this.DtreeIndex = 0;
        }

        internal int Mode { get; private set; } // current inflate_codes mode

        // mode dependent information
        internal int Len { get; private set; }

        internal int[] Tree { get; private set; } // pointer into tree

        internal int TreeIndex { get; private set; } = 0;

        internal int Need { get; private set; } // bits needed

        internal int Lit { get; private set; }

        // if EXT or COPY, where and how much
        internal int GetRenamed { get; private set; } // bits to get for extra

        internal int Dist { get; private set; } // distance back to copy from

        internal byte Lbits { get; private set; } // ltree bits decoded per branch

        internal byte Dbits { get; private set; } // dtree bits decoder per branch

        internal int[] Ltree { get; private set; } // literal/length/eob tree

        internal int LtreeIndex { get; private set; } // literal/length/eob tree

        internal int[] Dtree { get; private set; } // distance tree

        internal int DtreeIndex { get; private set; } // distance tree

        internal static void Free()
        {
            // ZFREE(z, c);
        }

        // Called with number of bytes left to write in window at least 258
        // (the maximum string length) and number of input bytes available
        // at least ten.  The ten bytes are six bytes for the longest length/
        // distance pair plus four bytes for overloading the bit buffer.
        internal static CompressionState Inflate_fast(int bl, int bd, int[] tl, int tl_index, int[] td, int td_index, InflateBlocks blocks, ZLibStream zStream)
        {
            int t; // temporary pointer
            int[] tp; // temporary pointer
            int tp_index; // temporary pointer
            int e; // extra bits or operation
            int b; // bit buffer
            int k; // bits in bit buffer
            int p; // input data pointer
            int n; // bytes available there
            int q; // output window write pointer
            int m; // bytes to end of window or read pointer
            int ml; // mask for literal/length tree
            int md; // mask for distance tree
            int c; // bytes to copy
            int d; // distance back to copy from
            int r; // copy source pointer

            // load input, output, bit values
            p = zStream.NextInIndex;
            n = zStream.AvailableIn;
            b = blocks.Bitb;
            k = blocks.Bitk;
            q = blocks.Write;
            m = q < blocks.Read ? blocks.Read - q - 1 : blocks.End - q;

            // initialize masks
            ml = InflateMask[bl];
            md = InflateMask[bd];

            // do until not enough input or output space for fast loop
            do
            {
                // assume called with m >= 258 && n >= 10
                // get literal/length code
                while (k < 20)
                {
                    // max bits for literal/length code
                    n--;
                    b |= (zStream.NextIn[p++] & 0xff) << k;
                    k += 8;
                }

                t = b & ml;
                tp = tl;
                tp_index = tl_index;
                if ((e = tp[(tp_index + t) * 3]) == 0)
                {
                    b >>= tp[((tp_index + t) * 3) + 1];
                    k -= tp[((tp_index + t) * 3) + 1];

                    blocks.Window[q++] = (byte)tp[((tp_index + t) * 3) + 2];
                    m--;
                    continue;
                }

                do
                {
                    b >>= tp[((tp_index + t) * 3) + 1];
                    k -= tp[((tp_index + t) * 3) + 1];

                    if ((e & 16) != 0)
                    {
                        e &= 15;
                        c = tp[((tp_index + t) * 3) + 2] + (b & InflateMask[e]);

                        b >>= e;
                        k -= e;

                        // decode distance base of block to copy
                        while (k < 15)
                        {
                            // max bits for distance code
                            n--;
                            b |= (zStream.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        t = b & md;
                        tp = td;
                        tp_index = td_index;
                        e = tp[(tp_index + t) * 3];

                        do
                        {
                            b >>= tp[((tp_index + t) * 3) + 1];
                            k -= tp[((tp_index + t) * 3) + 1];

                            if ((e & 16) != 0)
                            {
                                // get extra bits to add to distance base
                                e &= 15;
                                while (k < e)
                                {
                                    // get extra bits (up to 13)
                                    n--;
                                    b |= (zStream.NextIn[p++] & 0xff) << k;
                                    k += 8;
                                }

                                d = tp[((tp_index + t) * 3) + 2] + (b & InflateMask[e]);

                                b >>= e;
                                k -= e;

                                // do the copy
                                m -= c;
                                if (q >= d)
                                {
                                    // offset before dest
                                    //  just copy
                                    r = q - d;
                                    if (q - r > 0 && (q - r) < 2)
                                    {
                                        blocks.Window[q++] = blocks.Window[r++];
                                        c--; // minimum count is three,
                                        blocks.Window[q++] = blocks.Window[r++];
                                        c--; // so unroll loop a little
                                    }
                                    else
                                    {
                                        Buffer.BlockCopy(blocks.Window, r, blocks.Window, q, 2);
                                        q += 2;
                                        r += 2;
                                        c -= 2;
                                    }
                                }
                                else
                                {
                                    // else offset after destination
                                    r = q - d;
                                    do
                                    {
                                        r += blocks.End; // force pointer in window
                                    }
                                    while (r < 0); // covers invalid distances
                                    e = blocks.End - r;
                                    if (c > e)
                                    {
                                        // if source crosses,
                                        c -= e; // wrapped copy
                                        if (q - r > 0 && e > (q - r))
                                        {
                                            do
                                            {
                                                blocks.Window[q++] = blocks.Window[r++];
                                            }
                                            while (--e != 0);
                                        }
                                        else
                                        {
                                            Buffer.BlockCopy(blocks.Window, r, blocks.Window, q, e);
                                            q += e;
                                            r += e;
                                            e = 0;
                                        }

                                        r = 0; // copy rest from start of window
                                    }
                                }

                                // copy all or what's left
                                if (q - r > 0 && c > (q - r))
                                {
                                    do
                                    {
                                        blocks.Window[q++] = blocks.Window[r++];
                                    }
                                    while (--c != 0);
                                }
                                else
                                {
                                    Buffer.BlockCopy(blocks.Window, r, blocks.Window, q, c);
                                    q += c;
                                    r += c;
                                    c = 0;
                                }

                                break;
                            }
                            else if ((e & 64) == 0)
                            {
                                t += tp[((tp_index + t) * 3) + 2];
                                t += b & InflateMask[e];
                                e = tp[(tp_index + t) * 3];
                            }
                            else
                            {
                                zStream.Message = "invalid distance code";

                                c = zStream.AvailableIn - n;
                                c = (k >> 3) < c ? k >> 3 : c;
                                n += c;
                                p -= c;
                                k -= c << 3;

                                blocks.Bitb = b;
                                blocks.Bitk = k;
                                zStream.AvailableIn = n;
                                zStream.TotalIn += p - zStream.NextInIndex;
                                zStream.NextInIndex = p;
                                blocks.Write = q;

                                return CompressionState.ZDATAERROR;
                            }
                        }
                        while (true);
                        break;
                    }

                    if ((e & 64) == 0)
                    {
                        t += tp[((tp_index + t) * 3) + 2];
                        t += b & InflateMask[e];
                        if ((e = tp[(tp_index + t) * 3]) == 0)
                        {
                            b >>= tp[((tp_index + t) * 3) + 1];
                            k -= tp[((tp_index + t) * 3) + 1];

                            blocks.Window[q++] = (byte)tp[((tp_index + t) * 3) + 2];
                            m--;
                            break;
                        }
                    }
                    else if ((e & 32) != 0)
                    {
                        c = zStream.AvailableIn - n;
                        c = (k >> 3) < c ? k >> 3 : c;
                        n += c;
                        p -= c;
                        k -= c << 3;

                        blocks.Bitb = b;
                        blocks.Bitk = k;
                        zStream.AvailableIn = n;
                        zStream.TotalIn += p - zStream.NextInIndex;
                        zStream.NextInIndex = p;
                        blocks.Write = q;

                        return CompressionState.ZSTREAMEND;
                    }
                    else
                    {
                        zStream.Message = "invalid literal/length code";

                        c = zStream.AvailableIn - n;
                        c = (k >> 3) < c ? k >> 3 : c;
                        n += c;
                        p -= c;
                        k -= c << 3;

                        blocks.Bitb = b;
                        blocks.Bitk = k;
                        zStream.AvailableIn = n;
                        zStream.TotalIn += p - zStream.NextInIndex;
                        zStream.NextInIndex = p;
                        blocks.Write = q;

                        return CompressionState.ZDATAERROR;
                    }
                }
                while (true);
            }
            while (m >= 258 && n >= 10);

            // not enough input or output--restore pointers and return
            c = zStream.AvailableIn - n;
            c = (k >> 3) < c ? k >> 3 : c;
            n += c;
            p -= c;
            k -= c << 3;

            blocks.Bitb = b;
            blocks.Bitk = k;
            zStream.AvailableIn = n;
            zStream.TotalIn += p - zStream.NextInIndex;
            zStream.NextInIndex = p;
            blocks.Write = q;

            return CompressionState.ZOK;
        }

        internal CompressionState Process(InflateBlocks s, ZLibStream zStream, CompressionState state)
        {
            int j; // temporary storage

            // int[] t; // temporary pointer
            int tindex; // temporary pointer
            int e; // extra bits or operation
            int b; // bit buffer
            int k; // bits in bit buffer
            int p; // input data pointer
            int n; // bytes available there
            int q; // output window write pointer
            int m; // bytes to end of window or read pointer
            int f; // pointer to copy strings from

            // copy input/output information to locals (UPDATE macro restores)
            p = zStream.NextInIndex;
            n = zStream.AvailableIn;
            b = s.Bitb;
            k = s.Bitk;
            q = s.Write;
            m = q < s.Read ? s.Read - q - 1 : s.End - q;

            // process input and output based on current state
            while (true)
            {
                switch (this.Mode)
                {
                    // waiting for "i:"=input, "o:"=output, "x:"=nothing
                    case START: // x: set up for LEN
                        if (m >= 258 && n >= 10)
                        {
                            s.Bitb = b;
                            s.Bitk = k;
                            zStream.AvailableIn = n;
                            zStream.TotalIn += p - zStream.NextInIndex;
                            zStream.NextInIndex = p;
                            s.Write = q;
                            state = Inflate_fast(this.Lbits, this.Dbits, this.Ltree, this.LtreeIndex, this.Dtree, this.DtreeIndex, s, zStream);

                            p = zStream.NextInIndex;
                            n = zStream.AvailableIn;
                            b = s.Bitb;
                            k = s.Bitk;
                            q = s.Write;
                            m = q < s.Read ? s.Read - q - 1 : s.End - q;

                            if (state != CompressionState.ZOK)
                            {
                                this.Mode = state == CompressionState.ZSTREAMEND ? WASH : BADCODE;
                                break;
                            }
                        }

                        this.Need = this.Lbits;
                        this.Tree = this.Ltree;
                        this.TreeIndex = this.LtreeIndex;

                        this.Mode = LEN;
                        goto case LEN;

                    case LEN: // i: get length/literal/eob next
                        j = this.Need;

                        while (k < j)
                        {
                            if (n != 0)
                            {
                                state = CompressionState.ZOK;
                            }
                            else
                            {
                                s.Bitb = b;
                                s.Bitk = k;
                                zStream.AvailableIn = n;
                                zStream.TotalIn += p - zStream.NextInIndex;
                                zStream.NextInIndex = p;
                                s.Write = q;
                                return s.Inflate_flush(zStream, state);
                            }

                            n--;
                            b |= (zStream.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        tindex = (this.TreeIndex + (b & InflateMask[j])) * 3;

                        b >>= this.Tree[tindex + 1];
                        k -= this.Tree[tindex + 1];

                        e = this.Tree[tindex];

                        if (e == 0)
                        {
                            // literal
                            this.Lit = this.Tree[tindex + 2];
                            this.Mode = LIT;
                            break;
                        }

                        if ((e & 16) != 0)
                        {
                            // length
                            this.GetRenamed = e & 15;
                            this.Len = this.Tree[tindex + 2];
                            this.Mode = LENEXT;
                            break;
                        }

                        if ((e & 64) == 0)
                        {
                            // next table
                            this.Need = e;
                            this.TreeIndex = (tindex / 3) + this.Tree[tindex + 2];
                            break;
                        }

                        if ((e & 32) != 0)
                        {
                            // end of block
                            this.Mode = WASH;
                            break;
                        }

                        this.Mode = BADCODE; // invalid code
                        zStream.Message = "invalid literal/length code";
                        state = CompressionState.ZDATAERROR;

                        s.Bitb = b;
                        s.Bitk = k;
                        zStream.AvailableIn = n;
                        zStream.TotalIn += p - zStream.NextInIndex;
                        zStream.NextInIndex = p;
                        s.Write = q;
                        return s.Inflate_flush(zStream, state);

                    case LENEXT: // i: getting length extra (have base)
                        j = this.GetRenamed;

                        while (k < j)
                        {
                            if (n != 0)
                            {
                                state = CompressionState.ZOK;
                            }
                            else
                            {
                                s.Bitb = b;
                                s.Bitk = k;
                                zStream.AvailableIn = n;
                                zStream.TotalIn += p - zStream.NextInIndex;
                                zStream.NextInIndex = p;
                                s.Write = q;
                                return s.Inflate_flush(zStream, state);
                            }

                            n--;
                            b |= (zStream.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        this.Len += b & InflateMask[j];

                        b >>= j;
                        k -= j;

                        this.Need = this.Dbits;
                        this.Tree = this.Dtree;
                        this.TreeIndex = this.DtreeIndex;
                        this.Mode = DIST;
                        goto case DIST;

                    case DIST: // i: get distance next
                        j = this.Need;

                        while (k < j)
                        {
                            if (n != 0)
                            {
                                state = CompressionState.ZOK;
                            }
                            else
                            {
                                s.Bitb = b;
                                s.Bitk = k;
                                zStream.AvailableIn = n;
                                zStream.TotalIn += p - zStream.NextInIndex;
                                zStream.NextInIndex = p;
                                s.Write = q;
                                return s.Inflate_flush(zStream, state);
                            }

                            n--;
                            b |= (zStream.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        tindex = (this.TreeIndex + (b & InflateMask[j])) * 3;

                        b >>= this.Tree[tindex + 1];
                        k -= this.Tree[tindex + 1];

                        e = this.Tree[tindex];
                        if ((e & 16) != 0)
                        {
                            // distance
                            this.GetRenamed = e & 15;
                            this.Dist = this.Tree[tindex + 2];
                            this.Mode = DISTEXT;
                            break;
                        }

                        if ((e & 64) == 0)
                        {
                            // next table
                            this.Need = e;
                            this.TreeIndex = (tindex / 3) + this.Tree[tindex + 2];
                            break;
                        }

                        this.Mode = BADCODE; // invalid code
                        zStream.Message = "invalid distance code";
                        state = CompressionState.ZDATAERROR;

                        s.Bitb = b;
                        s.Bitk = k;
                        zStream.AvailableIn = n;
                        zStream.TotalIn += p - zStream.NextInIndex;
                        zStream.NextInIndex = p;
                        s.Write = q;
                        return s.Inflate_flush(zStream, state);

                    case DISTEXT: // i: getting distance extra
                        j = this.GetRenamed;

                        while (k < j)
                        {
                            if (n != 0)
                            {
                                state = CompressionState.ZOK;
                            }
                            else
                            {
                                s.Bitb = b;
                                s.Bitk = k;
                                zStream.AvailableIn = n;
                                zStream.TotalIn += p - zStream.NextInIndex;
                                zStream.NextInIndex = p;
                                s.Write = q;
                                return s.Inflate_flush(zStream, state);
                            }

                            n--;
                            b |= (zStream.NextIn[p++] & 0xff) << k;
                            k += 8;
                        }

                        this.Dist += b & InflateMask[j];

                        b >>= j;
                        k -= j;

                        this.Mode = COPY;
                        goto case COPY;

                    case COPY: // o: copying bytes in window, waiting for space
                        f = q - this.Dist;
                        while (f < 0)
                        {
                            // modulo window size-"while" instead
                            f += s.End; // of "if" handles invalid distances
                        }

                        while (this.Len != 0)
                        {
                            if (m == 0)
                            {
                                if (q == s.End && s.Read != 0)
                                {
                                    q = 0;
                                    m = q < s.Read ? s.Read - q - 1 : s.End - q;
                                }

                                if (m == 0)
                                {
                                    s.Write = q;
                                    state = s.Inflate_flush(zStream, state);
                                    q = s.Write;
                                    m = q < s.Read ? s.Read - q - 1 : s.End - q;

                                    if (q == s.End && s.Read != 0)
                                    {
                                        q = 0;
                                        m = q < s.Read ? s.Read - q - 1 : s.End - q;
                                    }

                                    if (m == 0)
                                    {
                                        s.Bitb = b;
                                        s.Bitk = k;
                                        zStream.AvailableIn = n;
                                        zStream.TotalIn += p - zStream.NextInIndex;
                                        zStream.NextInIndex = p;
                                        s.Write = q;
                                        return s.Inflate_flush(zStream, state);
                                    }
                                }
                            }

                            s.Window[q++] = s.Window[f++];
                            m--;

                            if (f == s.End)
                            {
                                f = 0;
                            }

                            this.Len--;
                        }

                        this.Mode = START;
                        break;

                    case LIT: // o: got literal, waiting for output space
                        if (m == 0)
                        {
                            if (q == s.End && s.Read != 0)
                            {
                                q = 0;
                                m = q < s.Read ? s.Read - q - 1 : s.End - q;
                            }

                            if (m == 0)
                            {
                                s.Write = q;
                                state = s.Inflate_flush(zStream, state);
                                q = s.Write;
                                m = q < s.Read ? s.Read - q - 1 : s.End - q;

                                if (q == s.End && s.Read != 0)
                                {
                                    q = 0;
                                    m = q < s.Read ? s.Read - q - 1 : s.End - q;
                                }

                                if (m == 0)
                                {
                                    s.Bitb = b;
                                    s.Bitk = k;
                                    zStream.AvailableIn = n;
                                    zStream.TotalIn += p - zStream.NextInIndex;
                                    zStream.NextInIndex = p;
                                    s.Write = q;
                                    return s.Inflate_flush(zStream, state);
                                }
                            }
                        }

                        state = CompressionState.ZOK;

                        s.Window[q++] = (byte)this.Lit;
                        m--;

                        this.Mode = START;
                        break;

                    case WASH: // o: got eob, possibly more output
                        if (k > 7)
                        {
                            // return unused byte, if any
                            k -= 8;
                            n++;
                            p--; // can always return one
                        }

                        s.Write = q;
                        state = s.Inflate_flush(zStream, state);
                        q = s.Write;
                        m = q < s.Read ? s.Read - q - 1 : s.End - q;

                        if (s.Read != s.Write)
                        {
                            s.Bitb = b;
                            s.Bitk = k;
                            zStream.AvailableIn = n;
                            zStream.TotalIn += p - zStream.NextInIndex;
                            zStream.NextInIndex = p;
                            s.Write = q;
                            return s.Inflate_flush(zStream, state);
                        }

                        this.Mode = END;
                        goto case END;

                    case END:
                        state = CompressionState.ZSTREAMEND;
                        s.Bitb = b;
                        s.Bitk = k;
                        zStream.AvailableIn = n;
                        zStream.TotalIn += p - zStream.NextInIndex;
                        zStream.NextInIndex = p;
                        s.Write = q;
                        return s.Inflate_flush(zStream, state);

                    case BADCODE: // x: got error

                        state = CompressionState.ZDATAERROR;

                        s.Bitb = b;
                        s.Bitk = k;
                        zStream.AvailableIn = n;
                        zStream.TotalIn += p - zStream.NextInIndex;
                        zStream.NextInIndex = p;
                        s.Write = q;
                        return s.Inflate_flush(zStream, state);

                    default:
                        state = CompressionState.ZSTREAMERROR;

                        s.Bitb = b;
                        s.Bitk = k;
                        zStream.AvailableIn = n;
                        zStream.TotalIn += p - zStream.NextInIndex;
                        zStream.NextInIndex = p;
                        s.Write = q;
                        return s.Inflate_flush(zStream, state);
                }
            }
        }
    }
}
