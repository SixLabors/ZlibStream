// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

// Uncomment to use DeflateQuick
// TODO: Make an option.
// #define USE_QUICK
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Class for compressing data through zlib.
    /// </summary>
    internal sealed unsafe partial class Deflate
    {
        private const int MAXMEMLEVEL = 9;
        private const int MAXWBITS = 15; // 32K LZ77 window
        private const int DEFMEMLEVEL = 8;
        private const int DEFNOCOMPRESSIONMEMLEVEL = 7;
        private const int STORED = 0;
        private const int FAST = 1;
        private const int SLOW = 2;
        private const int QUICK = 3;

        // block not completed, need more input or more output
        private const int NeedMore = 0;

        // block flush performed
        private const int BlockDone = 1;

        // finish started, need only more output at next deflate
        private const int FinishStarted = 2;

        // finish done, accept no more input or output
        private const int FinishDone = 3;

        // preset dictionary flag in zlib header
        private const int PRESETDICT = 0x20;
        private const int INITSTATE = 42;
        private const int BUSYSTATE = 113;
        private const int FINISHSTATE = 666;

        // The deflate compression method
        private const int ZDEFLATED = 8;

        public const int STOREDBLOCK = 0;
        public const int STATICTREES = 1;
        public const int DYNTREES = 2;

        // The three kinds of block type
        public const int ZBINARY = 0;
        public const int ZTEXT = 1;
        public const int ZASCII = ZTEXT; // for compatibility with 1.2.2 and earlier
        public const int ZUNKNOWN = 2;

        // repeat previous bit length 3-6 times (2 bits of repeat count)
        private const int REP36 = 16;

        // repeat a zero length 3-10 times  (3 bits of repeat count)
        private const int REPZ310 = 17;

        // repeat a zero length 11-138 times  (7 bits of repeat count)
        private const int REPZ11138 = 18;

        private const int MINMATCH = 3;
        private const int MAXMATCH = 258;
        private const int MAXBITS = 15;
        private const int DCODES = 30; // number of distance codes
        private const int BLCODES = 19; // number of codes used to transfer the bit lengths
        private const int LENGTHCODES = 29; // number of length codes, not counting the special END_BLOCK code
        private const int LITERALS = 256; // number of literal bytes 0..255

        private const int ENDBLOCK = 256;
        private const int MINLOOKAHEAD = MAXMATCH + MINMATCH + 1;
        private const int LCODES = LITERALS + 1 + LENGTHCODES;
        private const int HEAPSIZE = (2 * LCODES) + 1; // maximum heap size
        private const int BufSize = 16; // Size of bit buffer in bi_buf

        private static readonly Config[] ConfigTable = new Config[10]
        {
            // good  lazy  nice  chain
            new Config(0, 0, 0, 0, STORED), // 0
#if USE_QUICK
            new Config(4, 4, 8, 4, QUICK),  // 1
#else
            new Config(4, 4, 8, 4, FAST),   // 1
#endif
            new Config(4, 5, 16, 8, FAST),  // 2
            new Config(4, 6, 32, 32, FAST),  // 3

            new Config(4, 4, 16, 16, SLOW),  // 4
            new Config(8, 16, 32, 32, SLOW),  // 5
            new Config(8, 16, 128, 128, SLOW),  // 6
            new Config(8, 32, 128, 256, SLOW),  // 7
            new Config(32, 128, 258, 1024, SLOW),  // 8
            new Config(32, 258, 258, 4096, SLOW),  // 9
        };

        private static readonly string[] ZErrmsg = new string[]
        {
            "need dictionary",
            "stream end",
            string.Empty,
            "file error",
            "stream error",
            "data error",
            "insufficient memory",
            "buffer error",
            "incompatible version",
            string.Empty,
        };

        // State
        private ZStream strm; // pointer back to this zlib stream
        private int status; // as the name implies

        private int pendingBufferSize; // size of pending_buf
        private byte[] pendingBuffer; // output still pending
        private MemoryHandle pendingHandle;
        internal byte* pendingPointer;

        internal byte dataType; // UNKNOWN, BINARY or ASCII
        private byte method; // STORED (for zip only) or DEFLATED
        private FlushStrategy lastFlush; // value of flush param for previous deflate call

        // Used by deflate
        private int wSize; // LZ77 window size (32K by default)
        private int wBits; // log2(w_size)  (8..16)
        private int wMask; // w_size - 1

        // Sliding window. Input bytes are read into the second half of the window,
        // and move to the first half later to keep a dictionary of at least wSize
        // bytes. With this organization, matches are limited to a distance of
        // wSize-MAX_MATCH bytes, but this ensures that IO is always
        // performed with a length multiple of the block size. Also, it limits
        // the window size to 64K, which is quite useful on MSDOS.
        // To do: use the user input buffer as sliding window.
        private byte[] windowBuffer;
        private MemoryHandle windowHandle;
        private byte* windowPointer;

        // Actual size of window: 2*wSize, except when the user input buffer
        // is directly used as sliding window.
        private int windowSize;

        // Link to older string with same hash index. To limit the size of this
        // array to 64K, this link is maintained only for the last 32K strings.
        // An index in this array is thus a window index modulo 32K.
        private ushort[] prevBuffer;
        private MemoryHandle prevHandle;
        private ushort* prevPointer;

        private ushort[] headBuffer; // Heads of the hash chains or NIL.
        private MemoryHandle headHandle;
        private ushort* headPointer;

        private int hashSize; // number of elements in hash table
        private int hashBits; // log2(hashSize)
        private uint hashMask; // hashSize - 1

        // Window position at the beginning of the current output block. Gets
        // negative when the window is moved backwards.
        private int blockStart;

        private int matchLength; // length of best match
        private int prevMatch; // previous match
        private int matchAvailable; // set if previous match exists
        private int strStart; // start of string to insert
        private int matchStart; // start of matching string
        private int lookahead; // number of valid bytes ahead in window

        // Length of the best match at previous step. Matches not greater than this
        // are discarded. This is used in the lazy match evaluation.
        private int prevLength;

        // To speed up deflation, hash chains are never searched beyond this
        // length.  A higher limit improves compression ratio but degrades the speed.
        private int maxChainLength;

        // Attempt to find a better match only when the current match is strictly
        // smaller than this value. This mechanism is used only for compression
        // levels >= 4.
        //
        // Insert new strings in the hash table only if the match length is not
        // greater than this length. This saves time but degrades compression.
        // max_insert_length is used only for compression levels <= 3.
        private int maxLazyMatch;

        internal CompressionLevel level; // compression level (1..9)
        private CompressionStrategy strategy; // favor or force Huffman coding

        // Use a faster search when the previous match is longer than this
        private int goodMatch;

        // Stop searching when current match exceeds this
        private int niceMatch;

        // Number of codes at each bit length for an optimal tree
        private readonly ushort[] blCountBuffer;
        private MemoryHandle blCountHandle;

        // Heap used to build the Huffman trees
        private readonly int[] heapBuffer;
        private MemoryHandle heapHandle;

        // Depth of each subtree used as tie breaker for trees of equal frequency
        private readonly byte[] depthBuffer;
        private MemoryHandle depthHandle;

        internal int matches; // number of string matches in current block

        internal int lastEobLen;  // bit length of EOB code for last block

        // Output buffer. bits are inserted starting at the bottom (least
        // significant bits).
        internal ushort biBuf;

        // Number of valid bits in bi_buf.  All bits above the last valid bit
        // are always zero.
        internal int biValid;

        internal int lBuf; // index for literals or lengths */

        // Size of match buffer for literals/lengths.  There are 4 reasons for
        // limiting lit_bufsize to 64K:
        //   - frequencies can be kept in 16 bit counters
        //   - if compression is not successful for the first block, all input
        //     data is still in the window so we can still emit a stored block even
        //     when input comes from standard input.  (This can also be done for
        //     all blocks if lit_bufsize is not greater than 32K.)
        //   - if compression is not successful for a file smaller than 64K, we can
        //     even emit a stored file instead of a stored block (saving 5 bytes).
        //     This is applicable only for zip (not gzip or zlib).
        //   - creating new Huffman trees less frequently may not provide fast
        //     adaptation to changes in the input data statistics. (Take for
        //     example a binary file with poorly compressible code followed by
        //     a highly compressible string table.) Smaller buffer sizes give
        //     fast adaptation but have of course the overhead of transmitting
        //     trees more frequently.
        //   - I can't count above 4
        private int litBufsize;

        internal int lastLit; // running index in l_buf

        // Buffer for distances. To simplify the code, d_buf and l_buf have
        // the same number of elements. To use different lengths, an extra flag
        // array would be necessary.
        internal int dBuf; // index of pendig_buf

        // Whether or not a block is currently open for the QUICK deflation scheme.
        // This is set to true if there is an active block, or false if the block was just
        // closed.
        internal bool blockOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deflate"/> class.
        /// </summary>
        internal Deflate()
        {
            this.blCountBuffer = ArrayPool<ushort>.Shared.Rent(MAXBITS + 1);
            this.blCountHandle = new Memory<ushort>(this.blCountBuffer).Pin();
            this.BlCountPointer = (ushort*)this.blCountHandle.Pointer;

            this.heapBuffer = ArrayPool<int>.Shared.Rent((2 * LCODES) + 1);
            this.heapHandle = new Memory<int>(this.heapBuffer).Pin();
            this.HeapPointer = (int*)this.heapHandle.Pointer;

            this.depthBuffer = ArrayPool<byte>.Shared.Rent((2 * LCODES) + 1);
            this.depthHandle = new Memory<byte>(this.depthBuffer).Pin();
            this.DepthPointer = (byte*)this.depthHandle.Pointer;
        }

        internal int Pending { get; set; } // nb of bytes in the pending buffer

        internal int Noheader { get; set; } // suppress zlib header and adler32

        // number of codes at each bit length for an optimal tree
        internal ushort* BlCountPointer { get; private set; }

        // heap used to build the Huffman trees
        // The sons of heap[n] are heap[2*n] and heap[2*n+1]. heap[0] is not used.
        // The same heap array is used to build all trees.
        internal int* HeapPointer { get; private set; }

        internal int HeapLen { get; set; } // number of elements in the heap

        internal int HeapMax { get; set; } // element of largest frequency

        // Depth of each subtree used as tie breaker for trees of equal frequency
        internal byte* DepthPointer { get; private set; }

        internal int PendingOut { get; set; } // next pending byte to output to the stream

        internal int OptLen { get; set; } // bit length of current block with optimal trees

        internal int StaticLen { get; set; } // bit length of current block with static trees

        /// <summary>
        /// Gets the huffman tree literal and length tree description.
        /// </summary>
        internal Trees.DynamicTreeDesc DynLTree { get; } = new Trees.DynamicTreeDesc(HEAPSIZE, Trees.StaticLDesc);

        /// <summary>
        /// Gets the huffman tree bit length tree description.
        /// </summary>
        internal Trees.DynamicTreeDesc DynBLTree { get; } = new Trees.DynamicTreeDesc((2 * BLCODES) + 1, Trees.StaticBlDesc);

        /// <summary>
        /// Gets the huffman tree distance tree description.
        /// </summary>
        internal Trees.DynamicTreeDesc DynDTree { get; } = new Trees.DynamicTreeDesc((2 * DCODES) + 1, Trees.StaticDDesc);

        public CompressionState DeflateInit(ZStream strm, CompressionLevel level, int bits)
            => this.DeflateInit2(strm, level, ZDEFLATED, bits, DEFMEMLEVEL, CompressionStrategy.DefaultStrategy);

        public CompressionState DeflateInit2(
            ZStream strm,
            CompressionLevel level,
            int method,
            int windowBits,
            int memLevel,
            CompressionStrategy strategy)
        {
            int noheader = 0;
            strm.Msg = null;

            if (level == CompressionLevel.DefaultCompression)
            {
                level = CompressionLevel.Level6;
            }

            if (level == CompressionLevel.NoCompression)
            {
                memLevel = DEFNOCOMPRESSIONMEMLEVEL;
            }

            if (windowBits < 0)
            {
                // undocumented feature: suppress zlib header
                noheader = 1;
                windowBits = -windowBits;
            }

            if (memLevel < 1
                || memLevel > MAXMEMLEVEL
                || method != ZDEFLATED
                || windowBits < 9
                || windowBits > 15
                || level < CompressionLevel.NoCompression
                || level > CompressionLevel.BestCompression
                || strategy < CompressionStrategy.DefaultStrategy
                || strategy > CompressionStrategy.Rle)
            {
                return CompressionState.ZSTREAMERROR;
            }

#if USE_QUICK
            if (level == ZlibCompressionLevel.ZBESTSPEED)
            {
                windowBits = 13;
            }
#endif

            strm.Dstate = this;

            this.Noheader = noheader;
            this.wBits = windowBits;
            this.wSize = 1 << this.wBits;
            this.wMask = this.wSize - 1;

            this.hashBits = memLevel + 7;
            this.hashSize = 1 << this.hashBits;
            this.hashMask = (uint)this.hashSize - 1;

            this.windowBuffer = ArrayPool<byte>.Shared.Rent(this.wSize * 2);
            this.windowHandle = new Memory<byte>(this.windowBuffer).Pin();
            this.windowPointer = (byte*)this.windowHandle.Pointer;

            this.prevBuffer = ArrayPool<ushort>.Shared.Rent(this.wSize);
            this.prevHandle = new Memory<ushort>(this.prevBuffer).Pin();
            this.prevPointer = (ushort*)this.prevHandle.Pointer;

            this.headBuffer = ArrayPool<ushort>.Shared.Rent(this.hashSize);
            this.headHandle = new Memory<ushort>(this.headBuffer).Pin();
            this.headPointer = (ushort*)this.headHandle.Pointer;

            this.litBufsize = 1 << (memLevel + 6); // 16K elements by default

            // We overlay pending_buf and d_buf+l_buf. This works since the average
            // output size for (length,distance) codes is <= 24 bits.
            this.pendingBufferSize = this.litBufsize * 4;
            this.pendingBuffer = ArrayPool<byte>.Shared.Rent(this.pendingBufferSize);
            this.pendingHandle = new Memory<byte>(this.pendingBuffer).Pin();
            this.pendingPointer = (byte*)this.pendingHandle.Pointer;

            this.dBuf = this.litBufsize;
            this.lBuf = (1 + 2) * this.litBufsize;

            this.level = level;
            this.strategy = strategy;
            this.method = (byte)method;

            return this.DeflateReset(strm);
        }

        public CompressionState DeflateEnd()
        {
            if (this.status != INITSTATE && this.status != BUSYSTATE && this.status != FINISHSTATE)
            {
                return CompressionState.ZSTREAMERROR;
            }

            // Deallocate in reverse order of allocations:
            this.pendingHandle.Dispose();
            ArrayPool<byte>.Shared.Return(this.pendingBuffer);

            this.headHandle.Dispose();
            ArrayPool<ushort>.Shared.Return(this.headBuffer);

            this.prevHandle.Dispose();
            ArrayPool<ushort>.Shared.Return(this.prevBuffer);

            this.windowHandle.Dispose();
            ArrayPool<byte>.Shared.Return(this.windowBuffer);

            this.DynLTree.Dispose();
            this.DynDTree.Dispose();
            this.DynBLTree.Dispose();

            this.depthHandle.Dispose();
            ArrayPool<byte>.Shared.Return(this.depthBuffer);

            this.heapHandle.Dispose();
            ArrayPool<int>.Shared.Return(this.heapBuffer);

            this.blCountHandle.Dispose();
            ArrayPool<ushort>.Shared.Return(this.blCountBuffer);

            // free
            // dstate=null;
            return this.status == BUSYSTATE ? CompressionState.ZDATAERROR : CompressionState.ZOK;
        }

        public CompressionState DeflateParams(ZStream strm, CompressionLevel level, CompressionStrategy strategy)
        {
            CompressionState err = CompressionState.ZOK;

            if (level == CompressionLevel.DefaultCompression)
            {
                level = CompressionLevel.Level6;
            }

            if (level < CompressionLevel.NoCompression
                || level > CompressionLevel.BestCompression
                || strategy < CompressionStrategy.DefaultStrategy
                || strategy > CompressionStrategy.HuffmanOnly)
            {
                return CompressionState.ZSTREAMERROR;
            }

            if (ConfigTable[(int)this.level].Func != ConfigTable[(int)level].Func && strm.TotalIn != 0)
            {
                // Flush the last buffer:
                err = strm.Deflate(FlushStrategy.PartialFlush);
            }

            if (this.level != level)
            {
                this.level = level;
                this.maxLazyMatch = ConfigTable[(int)this.level].MaxLazy;
                this.goodMatch = ConfigTable[(int)this.level].GoodLength;
                this.niceMatch = ConfigTable[(int)this.level].NiceLength;
                this.maxChainLength = ConfigTable[(int)this.level].MaxChain;
            }

            this.strategy = strategy;
            return err;
        }

        public CompressionState DeflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
        {
            int length = dictLength;
            int index = 0;

            if (dictionary == null || this.status != INITSTATE)
            {
                return CompressionState.ZSTREAMERROR;
            }

            strm.Adler = Adler32.Calculate(strm.Adler, dictionary.AsSpan(0, dictLength));

            if (length < MINMATCH)
            {
                return CompressionState.ZOK;
            }

            if (length > this.wSize - MINLOOKAHEAD)
            {
                length = this.wSize - MINLOOKAHEAD;
                index = dictLength - length; // use the tail of the dictionary
            }

            Buffer.BlockCopy(dictionary, index, this.windowBuffer, 0, length);
            this.strStart = length;
            this.blockStart = length;

            // Insert all strings in the hash table (except for the last two bytes).
            // s->lookahead stays null, so s->ins_h will be recomputed at the next
            // call of fill_window.
            byte* window = this.windowPointer;
            ushort* head = this.headPointer;
            ushort* prev = this.prevPointer;

            this.InsertString(prev, head, window, 1);

            for (int n = 0; n <= length - MINMATCH; n++)
            {
                this.InsertString(prev, head, window, n);
            }

            return CompressionState.ZOK;
        }

        public CompressionState Compress(ZStream strm, FlushStrategy flush)
        {
            FlushStrategy old_flush;

            if (flush > FlushStrategy.Finish || flush < 0)
            {
                return CompressionState.ZSTREAMERROR;
            }

            if (strm.INextOut == null
                || (strm.INextIn == null && strm.AvailIn != 0)
                || (this.status == FINISHSTATE && flush != FlushStrategy.Finish))
            {
                strm.Msg = ZErrmsg[CompressionState.ZNEEDDICT - CompressionState.ZSTREAMERROR];
                return CompressionState.ZSTREAMERROR;
            }

            if (strm.AvailOut == 0)
            {
                strm.Msg = ZErrmsg[CompressionState.ZNEEDDICT - CompressionState.ZBUFERROR];
                return CompressionState.ZBUFERROR;
            }

            this.strm = strm; // just in case
            old_flush = this.lastFlush;
            this.lastFlush = flush;

            // Write the zlib header
            if (this.status == INITSTATE)
            {
                int header = (ZDEFLATED + ((this.wBits - 8) << 4)) << 8;
                int level_flags = (((int)this.level - 1) & 0xff) >> 1;

                if (level_flags > 3)
                {
                    level_flags = 3;
                }

                header |= level_flags << 6;
                if (this.strStart != 0)
                {
                    header |= PRESETDICT;
                }

                header += 31 - (header % 31);

                this.status = BUSYSTATE;
                this.PutShortMSB(header);

                // Save the adler32 of the preset dictionary:
                if (this.strStart != 0)
                {
                    this.PutShortMSB((int)strm.Adler >> 16);
                    this.PutShortMSB((int)strm.Adler);
                }

                strm.Adler = Adler32.SeedValue;
            }

            // Flush as much pending output as possible
            if (this.Pending != 0)
            {
                this.Flush_pending(strm);
                if (strm.AvailOut == 0)
                {
                    // System.out.println("  avail_out==0");
                    // Since avail_out is 0, deflate will be called again with
                    // more output space, but possibly with both pending and
                    // avail_in equal to zero. There won't be anything to do,
                    // but this is not an error situation so make sure we
                    // return OK instead of BUF_ERROR at next call of deflate:
                    this.lastFlush = (FlushStrategy)(-1);
                    return CompressionState.ZOK;
                }

                // Make sure there is something to do and avoid duplicate consecutive
                // flushes. For repeated and useless calls with Z_FINISH, we keep
                // returning Z_STREAM_END instead of Z_BUFF_ERROR.
            }
            else if (strm.AvailIn == 0 && flush <= old_flush && flush != FlushStrategy.Finish)
            {
                strm.Msg = ZErrmsg[CompressionState.ZNEEDDICT - CompressionState.ZBUFERROR];
                return CompressionState.ZBUFERROR;
            }

            // User must not provide more input after the first FINISH:
            if (this.status == FINISHSTATE && strm.AvailIn != 0)
            {
                strm.Msg = ZErrmsg[CompressionState.ZNEEDDICT - CompressionState.ZBUFERROR];
                return CompressionState.ZBUFERROR;
            }

            // Start a new block or continue the current one.
            if (strm.AvailIn != 0
                || this.lookahead != 0
                || (flush != FlushStrategy.NoFlush && this.status != FINISHSTATE))
            {
                int bstate = -1;

                if (this.strategy == CompressionStrategy.Rle)
                {
                    bstate = this.DeflateRle(flush);
                }
                else
                {
                    switch (ConfigTable[(int)this.level].Func)
                    {
                        case STORED:
                            bstate = this.DeflateStored(flush);
                            break;

                        case FAST:
                            bstate = this.DeflateFast(flush);
                            break;

                        case SLOW:
                            bstate = this.DeflateSlow(flush);
                            break;

                        case QUICK:
                            bstate = this.DeflateQuick(flush);
                            break;
                    }
                }

                if (bstate == FinishStarted || bstate == FinishDone)
                {
                    this.status = FINISHSTATE;
                }

                if (bstate == NeedMore || bstate == FinishStarted)
                {
                    if (strm.AvailOut == 0)
                    {
                        this.lastFlush = (FlushStrategy)(-1); // avoid BUF_ERROR next call, see above
                    }

                    return CompressionState.ZOK;

                    // If flush != Z_NO_FLUSH && avail_out == 0, the next call
                    // of deflate should use the same flush parameter to make sure
                    // that the flush is complete. So we don't have to output an
                    // empty block here, this will be done at next call. This also
                    // ensures that for a very small output buffer, we emit at most
                    // one empty block.
                }

                if (bstate == BlockDone)
                {
                    if (flush == FlushStrategy.PartialFlush)
                    {
                        Trees.Tr_align(this);
                    }
                    else
                    {
                        // FULL_FLUSH or SYNC_FLUSH
                        Trees.Tr_stored_block(this, 0, 0, false);

                        // For a full flush, this empty block will be recognized
                        // as a special marker by inflate_sync().
                        if (flush == FlushStrategy.FullFlush)
                        {
                            // state.head[s.hash_size-1]=0;
                            ushort* head = this.headPointer;
                            for (int i = 0; i < this.hashSize; i++)
                            {
                                // forget history
                                head[i] = 0;
                            }
                        }
                    }

                    this.Flush_pending(strm);
                    if (strm.AvailOut == 0)
                    {
                        this.lastFlush = (FlushStrategy)(-1); // avoid BUF_ERROR at next call, see above
                        return CompressionState.ZOK;
                    }
                }
            }

            if (flush != FlushStrategy.Finish)
            {
                return CompressionState.ZOK;
            }

            if (this.Noheader != 0)
            {
                return CompressionState.ZSTREAMEND;
            }

            // Write the zlib trailer (adler32)
            this.PutShortMSB((int)strm.Adler >> 16);
            this.PutShortMSB((int)strm.Adler);
            this.Flush_pending(strm);

            // If avail_out is zero, the application will call deflate again
            // to flush the rest.
            this.Noheader = -1; // write the trailer only once!
            return this.Pending != 0 ? CompressionState.ZOK : CompressionState.ZSTREAMEND;
        }

        // Flush the bit buffer, keeping at most 7 bits in it.
        [MethodImpl(InliningOptions.ShortMethod)]
        public void Bi_flush()
        {
            if (this.biValid == 16)
            {
                this.PutShort(this.biBuf);
                this.biBuf = 0;
                this.biValid = 0;
            }
            else if (this.biValid >= 8)
            {
                this.PutByte((byte)this.biBuf);
                this.biBuf = (ushort)(this.biBuf >> 8);
                this.biValid -= 8;
            }
        }

        // Flush the bit buffer and align the output on a byte boundary
        [MethodImpl(InliningOptions.ShortMethod)]
        public void Bi_windup()
        {
            if (this.biValid > 8)
            {
                this.PutShort(this.biBuf);
            }
            else if (this.biValid > 0)
            {
                this.PutByte((byte)this.biBuf);
            }

            this.biBuf = 0;
            this.biValid = 0;
        }

        // Copy a stored block, storing first the length and its
        // one's complement if requested.
        [MethodImpl(InliningOptions.ShortMethod)]
        public void Copy_block(int buf, int len, bool header)
        {
            this.Bi_windup(); // align on byte boundary
            this.lastEobLen = 8; // enough lookahead for inflate

            if (header)
            {
                this.PutShort((ushort)len);
                this.PutShort((ushort)~len);
            }

            this.PutByte(this.windowBuffer, buf, len);
        }

        private void Lm_init()
        {
            this.windowSize = 2 * this.wSize;
            ushort* head = this.headPointer;

            head[this.hashSize - 1] = 0;
            for (int i = 0; i < this.hashSize - 1; i++)
            {
                head[i] = 0;
            }

            // Set the default configuration parameters:
            this.maxLazyMatch = ConfigTable[(int)this.level].MaxLazy;
            this.goodMatch = ConfigTable[(int)this.level].GoodLength;
            this.niceMatch = ConfigTable[(int)this.level].NiceLength;
            this.maxChainLength = ConfigTable[(int)this.level].MaxChain;

            this.strStart = 0;
            this.blockStart = 0;
            this.lookahead = 0;
            this.matchLength = this.prevLength = MINMATCH - 1;
            this.matchAvailable = 0;
        }

        // Output a byte on the stream.
        // IN assertion: there is enough room in pending_buf.
        [MethodImpl(InliningOptions.ShortMethod)]
        private void PutByte(byte[] p, int start, int len)
        {
            Buffer.BlockCopy(p, start, this.pendingBuffer, this.Pending, len);
            this.Pending += len;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void PutByte(byte c) => this.pendingPointer[this.Pending++] = c;

        [MethodImpl(InliningOptions.ShortMethod)]
        private void PutShort(int w)
        {
            *(ushort*)&this.pendingPointer[this.Pending] = (ushort)w;
            this.Pending += 2;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void PutShortMSB(int b)
        {
            this.PutByte((byte)(b >> 8));
            this.PutByte((byte)b);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public void Send_code(int c, Trees.CodeData* tree)
            => this.Send_bits(tree[c].Code, tree[c].Len);

        [MethodImpl(InliningOptions.ShortMethod)]
        public void Send_bits(int value, int length)
        {
            if (this.biValid > BufSize - length)
            {
                this.biBuf |= (ushort)(value << this.biValid);
                this.PutShort(this.biBuf);
                this.biBuf = (ushort)(value >> (BufSize - this.biValid));
                this.biValid += length - BufSize;
            }
            else
            {
                this.biBuf |= (ushort)(value << this.biValid);
                this.biValid += length;
            }
        }

        // Flush as much pending output as possible. All deflate() output goes
        // through this function so some applications may wish to modify it
        // to avoid allocating a large strm->next_out buffer and copying into it.
        // (See also read_buf()).
        [MethodImpl(InliningOptions.ShortMethod)]
        public void Flush_pending(ZStream strm)
        {
            this.Bi_flush();
            int len = this.Pending;

            if (len > strm.AvailOut)
            {
                len = strm.AvailOut;
            }

            if (len == 0)
            {
                return;
            }

            Buffer.BlockCopy(this.pendingBuffer, this.PendingOut, strm.INextOut, strm.NextOutIndex, len);

            strm.NextOutIndex += len;
            this.PendingOut += len;
            strm.TotalOut += len;
            strm.AvailOut -= len;
            this.Pending -= len;
            if (this.Pending == 0)
            {
                this.PendingOut = 0;
            }
        }

        /// <summary>
        /// Insert string str in the dictionary and set match_head to the previous head
        /// of the hash chain(the most recent string with same hash key). Return
        /// the previous length of the hash chain.
        /// IN  assertion: all calls to InsertString are made with consecutive input
        /// characters and the first MINMATCH bytes of str are valid(except for
        /// the last MINMATCH-1 bytes of the input file).
        /// </summary>
        /// <returns>The <see cref="int"/>.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        private int InsertString(ushort* prev, ushort* head, byte* window, int str)
        {
            uint hash = this.UpdateHash(*(uint*)(window + (str + (MINMATCH - 1))));
            ushort cur = head[hash & this.hashMask];
            if (cur != str)
            {
                prev[str & this.wMask] = cur;
                head[hash & this.hashMask] = (ushort)str;
            }

            return cur;
        }

        private CompressionState DeflateReset(ZStream strm)
        {
            strm.TotalIn = strm.TotalOut = 0;
            strm.Msg = null;
            strm.DataType = ZUNKNOWN;

            this.Pending = 0;
            this.PendingOut = 0;

            if (this.Noheader < 0)
            {
                this.Noheader = 0; // was set to -1 by deflate(..., Z_FINISH);
            }

            this.status = (this.Noheader != 0) ? BUSYSTATE : INITSTATE;
            strm.Adler = Adler32.SeedValue;

            this.lastFlush = FlushStrategy.NoFlush;

            Trees.Tr_init(this);
            this.Lm_init();
            return CompressionState.ZOK;
        }

        /// <summary>
        /// Save the match info and tally the frequency counts. Return true if
        /// the current block must be flushed.
        /// </summary>
        /// <param name="dist">The distance of matched string.</param>
        /// <param name="len">The match length-MINMATCH.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        private bool Tr_tally_dist(int dist, int len)
        {
            byte* pending = this.pendingPointer;
            int dbuffindex = this.dBuf + (this.lastLit * 2);

            pending[dbuffindex++] = (byte)(dist >> 8);
            pending[dbuffindex] = (byte)dist;
            pending[this.lBuf + this.lastLit++] = (byte)len;
            this.matches++;

            // Here, lc is the match length - MINMATCH
            dist--; // dist = match distance - 1
            this.DynLTree[Trees.GetLengthCode(len) + LITERALS + 1].Freq++;
            this.DynDTree[Trees.GetDistanceCode(dist)].Freq++;

            return this.lastLit == this.litBufsize - 1;
        }

        /// <summary>
        /// Save the match info and tally the frequency counts. Return true if
        /// the current block must be flushed.
        /// </summary>
        /// <param name="c">The unmatched byte.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        private bool Tr_tally_lit(byte c)
        {
            byte* pending = this.pendingPointer;
            int dbuffindex = this.dBuf + (this.lastLit * 2);

            pending[dbuffindex++] = 0;
            pending[dbuffindex] = 0;
            pending[this.lBuf + this.lastLit++] = c;

            // lc is the unmatched char
            this.DynLTree[c].Freq++;

            return this.lastLit == this.litBufsize - 1;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void Flush_block_only(bool eof)
        {
            Trees.Tr_flush_block(this, this.blockStart >= 0 ? this.blockStart : -1, this.strStart - this.blockStart, eof);
            this.blockStart = this.strStart;
            this.Flush_pending(this.strm);
        }

        // Fill the window when the lookahead becomes insufficient.
        // Updates strstart and lookahead.
        //
        // IN assertion: lookahead < MIN_LOOKAHEAD
        // OUT assertions: strstart <= window_size-MIN_LOOKAHEAD
        //    At least one byte has been read, or avail_in == 0; reads are
        //    performed for at least two bytes (required for the zip translate_eol
        //    option -- not supported here).
        [MethodImpl(InliningOptions.HotPath)]
        private void Fill_window()
        {
            int n;
            int more; // Amount of free space at the end of the window.

            byte* window = this.windowPointer;
            ushort* head = this.headPointer;
            ushort* prev = this.prevPointer;

            do
            {
                more = this.windowSize - this.lookahead - this.strStart;

                if (this.strStart >= this.wSize + this.wSize - MINLOOKAHEAD)
                {
                    Buffer.BlockCopy(this.windowBuffer, this.wSize, this.windowBuffer, 0, this.wSize);
                    this.matchStart -= this.wSize;
                    this.strStart -= this.wSize; // we now have strstart >= MAX_DIST
                    this.blockStart -= this.wSize;

                    this.SlideHash(head, prev);
                    more += this.wSize;
                }

                if (this.strm.AvailIn == 0)
                {
                    return;
                }

                // If there was no sliding:
                //    strstart <= WSIZE+MAX_DIST-1 && lookahead <= MIN_LOOKAHEAD - 1 &&
                //    more == window_size - lookahead - strstart
                // => more >= window_size - (MIN_LOOKAHEAD-1 + WSIZE + MAX_DIST-1)
                // => more >= window_size - 2*WSIZE + 2
                // In the BIG_MEM or MMAP case (not yet supported),
                //   window_size == input_size + MIN_LOOKAHEAD  &&
                //   strstart + s->lookahead <= input_size => more >= MIN_LOOKAHEAD.
                // Otherwise, window_size == 2*WSIZE so more >= 2.
                // If there was sliding, more >= WSIZE. So in all cases, more >= 2.
                n = this.strm.Read_buf(this.windowBuffer, this.strStart + this.lookahead, more);
                this.lookahead += n;

                // Initialize the hash value now that we have some input:
                if (this.lookahead >= MINMATCH)
                {
                    this.InsertString(prev, head, window, this.strStart + 1);
                }

                // If the whole input has less than MINMATCH bytes, ins_h is garbage,
                // but this is not important since only literal bytes will be emitted.
            }
            while (this.lookahead < MINLOOKAHEAD && this.strm.AvailIn != 0);
        }

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private int Longest_match(int cur_match)
        {
            byte* window = this.windowPointer;

            int chain_length = this.maxChainLength; // max hash chain length
            byte* scan = &window[this.strStart]; // current string
            byte* match; // matched string
            int len; // length of current match
            int best_len = this.prevLength; // best match length so far
            int limit = this.strStart > (this.wSize - MINLOOKAHEAD) ? this.strStart - (this.wSize - MINLOOKAHEAD) : 0;
            int nice_match = this.niceMatch;
            int matchStrt = this.matchStart;

            // Stop when cur_match becomes <= limit. To simplify the code,
            // we prevent matches with the string of window index 0.
            int wmask = this.wMask;

            if (best_len == 0)
            {
                best_len = 1;
            }

            ushort scan_start = *(ushort*)scan;
            ushort scan_end = *(ushort*)&scan[best_len - 1];

            // The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple of 16.
            // It is easy to get rid of this optimization if necessary.

            // Do not waste too much time if we already have a good match:
            if (this.prevLength >= this.goodMatch)
            {
                chain_length >>= 2;
            }

            // Do not look for matches beyond the end of the input. This is necessary
            // to make deflate deterministic.
            if (nice_match > this.lookahead)
            {
                nice_match = this.lookahead;
            }

            ushort* prev = this.prevPointer;

            do
            {
                if (cur_match >= this.strStart)
                {
                    break;
                }

                match = &window[cur_match];

                // Skip to next match if the match length cannot increase
                // or if the match length is less than 2:
                if (*(ushort*)&match[best_len - 1] != scan_end
                    || *(ushort*)match != scan_start)
                {
                    continue;
                }

                len = Compare256(scan + 2, match + 2) + 2;

                if (len > best_len)
                {
                    matchStrt = cur_match;
                    best_len = len;
                    if (len >= nice_match)
                    {
                        break;
                    }

                    scan_end = *(ushort*)&scan[best_len - 1];
                }
            }
            while ((cur_match = prev[cur_match & wmask]) > limit && --chain_length != 0);

            this.matchStart = matchStrt;
            return Math.Min(best_len, this.lookahead);
        }

        private struct Config
        {
            // reduce lazy search above this match length
            public int GoodLength;

            // do not perform lazy search above this match length
            public int MaxLazy;

            // quit search above this match length
            public int NiceLength;

            public int MaxChain;

            public int Func;

            public Config(int good_length, int max_lazy, int nice_length, int max_chain, int func)
            {
                this.GoodLength = good_length;
                this.MaxLazy = max_lazy;
                this.NiceLength = nice_length;
                this.MaxChain = max_chain;
                this.Func = func;
            }
        }
    }
}
