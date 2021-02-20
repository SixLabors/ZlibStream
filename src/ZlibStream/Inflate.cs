// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Class for decompressing data through zlib.
    /// </summary>
    internal sealed class Inflate
    {
        // preset dictionary flag in zlib header
        private const int PRESETDICT = 0x20;
        private const int ZDEFLATED = 8;

        private const int METHOD = 0; // waiting for method byte
        private const int FLAG = 1; // waiting for flag byte
        private const int DICT4 = 2; // four dictionary check bytes to go
        private const int DICT3 = 3; // three dictionary check bytes to go
        private const int DICT2 = 4; // two dictionary check bytes to go
        private const int DICT1 = 5; // one dictionary check byte to go
        private const int DICT0 = 6; // waiting for inflateSetDictionary
        private const int BLOCKS = 7; // decompressing blocks
        private const int CHECK4 = 8; // four check bytes to go
        private const int CHECK3 = 9; // three check bytes to go
        private const int CHECK2 = 10; // two check bytes to go
        private const int CHECK1 = 11; // one check byte to go
        private const int DONE = 12; // finished check, done
        private const int BAD = 13; // got an error--stay here

        private static readonly byte[] Mark = new byte[] { 0, 0, 0xFF, 0xFF };

        internal int Mode { get; private set; } // current inflate mode

        // mode dependent information
        internal int Method { get; private set; } // if FLAGS, method byte

        // if CHECK, check values to compare
        internal long[] Was { get; private set; } = new long[1]; // computed check value

        internal long Need { get; private set; } // stream check value

        // if BAD, inflateSync's marker bytes count
        internal int Marker { get; private set; }

        // mode independent information
        internal int Nowrap { get; private set; } // flag for no wrapper

        internal int Wbits { get; private set; } // log2(window size)  (8..15, defaults to 15)

        internal InfBlocks Blocks { get; private set; } // current inflate_blocks state

        /// <summary>
        /// Resets the inflate state.
        /// </summary>
        /// <param name="z">The Z stream.</param>
        /// <returns>The zlib state.</returns>
        public static CompressionState InflateReset(ZStream z)
        {
            if (z == null || z.Istate == null)
            {
                return CompressionState.ZSTREAMERROR;
            }

            z.TotalIn = z.TotalOut = 0;
            z.Msg = null;
            z.Istate.Mode = z.Istate.Nowrap != 0 ? BLOCKS : METHOD;
            z.Istate.Blocks.Reset(z, null);
            return CompressionState.ZOK;
        }

        internal static CompressionState Decompress(ZStream z, FlushStrategy f)
        {
            CompressionState r;
            int b;

            if (z == null || z.Istate == null || z.INextIn == null)
            {
                return CompressionState.ZSTREAMERROR;
            }

            // f = f == ZlibFlushStrategy.ZFINISH ? ZlibCompressionState.ZBUFERROR : ZlibCompressionState.ZOK;
            r = CompressionState.ZBUFERROR;
            while (true)
            {
                // System.out.println("mode: "+z.istate.mode);
                switch (z.Istate.Mode)
                {
                    case METHOD:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        if (((z.Istate.Method = z.INextIn[z.NextInIndex++]) & 0xf) != ZDEFLATED)
                        {
                            z.Istate.Mode = BAD;
                            z.Msg = "unknown compression method";
                            z.Istate.Marker = 5; // can't try inflateSync
                            break;
                        }

                        if ((z.Istate.Method >> 4) + 8 > z.Istate.Wbits)
                        {
                            z.Istate.Mode = BAD;
                            z.Msg = "invalid window size";
                            z.Istate.Marker = 5; // can't try inflateSync
                            break;
                        }

                        z.Istate.Mode = FLAG;
                        goto case FLAG;

                    case FLAG:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        b = z.INextIn[z.NextInIndex++] & 0xff;

                        if ((((z.Istate.Method << 8) + b) % 31) != 0)
                        {
                            z.Istate.Mode = BAD;
                            z.Msg = "incorrect header check";
                            z.Istate.Marker = 5; // can't try inflateSync
                            break;
                        }

                        if ((b & PRESETDICT) == 0)
                        {
                            z.Istate.Mode = BLOCKS;
                            break;
                        }

                        z.Istate.Mode = DICT4;
                        goto case DICT4;

                    case DICT4:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need = ((z.INextIn[z.NextInIndex++] & 0xff) << 24) & unchecked((int)0xff000000L);
                        z.Istate.Mode = DICT3;
                        goto case DICT3;

                    case DICT3:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need += ((z.INextIn[z.NextInIndex++] & 0xff) << 16) & 0xff0000L;
                        z.Istate.Mode = DICT2;
                        goto case DICT2;

                    case DICT2:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need += ((z.INextIn[z.NextInIndex++] & 0xff) << 8) & 0xff00L;
                        z.Istate.Mode = DICT1;
                        goto case DICT1;

                    case DICT1:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need += z.INextIn[z.NextInIndex++] & 0xffL;
                        z.Adler = (uint)z.Istate.Need;
                        z.Istate.Mode = DICT0;
                        return CompressionState.ZNEEDDICT;

                    case DICT0:
                        z.Istate.Mode = BAD;
                        z.Msg = "need dictionary";
                        z.Istate.Marker = 0; // can try inflateSync
                        return CompressionState.ZSTREAMERROR;

                    case BLOCKS:

                        r = z.Istate.Blocks.Proc(z, r);
                        if (r == CompressionState.ZDATAERROR)
                        {
                            z.Istate.Mode = BAD;
                            z.Istate.Marker = 0; // can try inflateSync
                            break;
                        }

                        if (r == CompressionState.ZOK)
                        {
                            r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;
                        }

                        if (r != CompressionState.ZSTREAMEND)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;
                        z.Istate.Blocks.Reset(z, z.Istate.Was);
                        if (z.Istate.Nowrap != 0)
                        {
                            z.Istate.Mode = DONE;
                            break;
                        }

                        z.Istate.Mode = CHECK4;
                        goto case CHECK4;

                    case CHECK4:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need = ((z.INextIn[z.NextInIndex++] & 0xff) << 24) & unchecked((int)0xff000000L);
                        z.Istate.Mode = CHECK3;
                        goto case CHECK3;

                    case CHECK3:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need += ((z.INextIn[z.NextInIndex++] & 0xff) << 16) & 0xff0000L;
                        z.Istate.Mode = CHECK2;
                        goto case CHECK2;

                    case CHECK2:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need += ((z.INextIn[z.NextInIndex++] & 0xff) << 8) & 0xff00L;
                        z.Istate.Mode = CHECK1;
                        goto case CHECK1;

                    case CHECK1:

                        if (z.AvailIn == 0)
                        {
                            return r;
                        }

                        r = f == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        z.AvailIn--; z.TotalIn++;
                        z.Istate.Need += z.INextIn[z.NextInIndex++] & 0xffL;

                        if (((int)z.Istate.Was[0]) != ((int)z.Istate.Need))
                        {
                            z.Istate.Mode = BAD;
                            z.Msg = "incorrect data check";
                            z.Istate.Marker = 5; // can't try inflateSync
                            break;
                        }

                        z.Istate.Mode = DONE;
                        goto case DONE;

                    case DONE:
                        return CompressionState.ZSTREAMEND;

                    case BAD:
                        return CompressionState.ZDATAERROR;

                    default:
                        return CompressionState.ZSTREAMERROR;
                }
            }
        }

        internal static CompressionState InflateSetDictionary(ZStream z, byte[] dictionary, int dictLength)
        {
            var index = 0;
            var length = dictLength;
            if (z == null || z.Istate == null || z.Istate.Mode != DICT0)
            {
                return CompressionState.ZSTREAMERROR;
            }

            if (Adler32.Calculate(dictionary.AsSpan(0, dictLength)) != z.Adler)
            {
                return CompressionState.ZDATAERROR;
            }

            z.Adler = Adler32.SeedValue;

            if (length >= (1 << z.Istate.Wbits))
            {
                length = (1 << z.Istate.Wbits) - 1;
                index = dictLength - length;
            }

            z.Istate.Blocks.Set_dictionary(dictionary, index, length);
            z.Istate.Mode = BLOCKS;
            return CompressionState.ZOK;
        }

        internal static CompressionState InflateSync(ZStream z)
        {
            int n; // number of bytes to look at
            int p; // pointer to bytes
            int m; // number of marker bytes found in a row
            long r, w; // temporaries to save total_in and total_out

            // set up
            if (z == null || z.Istate == null)
            {
                return CompressionState.ZSTREAMERROR;
            }

            if (z.Istate.Mode != BAD)
            {
                z.Istate.Mode = BAD;
                z.Istate.Marker = 0;
            }

            if ((n = z.AvailIn) == 0)
            {
                return CompressionState.ZBUFERROR;
            }

            p = z.NextInIndex;
            m = z.Istate.Marker;

            // search
            while (n != 0 && m < 4)
            {
                if (z.INextIn[p] == Mark[m])
                {
                    m++;
                }
                else
                {
                    m = z.INextIn[p] != 0 ? 0 : 4 - m;
                }

                p++;
                n--;
            }

            // restore
            z.TotalIn += p - z.NextInIndex;
            z.NextInIndex = p;
            z.AvailIn = n;
            z.Istate.Marker = m;

            // return no joy or set up to restart on a new block
            if (m != 4)
            {
                return CompressionState.ZDATAERROR;
            }

            r = z.TotalIn;
            w = z.TotalOut;
            _ = InflateReset(z);
            z.TotalIn = r;
            z.TotalOut = w;
            z.Istate.Mode = BLOCKS;
            return CompressionState.ZOK;
        }

        // Returns true if inflate is currently at the end of a block generated
        // by Z_SYNC_FLUSH or Z_FULL_FLUSH. This function is used by one PPP
        // implementation to provide an additional safety check. PPP uses Z_SYNC_FLUSH
        // but removes the length bytes of the resulting empty stored block. When
        // decompressing, PPP checks that at the end of input packet, inflate is
        // waiting for these length bytes.
        internal static CompressionState InflateSyncPoint(ZStream z) => z == null || z.Istate == null || z.Istate.Blocks == null ? CompressionState.ZSTREAMERROR : z.Istate.Blocks.Sync_point();

        internal CompressionState InflateEnd(ZStream z)
        {
            if (this.Blocks != null)
            {
                this.Blocks.Free(z);
            }

            this.Blocks = null;

            // ZFREE(z, z->state);
            return CompressionState.ZOK;
        }

        internal CompressionState InflateInit(ZStream z, int w)
        {
            z.Msg = null;
            this.Blocks = null;

            // handle undocumented nowrap option (no zlib header or check)
            this.Nowrap = 0;
            if (w < 0)
            {
                w = -w;
                this.Nowrap = 1;
            }

            // set window size
            if (w < 8 || w > 15)
            {
                _ = this.InflateEnd(z);
                return CompressionState.ZSTREAMERROR;
            }

            this.Wbits = w;

            z.Istate.Blocks = new InfBlocks(z, z.Istate.Nowrap != 0 ? null : this, 1 << w);

            // reset state
            _ = InflateReset(z);
            return CompressionState.ZOK;
        }
    }
}
