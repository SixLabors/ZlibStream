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

        /// <summary>
        /// Initializes a new instance of the <see cref="Inflate"/> class.
        /// </summary>
        /// <param name="zStream">The zlib stream.</param>
        /// <param name="windowBits">The window size in bits.</param>
        public Inflate(ZStream zStream, int windowBits)
        {
            // Handle undocumented nowrap option (no zlib header or check)
            if (windowBits < 0)
            {
                windowBits = -windowBits;
                this.NoWrap = true;
            }

            // Set window size
            if (windowBits < 8 || windowBits > 15)
            {
                ThrowHelper.ThrowArgumentRangeException(nameof(windowBits));
            }

            this.WindowBits = windowBits;
            this.Blocks = new InfBlocks(zStream, !this.NoWrap, 1 << windowBits);

            // Reset state
            InflateReset(zStream, this);
        }

        internal int Mode { get; private set; } // current inflate mode

        // mode dependent information
        internal int Method { get; private set; } // if FLAGS, method byte

        // if CHECK, check values to compare
        internal long[] Was { get; private set; } = new long[1]; // computed check value

        internal long Need { get; private set; } // stream check value

        // if BAD, inflateSync's marker bytes count
        internal int Marker { get; private set; }

        // mode independent information
        internal bool NoWrap { get; private set; } // flag for no wrapper

        internal int WindowBits { get; private set; } // log2(window size)  (8..15, defaults to 15)

        internal InfBlocks Blocks { get; private set; } // current inflate_blocks state

        /// <summary>
        /// Resets the inflate state.
        /// </summary>
        /// <param name="zStream">The Zlib stream.</param>
        /// <param name="inflate">The inflate state.</param>
        public static void InflateReset(ZStream zStream, Inflate inflate)
        {
            if (zStream is null || inflate is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(zStream));
            }

            zStream.TotalIn = zStream.TotalOut = 0;
            zStream.Message = null;
            inflate.Mode = inflate.NoWrap ? BLOCKS : METHOD;
            inflate.Blocks.Reset(zStream, null);
        }

        internal static CompressionState Decompress(ZStream zStream, FlushStrategy strategy)
        {
            CompressionState state;
            int b;

            if (zStream == null || zStream.InflateState == null || zStream.NextIn == null)
            {
                return CompressionState.ZSTREAMERROR;
            }

            // f = f == ZlibFlushStrategy.ZFINISH ? ZlibCompressionState.ZBUFERROR : ZlibCompressionState.ZOK;
            state = CompressionState.ZBUFERROR;
            while (true)
            {
                // System.out.println("mode: "+z.istate.mode);
                switch (zStream.InflateState.Mode)
                {
                    case METHOD:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        if (((zStream.InflateState.Method = zStream.NextIn[zStream.NextInIndex++]) & 0xf) != ZDEFLATED)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Message = "unknown compression method";
                            zStream.InflateState.Marker = 5; // can't try inflateSync
                            break;
                        }

                        if ((zStream.InflateState.Method >> 4) + 8 > zStream.InflateState.WindowBits)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Message = "invalid window size";
                            zStream.InflateState.Marker = 5; // can't try inflateSync
                            break;
                        }

                        zStream.InflateState.Mode = FLAG;
                        goto case FLAG;

                    case FLAG:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        b = zStream.NextIn[zStream.NextInIndex++] & 0xff;

                        if ((((zStream.InflateState.Method << 8) + b) % 31) != 0)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Message = "incorrect header check";
                            zStream.InflateState.Marker = 5; // can't try inflateSync
                            break;
                        }

                        if ((b & PRESETDICT) == 0)
                        {
                            zStream.InflateState.Mode = BLOCKS;
                            break;
                        }

                        zStream.InflateState.Mode = DICT4;
                        goto case DICT4;

                    case DICT4:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need = ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 24) & unchecked((int)0xff000000L);
                        zStream.InflateState.Mode = DICT3;
                        goto case DICT3;

                    case DICT3:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 16) & 0xff0000L;
                        zStream.InflateState.Mode = DICT2;
                        goto case DICT2;

                    case DICT2:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 8) & 0xff00L;
                        zStream.InflateState.Mode = DICT1;
                        goto case DICT1;

                    case DICT1:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need += zStream.NextIn[zStream.NextInIndex++] & 0xffL;
                        zStream.Adler = (uint)zStream.InflateState.Need;
                        zStream.InflateState.Mode = DICT0;
                        return CompressionState.ZNEEDDICT;

                    case DICT0:
                        zStream.InflateState.Mode = BAD;
                        zStream.Message = "need dictionary";
                        zStream.InflateState.Marker = 0; // can try inflateSync
                        return CompressionState.ZSTREAMERROR;

                    case BLOCKS:

                        state = zStream.InflateState.Blocks.Proc(zStream, state);
                        if (state == CompressionState.ZDATAERROR)
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.InflateState.Marker = 0; // can try inflateSync
                            break;
                        }

                        if (state == CompressionState.ZOK)
                        {
                            state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;
                        }

                        if (state != CompressionState.ZSTREAMEND)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;
                        zStream.InflateState.Blocks.Reset(zStream, zStream.InflateState.Was);
                        if (zStream.InflateState.NoWrap)
                        {
                            zStream.InflateState.Mode = DONE;
                            break;
                        }

                        zStream.InflateState.Mode = CHECK4;
                        goto case CHECK4;

                    case CHECK4:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need = ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 24) & unchecked((int)0xff000000L);
                        zStream.InflateState.Mode = CHECK3;
                        goto case CHECK3;

                    case CHECK3:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 16) & 0xff0000L;
                        zStream.InflateState.Mode = CHECK2;
                        goto case CHECK2;

                    case CHECK2:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need += ((zStream.NextIn[zStream.NextInIndex++] & 0xff) << 8) & 0xff00L;
                        zStream.InflateState.Mode = CHECK1;
                        goto case CHECK1;

                    case CHECK1:

                        if (zStream.AvailableIn == 0)
                        {
                            return state;
                        }

                        state = strategy == FlushStrategy.Finish ? CompressionState.ZBUFERROR : CompressionState.ZOK;

                        zStream.AvailableIn--;
                        zStream.TotalIn++;
                        zStream.InflateState.Need += zStream.NextIn[zStream.NextInIndex++] & 0xffL;

                        if (((int)zStream.InflateState.Was[0]) != ((int)zStream.InflateState.Need))
                        {
                            zStream.InflateState.Mode = BAD;
                            zStream.Message = "incorrect data check";
                            zStream.InflateState.Marker = 5; // can't try inflateSync
                            break;
                        }

                        zStream.InflateState.Mode = DONE;
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
            if (z == null || z.InflateState == null || z.InflateState.Mode != DICT0)
            {
                return CompressionState.ZSTREAMERROR;
            }

            if (Adler32.Calculate(dictionary.AsSpan(0, dictLength)) != z.Adler)
            {
                return CompressionState.ZDATAERROR;
            }

            z.Adler = Adler32.SeedValue;

            if (length >= (1 << z.InflateState.WindowBits))
            {
                length = (1 << z.InflateState.WindowBits) - 1;
                index = dictLength - length;
            }

            z.InflateState.Blocks.Set_dictionary(dictionary, index, length);
            z.InflateState.Mode = BLOCKS;
            return CompressionState.ZOK;
        }

        internal static CompressionState InflateSync(ZStream zStream)
        {
            int n; // number of bytes to look at
            int p; // pointer to bytes
            int m; // number of marker bytes found in a row
            long r, w; // temporaries to save total_in and total_out

            // set up
            if (zStream == null || zStream.InflateState == null)
            {
                return CompressionState.ZSTREAMERROR;
            }

            if (zStream.InflateState.Mode != BAD)
            {
                zStream.InflateState.Mode = BAD;
                zStream.InflateState.Marker = 0;
            }

            if ((n = zStream.AvailableIn) == 0)
            {
                return CompressionState.ZBUFERROR;
            }

            p = zStream.NextInIndex;
            m = zStream.InflateState.Marker;

            // search
            while (n != 0 && m < 4)
            {
                if (zStream.NextIn[p] == Mark[m])
                {
                    m++;
                }
                else
                {
                    m = zStream.NextIn[p] != 0 ? 0 : 4 - m;
                }

                p++;
                n--;
            }

            // restore
            zStream.TotalIn += p - zStream.NextInIndex;
            zStream.NextInIndex = p;
            zStream.AvailableIn = n;
            zStream.InflateState.Marker = m;

            // return no joy or set up to restart on a new block
            if (m != 4)
            {
                return CompressionState.ZDATAERROR;
            }

            r = zStream.TotalIn;
            w = zStream.TotalOut;
            InflateReset(zStream, zStream.InflateState);
            zStream.TotalIn = r;
            zStream.TotalOut = w;
            zStream.InflateState.Mode = BLOCKS;
            return CompressionState.ZOK;
        }

        // Returns true if inflate is currently at the end of a block generated
        // by Z_SYNC_FLUSH or Z_FULL_FLUSH. This function is used by one PPP
        // implementation to provide an additional safety check. PPP uses Z_SYNC_FLUSH
        // but removes the length bytes of the resulting empty stored block. When
        // decompressing, PPP checks that at the end of input packet, inflate is
        // waiting for these length bytes.
        internal static CompressionState InflateSyncPoint(ZStream z)
            => (z == null || z.InflateState == null || z.InflateState.Blocks == null)
            ? CompressionState.ZSTREAMERROR
            : z.InflateState.Blocks.Sync_point();

        // TODO: Blocks should be IDisposable
        internal CompressionState InflateEnd(ZStream zStream)
        {
            this.Blocks?.Free(zStream);
            this.Blocks = null;

            // ZFREE(z, z->state);
            return CompressionState.ZOK;
        }
    }
}
