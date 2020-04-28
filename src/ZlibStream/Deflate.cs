// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors.ZlibStream
{
    using System;

    /// <summary>
    /// Class for compressing data through zlib.
    /// </summary>
    internal sealed class Deflate
    {
        private const int MAXMEMLEVEL = 9;
        private const int MAXWBITS = 15; // 32K LZ77 window
        private const int DEFMEMLEVEL = 8;
        private const int STORED = 0;
        private const int FAST = 1;
        private const int SLOW = 2;

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

        private const int STOREDBLOCK = 0;
        private const int STATICTREES = 1;
        private const int DYNTREES = 2;

        // The three kinds of block type
        private const int ZBINARY = 0;
        private const int ZASCII = 1;
        private const int ZUNKNOWN = 2;

        private const int BufSize = 8 * 2;

        // repeat previous bit length 3-6 times (2 bits of repeat count)
        private const int REP36 = 16;

        // repeat a zero length 3-10 times  (3 bits of repeat count)
        private const int REPZ310 = 17;

        // repeat a zero length 11-138 times  (7 bits of repeat count)
        private const int REPZ11138 = 18;

        private const int MINMATCH = 3;
        private const int MAXMATCH = 258;
        private const int MAXBITS = 15;
        private const int DCODES = 30;
        private const int BLCODES = 19;
        private const int LENGTHCODES = 29;
        private const int LITERALS = 256;

        private const int ENDBLOCK = 256;
        private const int MINLOOKAHEAD = MAXMATCH + MINMATCH + 1;
        private const int LCODES = LITERALS + 1 + LENGTHCODES;
        private const int HEAPSIZE = (2 * LCODES) + 1;

        private static readonly Config[] ConfigTable = new Config[10]
        {
            // good  lazy  nice  chain
            new Config(0, 0, 0, 0, STORED), // 0
            new Config(4, 4, 8, 4, FAST),  // 1
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
            "need dictionary", "stream end", string.Empty, "file error", "stream error",
            "data error", "insufficient memory", "buffer error", "incompatible version",
            string.Empty,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Deflate"/> class.
        /// </summary>
        internal Deflate()
        {
            this.DynLtree = new short[HEAPSIZE * 2];
            this.DynDtree = new short[((2 * DCODES) + 1) * 2]; // distance tree
            this.BlTree = new short[((2 * BLCODES) + 1) * 2]; // Huffman tree for bit lengths
        }

        internal ZStream Strm { get; private set; } // pointer back to this zlib stream

        internal int Status { get; private set; } // as the name implies

        internal byte[] PendingBuf { get; private set; } // output still pending

        internal int PendingBufSize { get; private set; } // size of pending_buf

        internal int PendingOut { get; set; } // next pending byte to output to the stream

        internal int Pending { get; set; } // nb of bytes in the pending buffer

        internal int Noheader { get; private set; } // suppress zlib header and adler32

        internal byte DataType { get; private set; } // UNKNOWN, BINARY or ASCII

        internal byte Method { get; private set; } // STORED (for zip only) or DEFLATED

        internal ZlibFlushStrategy LastFlush { get; private set; } // value of flush param for previous deflate call

        internal int WSize { get; private set; } // LZ77 window size (32K by default)

        internal int WBits { get; private set; } // log2(w_size)  (8..16)

        internal int WMask { get; private set; } // w_size - 1

        internal byte[] Window { get; private set; }

        // Sliding window. Input bytes are read into the second half of the window,
        // and move to the first half later to keep a dictionary of at least wSize
        // bytes. With this organization, matches are limited to a distance of
        // wSize-MAX_MATCH bytes, but this ensures that IO is always
        // performed with a length multiple of the block size. Also, it limits
        // the window size to 64K, which is quite useful on MSDOS.
        // To do: use the user input buffer as sliding window.
        internal int WindowSize { get; private set; }

        // Actual size of window: 2*wSize, except when the user input buffer
        // is directly used as sliding window.
        internal short[] Prev { get; private set; }

        // Link to older string with same hash index. To limit the size of this
        // array to 64K, this link is maintained only for the last 32K strings.
        // An index in this array is thus a window index modulo 32K.
        internal short[] Head { get; private set; } // Heads of the hash chains or NIL.

        internal int InsH { get; private set; } // hash index of string to be inserted

        internal int HashSize { get; private set; } // number of elements in hash table

        internal int HashBits { get; private set; } // log2(hash_size)

        internal int HashMask { get; private set; } // hash_size-1

        // Number of bits by which ins_h must be shifted at each input
        // step. It must be such that after MIN_MATCH steps, the oldest
        // byte no longer takes part in the hash key, that is:
        // hash_shift * MIN_MATCH >= hash_bits
        internal int HashShift { get; private set; }

        // Window position at the beginning of the current output block. Gets
        // negative when the window is moved backwards.
        internal int BlockStart { get; private set; }

        internal int MatchLength { get; private set; } // length of best match

        internal int PrevMatch { get; private set; } // previous match

        internal int MatchAvailable { get; private set; } // set if previous match exists

        internal int Strstart { get; private set; } // start of string to insert

        internal int MatchStart { get; private set; } // start of matching string

        internal int Lookahead { get; private set; } // number of valid bytes ahead in window

        // Length of the best match at previous step. Matches not greater than this
        // are discarded. This is used in the lazy match evaluation.
        internal int PrevLength { get; private set; }

        // To speed up deflation, hash chains are never searched beyond this
        // length.  A higher limit improves compression ratio but degrades the speed.
        internal int MaxChainLength { get; private set; }

        // Attempt to find a better match only when the current match is strictly
        // smaller than this value. This mechanism is used only for compression
        // levels >= 4.
        internal int MaxLazyMatch { get; private set; }

        // Insert new strings in the hash table only if the match length is not
        // greater than this length. This saves time but degrades compression.
        // max_insert_length is used only for compression levels <= 3.
        internal ZlibCompressionLevel Level { get; private set; } // compression level (1..9)

        internal ZlibCompressionStrategy Strategy { get; private set; } // favor or force Huffman coding

        // Use a faster search when the previous match is longer than this
        internal int GoodMatch { get; private set; }

        // Stop searching when current match exceeds this
        internal int NiceMatch { get; private set; }

        internal short[] DynLtree { get; private set; } // literal and length tree

        internal short[] DynDtree { get; private set; } // distance tree

        internal short[] BlTree { get; private set; } // Huffman tree for bit lengths

        internal Tree LDesc { get; private set; } = new Tree(); // desc for literal tree

        internal Tree DDesc { get; private set; } = new Tree(); // desc for distance tree

        internal Tree BlDesc { get; private set; } = new Tree(); // desc for bit length tree

        // number of codes at each bit length for an optimal tree
        internal short[] BlCount { get; private set; } = new short[MAXBITS + 1];

        // heap used to build the Huffman trees
        internal int[] Heap { get; private set; } = new int[(2 * LCODES) + 1];

        internal int HeapLen { get; set; } // number of elements in the heap

        internal int HeapMax { get; set; } // element of largest frequency

        // The sons of heap[n] are heap[2*n] and heap[2*n+1]. heap[0] is not used.
        // The same heap array is used to build all trees.

        // Depth of each subtree used as tie breaker for trees of equal frequency
        internal byte[] Depth { get; private set; } = new byte[(2 * LCODES) + 1];

        internal int LBuf { get; private set; } // index for literals or lengths */

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
        internal int LitBufsize { get; private set; }

        internal int LastLit { get; private set; } // running index in l_buf

        // Buffer for distances. To simplify the code, d_buf and l_buf have
        // the same number of elements. To use different lengths, an extra flag
        // array would be necessary.
        internal int DBuf { get; private set; } // index of pendig_buf

        internal int OptLen { get; set; } // bit length of current block with optimal trees

        internal int StaticLen { get; set; } // bit length of current block with static trees

        internal int Matches { get; private set; } // number of string matches in current block

        internal int LastEobLen { get; private set; } // bit length of EOB code for last block

        // Output buffer. bits are inserted starting at the bottom (least
        // significant bits).
        internal short BiBuf { get; private set; }

        // Number of valid bits in bi_buf.  All bits above the last valid bit
        // are always zero.
        internal int BiValid { get; private set; }

        internal static bool Smaller(short[] tree, int n, int m, byte[] depth)
            => tree[n * 2] < tree[m * 2] || (tree[n * 2] == tree[m * 2] && depth[n] <= depth[m]);

        internal void Lm_init()
        {
            this.WindowSize = 2 * this.WSize;

            this.Head[this.HashSize - 1] = 0;
            for (var i = 0; i < this.HashSize - 1; i++)
            {
                this.Head[i] = 0;
            }

            // Set the default configuration parameters:
            this.MaxLazyMatch = ConfigTable[(int)this.Level].MaxLazy;
            this.GoodMatch = ConfigTable[(int)this.Level].GoodLength;
            this.NiceMatch = ConfigTable[(int)this.Level].NiceLength;
            this.MaxChainLength = ConfigTable[(int)this.Level].MaxChain;

            this.Strstart = 0;
            this.BlockStart = 0;
            this.Lookahead = 0;
            this.MatchLength = this.PrevLength = MINMATCH - 1;
            this.MatchAvailable = 0;
            this.InsH = 0;
        }

        // Initialize the tree data structures for a new zlib stream.
        internal void Tr_init()
        {
            this.LDesc.DynTree = this.DynLtree;
            this.LDesc.StatDesc = StaticTree.StaticLDesc;

            this.DDesc.DynTree = this.DynDtree;
            this.DDesc.StatDesc = StaticTree.StaticDDesc;

            this.BlDesc.DynTree = this.BlTree;
            this.BlDesc.StatDesc = StaticTree.StaticBlDesc;

            this.BiBuf = 0;
            this.BiValid = 0;
            this.LastEobLen = 8; // enough lookahead for inflate

            // Initialize the first block of the first file:
            this.Init_block();
        }

        internal void Init_block()
        {
            // Initialize the trees.
            for (var i = 0; i < LCODES; i++)
            {
                this.DynLtree[i * 2] = 0;
            }

            for (var i = 0; i < DCODES; i++)
            {
                this.DynDtree[i * 2] = 0;
            }

            for (var i = 0; i < BLCODES; i++)
            {
                this.BlTree[i * 2] = 0;
            }

            this.DynLtree[ENDBLOCK * 2] = 1;
            this.OptLen = this.StaticLen = 0;
            this.LastLit = this.Matches = 0;
        }

        /// <summary>
        /// Restore the heap property by moving down the tree starting at node k,
        /// exchanging a node with the smallest of its two sons if necessary, stopping
        /// when the heap property is re-established (each father smaller than its
        /// two sons).
        /// </summary>
        /// <param name="tree">The tree to restore.</param>
        /// <param name="k">The node to move down.</param>
        internal void Pqdownheap(short[] tree, int k)
        {
            var v = this.Heap[k];
            var j = k << 1; // left son of k
            while (j <= this.HeapLen)
            {
                // Set j to the smallest of the two sons:
                if (j < this.HeapLen && Smaller(tree, this.Heap[j + 1], this.Heap[j], this.Depth))
                {
                    j++;
                }

                // Exit if v is smaller than both sons
                if (Smaller(tree, v, this.Heap[j], this.Depth))
                {
                    break;
                }

                // Exchange v with the smallest son
                this.Heap[k] = this.Heap[j];
                k = j;

                // And continue down the tree, setting j to the left son of k
                j <<= 1;
            }

            this.Heap[k] = v;
        }

        /// <summary>
        /// Scan a literal or distance tree to determine the frequencies of the codes
        /// in the bit length tree.
        /// </summary>
        /// <param name="tree">The tree to be scanned.</param>
        /// <param name="max_code">And its largest code of non zero frequency</param>
        internal void Scan_tree(short[] tree, int max_code)
        {
            int n; // iterates over all tree elements
            var prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[(0 * 2) + 1]; // length of next code
            var count = 0; // repeat count of the current code
            var max_count = 7; // max repeat count
            var min_count = 4; // min repeat count

            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }

            tree[((max_code + 1) * 2) + 1] = -1; // guard

            for (n = 0; n <= max_code; n++)
            {
                curlen = nextlen;
                nextlen = tree[((n + 1) * 2) + 1];
                if (++count < max_count && curlen == nextlen)
                {
                    continue;
                }
                else if (count < min_count)
                {
                    this.BlTree[curlen * 2] = (short)(this.BlTree[curlen * 2] + count);
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen)
                    {
                        this.BlTree[curlen * 2]++;
                    }

                    this.BlTree[REP36 * 2]++;
                }
                else if (count <= 10)
                {
                    this.BlTree[REPZ310 * 2]++;
                }
                else
                {
                    this.BlTree[REPZ11138 * 2]++;
                }

                count = 0;
                prevlen = curlen;
                if (nextlen == 0)
                {
                    max_count = 138;
                    min_count = 3;
                }
                else if (curlen == nextlen)
                {
                    max_count = 6;
                    min_count = 3;
                }
                else
                {
                    max_count = 7;
                    min_count = 4;
                }
            }
        }

        // Construct the Huffman tree for the bit lengths and return the index in
        // bl_order of the last bit length code to send.
        internal int Build_bl_tree()
        {
            int max_blindex; // index of last bit length code of non zero freq

            // Determine the bit length frequencies for literal and distance trees
            this.Scan_tree(this.DynLtree, this.LDesc.MaxCode);
            this.Scan_tree(this.DynDtree, this.DDesc.MaxCode);

            // Build the bit length tree:
            this.BlDesc.Build_tree(this);

            // opt_len now includes the length of the tree representations, except
            // the lengths of the bit lengths codes and the 5+5+4 bits for the counts.

            // Determine the number of bit length codes to send. The pkzip format
            // requires that at least 4 bit length codes be sent. (appnote.txt says
            // 3 but the actual value used is 4.)
            for (max_blindex = BLCODES - 1; max_blindex >= 3; max_blindex--)
            {
                if (this.BlTree[(Tree.BlOrder[max_blindex] * 2) + 1] != 0)
                {
                    break;
                }
            }

            // Update opt_len to include the bit length tree and counts
            this.OptLen += (3 * (max_blindex + 1)) + 5 + 5 + 4;

            return max_blindex;
        }

        // Send the header for a block using dynamic Huffman trees: the counts, the
        // lengths of the bit length codes, the literal tree and the distance tree.
        // IN assertion: lcodes >= 257, dcodes >= 1, blcodes >= 4.
        internal void Send_all_trees(int lcodes, int dcodes, int blcodes)
        {
            int rank; // index in bl_order

            this.Send_bits(lcodes - 257, 5); // not +255 as stated in appnote.txt
            this.Send_bits(dcodes - 1, 5);
            this.Send_bits(blcodes - 4, 4); // not -3 as stated in appnote.txt
            for (rank = 0; rank < blcodes; rank++)
            {
                this.Send_bits(this.BlTree[(Tree.BlOrder[rank] * 2) + 1], 3);
            }

            this.Send_tree(this.DynLtree, lcodes - 1); // literal tree
            this.Send_tree(this.DynDtree, dcodes - 1); // distance tree
        }

        // Send a literal or distance tree in compressed form, using the codes in
        // bl_tree.
        internal void Send_tree(short[] tree, int max_code)
        {
            int n; // iterates over all tree elements
            var prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[(0 * 2) + 1]; // length of next code
            var count = 0; // repeat count of the current code
            var max_count = 7; // max repeat count
            var min_count = 4; // min repeat count

            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }

            for (n = 0; n <= max_code; n++)
            {
                curlen = nextlen;
                nextlen = tree[((n + 1) * 2) + 1];
                if (++count < max_count && curlen == nextlen)
                {
                    continue;
                }
                else if (count < min_count)
                {
                    do
                    {
                        this.Send_code(curlen, this.BlTree);
                    }
                    while (--count != 0);
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen)
                    {
                        this.Send_code(curlen, this.BlTree);
                        count--;
                    }

                    this.Send_code(REP36, this.BlTree);
                    this.Send_bits(count - 3, 2);
                }
                else if (count <= 10)
                {
                    this.Send_code(REPZ310, this.BlTree);
                    this.Send_bits(count - 3, 3);
                }
                else
                {
                    this.Send_code(REPZ11138, this.BlTree);
                    this.Send_bits(count - 11, 7);
                }

                count = 0;
                prevlen = curlen;
                if (nextlen == 0)
                {
                    max_count = 138;
                    min_count = 3;
                }
                else if (curlen == nextlen)
                {
                    max_count = 6;
                    min_count = 3;
                }
                else
                {
                    max_count = 7;
                    min_count = 4;
                }
            }
        }

        // Output a byte on the stream.
        // IN assertion: there is enough room in pending_buf.
        internal void Put_byte(byte[] p, int start, int len)
        {
            Array.Copy(p, start, this.PendingBuf, this.Pending, len);
            this.Pending += len;
        }

        internal void Put_byte(byte c) => this.PendingBuf[this.Pending++] = c;

        internal void Put_short(int w)
        {
            this.Put_byte((byte)w);
            this.Put_byte((byte)ZlibUtilities.URShift(w, 8));
        }

        internal void PutShortMSB(int b)
        {
            this.Put_byte((byte)(b >> 8));
            this.Put_byte((byte)b);
        }

        internal void Send_code(int c, short[] tree) => this.Send_bits(tree[c * 2] & 0xffff, tree[(c * 2) + 1] & 0xffff);

        internal void Send_bits(int value_Renamed, int length)
        {
            var len = length;
            if (this.BiValid > BufSize - len)
            {
                var val = value_Renamed;

                // bi_buf |= (val << bi_valid);
                this.BiBuf = (short)((ushort)this.BiBuf | (ushort)((val << this.BiValid) & 0xffff));
                this.Put_short(this.BiBuf);
                this.BiBuf = (short)ZlibUtilities.URShift(val, BufSize - this.BiValid);
                this.BiValid += len - BufSize;
            }
            else
            {
                // bi_buf |= (value) << bi_valid;
                this.BiBuf = (short)((ushort)this.BiBuf | (ushort)((value_Renamed << this.BiValid) & 0xffff));
                this.BiValid += len;
            }
        }

        // Send one empty static block to give enough lookahead for inflate.
        // This takes 10 bits, of which 7 may remain in the bit buffer.
        // The current inflate code requires 9 bits of lookahead. If the
        // last two codes for the previous block (real code plus EOB) were coded
        // on 5 bits or less, inflate may have only 5+3 bits of lookahead to decode
        // the last real code. In this case we send two empty static blocks instead
        // of one. (There are no problems if the previous block is stored or fixed.)
        // To simplify the code, we assume the worst case of last real code encoded
        // on one bit only.
        internal void Tr_align()
        {
            this.Send_bits(STATICTREES << 1, 3);
            this.Send_code(ENDBLOCK, StaticTree.StaticLtree);

            this.Bi_flush();

            // Of the 10 bits for the empty block, we have already sent
            // (10 - bi_valid) bits. The lookahead for the last real code (before
            // the EOB of the previous block) was thus at least one plus the length
            // of the EOB plus what we have just sent of the empty static block.
            if (1 + this.LastEobLen + 10 - this.BiValid < 9)
            {
                this.Send_bits(STATICTREES << 1, 3);
                this.Send_code(ENDBLOCK, StaticTree.StaticLtree);
                this.Bi_flush();
            }

            this.LastEobLen = 7;
        }

        // Save the match info and tally the frequency counts. Return true if
        // the current block must be flushed.
        internal bool Tr_tally(int dist, int lc)
        {
            this.PendingBuf[this.DBuf + (this.LastLit * 2)] = (byte)ZlibUtilities.URShift(dist, 8);
            this.PendingBuf[this.DBuf + (this.LastLit * 2) + 1] = (byte)dist;

            this.PendingBuf[this.LBuf + this.LastLit] = (byte)lc;
            this.LastLit++;

            if (dist == 0)
            {
                // lc is the unmatched char
                this.DynLtree[lc * 2]++;
            }
            else
            {
                this.Matches++;

                // Here, lc is the match length - MIN_MATCH
                dist--; // dist = match distance - 1
                this.DynLtree[(Tree.LengthCode[lc] + LITERALS + 1) * 2]++;
                this.DynDtree[Tree.D_code(dist) * 2]++;
            }

            if ((this.LastLit & 0x1fff) == 0 && this.Level > (ZlibCompressionLevel)2)
            {
                // Compute an upper bound for the compressed length
                var out_length = this.LastLit * 8;
                var in_length = this.Strstart - this.BlockStart;
                int dcode;
                for (dcode = 0; dcode < DCODES; dcode++)
                {
                    out_length = (int)(out_length + (this.DynDtree[dcode * 2] * (5L + Tree.ExtraDbits[dcode])));
                }

                out_length = ZlibUtilities.URShift(out_length, 3);
                if ((this.Matches < (this.LastLit / 2)) && out_length < in_length / 2)
                {
                    return true;
                }
            }

            return this.LastLit == this.LitBufsize - 1;

            // We avoid equality with lit_bufsize because of wraparound at 64K
            // on 16 bit machines and because stored blocks are restricted to
            // 64K-1 bytes.
        }

        // Send the block data compressed using the given Huffman trees
        internal void Compress_block(short[] ltree, short[] dtree)
        {
            int dist; // distance of matched string
            int lc; // match length or unmatched char (if dist == 0)
            var lx = 0; // running index in l_buf
            int code; // the code to send
            int extra; // number of extra bits to send

            if (this.LastLit != 0)
            {
                do
                {
                    dist = ((this.PendingBuf[this.DBuf + (lx * 2)] << 8) & 0xff00) | (this.PendingBuf[this.DBuf + (lx * 2) + 1] & 0xff);
                    lc = this.PendingBuf[this.LBuf + lx] & 0xff;
                    lx++;

                    if (dist == 0)
                    {
                        this.Send_code(lc, ltree); // send a literal byte
                    }
                    else
                    {
                        // Here, lc is the match length - MIN_MATCH
                        code = Tree.LengthCode[lc];

                        this.Send_code(code + LITERALS + 1, ltree); // send the length code
                        extra = Tree.ExtraLbits[code];
                        if (extra != 0)
                        {
                            lc -= Tree.BaseLength[code];
                            this.Send_bits(lc, extra); // send the extra length bits
                        }

                        dist--; // dist is now the match distance - 1
                        code = Tree.D_code(dist);

                        this.Send_code(code, dtree); // send the distance code
                        extra = Tree.ExtraDbits[code];
                        if (extra != 0)
                        {
                            dist -= Tree.BaseDist[code];
                            this.Send_bits(dist, extra); // send the extra distance bits
                        }
                    } // literal or match pair ?

                    // Check that the overlay between pending_buf and d_buf+l_buf is ok:
                }
                while (lx < this.LastLit);
            }

            this.Send_code(ENDBLOCK, ltree);
            this.LastEobLen = ltree[(ENDBLOCK * 2) + 1];
        }

        // Set the data type to ASCII or BINARY, using a crude approximation:
        // binary if more than 20% of the bytes are <= 6 or >= 128, ascii otherwise.
        // IN assertion: the fields freq of dyn_ltree are set and the total of all
        // frequencies does not exceed 64K (to fit in an int on 16 bit machines).
        internal void Set_data_type()
        {
            var n = 0;
            var ascii_freq = 0;
            var bin_freq = 0;
            while (n < 7)
            {
                bin_freq += this.DynLtree[n * 2];
                n++;
            }

            while (n < 128)
            {
                ascii_freq += this.DynLtree[n * 2];
                n++;
            }

            while (n < LITERALS)
            {
                bin_freq += this.DynLtree[n * 2];
                n++;
            }

            this.DataType = (byte)(bin_freq > ZlibUtilities.URShift(ascii_freq, 2) ? ZBINARY : ZASCII);
        }

        // Flush the bit buffer, keeping at most 7 bits in it.
        internal void Bi_flush()
        {
            if (this.BiValid == 16)
            {
                this.Put_short(this.BiBuf);
                this.BiBuf = 0;
                this.BiValid = 0;
            }
            else if (this.BiValid >= 8)
            {
                this.Put_byte((byte)this.BiBuf);
                this.BiBuf = (short)ZlibUtilities.URShift(this.BiBuf, 8);
                this.BiValid -= 8;
            }
        }

        // Flush the bit buffer and align the output on a byte boundary
        internal void Bi_windup()
        {
            if (this.BiValid > 8)
            {
                this.Put_short(this.BiBuf);
            }
            else if (this.BiValid > 0)
            {
                this.Put_byte((byte)this.BiBuf);
            }

            this.BiBuf = 0;
            this.BiValid = 0;
        }

        // Copy a stored block, storing first the length and its
        // one's complement if requested.
        internal void Copy_block(int buf, int len, bool header)
        {
            this.Bi_windup(); // align on byte boundary
            this.LastEobLen = 8; // enough lookahead for inflate

            if (header)
            {
                this.Put_short((short)len);
                this.Put_short((short)~len);
            }

            // while(len--!=0) {
            //    put_byte(window[buf+index]);
            //    index++;
            //  }
            this.Put_byte(this.Window, buf, len);
        }

        internal void Flush_block_only(bool eof)
        {
            this.Tr_flush_block(this.BlockStart >= 0 ? this.BlockStart : -1, this.Strstart - this.BlockStart, eof);
            this.BlockStart = this.Strstart;
            this.Strm.Flush_pending();
        }

        // Copy without compression as much as possible from the input stream, return
        // the current block state.
        // This function does not insert new strings in the dictionary since
        // uncompressible data is probably not useful. This function is used
        // only for the level=0 compression option.
        // NOTE: this function should be optimized to avoid extra copying from
        // window to pending_buf.
        internal int Deflate_stored(ZlibFlushStrategy flush)
        {
            // Stored blocks are limited to 0xffff bytes, pending_buf is limited
            // to pending_buf_size, and each stored block has a 5 byte header:
            var max_block_size = 0xffff;
            int max_start;

            if (max_block_size > this.PendingBufSize - 5)
            {
                max_block_size = this.PendingBufSize - 5;
            }

            // Copy as much as possible from input to output:
            while (true)
            {
                // Fill the window as much as possible:
                if (this.Lookahead <= 1)
                {
                    this.Fill_window();
                    if (this.Lookahead == 0 && flush == ZlibFlushStrategy.ZNOFLUSH)
                    {
                        return NeedMore;
                    }

                    if (this.Lookahead == 0)
                    {
                        break; // flush the current block
                    }
                }

                this.Strstart += this.Lookahead;
                this.Lookahead = 0;

                // Emit a stored block if pending_buf will be full:
                max_start = this.BlockStart + max_block_size;
                if (this.Strstart == 0 || this.Strstart >= max_start)
                {
                    // strstart == 0 is possible when wraparound on 16-bit machine
                    this.Lookahead = this.Strstart - max_start;
                    this.Strstart = max_start;

                    this.Flush_block_only(false);
                    if (this.Strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }

                // Flush if we may have to slide, otherwise block_start may become
                // negative and the data will be gone:
                if (this.Strstart - this.BlockStart >= this.WSize - MINLOOKAHEAD)
                {
                    this.Flush_block_only(false);
                    if (this.Strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }
            }

            this.Flush_block_only(flush == ZlibFlushStrategy.ZFINISH);
            return this.Strm.AvailOut == 0 ? (flush == ZlibFlushStrategy.ZFINISH) ? FinishStarted : NeedMore : flush == ZlibFlushStrategy.ZFINISH ? FinishDone : BlockDone;
        }

        // Send a stored block
        internal void Tr_stored_block(int buf, int stored_len, bool eof)
        {
            this.Send_bits((STOREDBLOCK << 1) + (eof ? 1 : 0), 3); // send block type
            this.Copy_block(buf, stored_len, true); // with header
        }

        // Determine the best encoding for the current block: dynamic trees, static
        // trees or store, and output the encoded block to the zip file.
        internal void Tr_flush_block(int buf, int stored_len, bool eof)
        {
            int opt_lenb, static_lenb; // opt_len and static_len in bytes
            var max_blindex = 0; // index of last bit length code of non zero freq

            // Build the Huffman trees unless a stored block is forced
            if (this.Level > 0)
            {
                // Check if the file is ascii or binary
                if (this.DataType == ZUNKNOWN)
                {
                    this.Set_data_type();
                }

                // Construct the literal and distance trees
                this.LDesc.Build_tree(this);

                this.DDesc.Build_tree(this);

                // At this point, opt_len and static_len are the total bit lengths of
                // the compressed block data, excluding the tree representations.

                // Build the bit length tree for the above two trees, and get the index
                // in bl_order of the last bit length code to send.
                max_blindex = this.Build_bl_tree();

                // Determine the best encoding. Compute first the block length in bytes
                opt_lenb = ZlibUtilities.URShift(this.OptLen + 3 + 7, 3);
                static_lenb = ZlibUtilities.URShift(this.StaticLen + 3 + 7, 3);

                if (static_lenb <= opt_lenb)
                {
                    opt_lenb = static_lenb;
                }
            }
            else
            {
                opt_lenb = static_lenb = stored_len + 5; // force a stored block
            }

            if (stored_len + 4 <= opt_lenb && buf != -1)
            {
                // 4: two words for the lengths
                // The test buf != NULL is only necessary if LIT_BUFSIZE > WSIZE.
                // Otherwise we can't have processed more than WSIZE input bytes since
                // the last block flush, because compression would have been
                // successful. If LIT_BUFSIZE <= WSIZE, it is never too late to
                // transform a block into a stored block.
                this.Tr_stored_block(buf, stored_len, eof);
            }
            else if (static_lenb == opt_lenb)
            {
                this.Send_bits((STATICTREES << 1) + (eof ? 1 : 0), 3);
                this.Compress_block(StaticTree.StaticLtree, StaticTree.StaticDtree);
            }
            else
            {
                this.Send_bits((DYNTREES << 1) + (eof ? 1 : 0), 3);
                this.Send_all_trees(this.LDesc.MaxCode + 1, this.DDesc.MaxCode + 1, max_blindex + 1);
                this.Compress_block(this.DynLtree, this.DynDtree);
            }

            // The above check is made mod 2^32, for files larger than 512 MB
            // and uLong implemented on 32 bits.
            this.Init_block();

            if (eof)
            {
                this.Bi_windup();
            }
        }

        // Fill the window when the lookahead becomes insufficient.
        // Updates strstart and lookahead.
        //
        // IN assertion: lookahead < MIN_LOOKAHEAD
        // OUT assertions: strstart <= window_size-MIN_LOOKAHEAD
        //    At least one byte has been read, or avail_in == 0; reads are
        //    performed for at least two bytes (required for the zip translate_eol
        //    option -- not supported here).
        internal void Fill_window()
        {
            int n, m;
            int p;
            int more; // Amount of free space at the end of the window.

            do
            {
                more = this.WindowSize - this.Lookahead - this.Strstart;

                // Deal with !@#$% 64K limit:
                if (more == 0 && this.Strstart == 0 && this.Lookahead == 0)
                {
                    more = this.WSize;
                }
                else if (more == -1)
                {
                    // Very unlikely, but possible on 16 bit machine if strstart == 0
                    // and lookahead == 1 (input done one byte at time)
                    more--;

                    // If the window is almost full and there is insufficient lookahead,
                    // move the upper half to the lower one to make room in the upper half.
                }
                else if (this.Strstart >= this.WSize + this.WSize - MINLOOKAHEAD)
                {
                    Array.Copy(this.Window, this.WSize, this.Window, 0, this.WSize);
                    this.MatchStart -= this.WSize;
                    this.Strstart -= this.WSize; // we now have strstart >= MAX_DIST
                    this.BlockStart -= this.WSize;

                    // Slide the hash table (could be avoided with 32 bit values
                    // at the expense of memory usage). We slide even when level == 0
                    // to keep the hash table consistent if we switch back to level > 0
                    // later. (Using level 0 permanently is not an optimal usage of
                    // zlib, so we don't care about this pathological case.)
                    n = this.HashSize;
                    p = n;
                    do
                    {
                        m = this.Head[--p] & 0xffff;
                        this.Head[p] = (short)(m >= this.WSize ? (m - this.WSize) : 0);

                        // head[p] = (m >= w_size?(short) (m - w_size):0);
                    }
                    while (--n != 0);

                    n = this.WSize;
                    p = n;
                    do
                    {
                        m = this.Prev[--p] & 0xffff;
                        this.Prev[p] = (short)(m >= this.WSize ? (m - this.WSize) : 0);

                        // prev[p] = (m >= w_size?(short) (m - w_size):0);
                        // If n is not on any hash chain, prev[n] is garbage but
                        // its value will never be used.
                    }
                    while (--n != 0);
                    more += this.WSize;
                }

                if (this.Strm.AvailIn == 0)
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
                n = this.Strm.Read_buf(this.Window, this.Strstart + this.Lookahead, more);
                this.Lookahead += n;

                // Initialize the hash value now that we have some input:
                if (this.Lookahead >= MINMATCH)
                {
                    this.InsH = this.Window[this.Strstart] & 0xff;
                    this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[this.Strstart + 1] & 0xff)) & this.HashMask;
                }

                // If the whole input has less than MIN_MATCH bytes, ins_h is garbage,
                // but this is not important since only literal bytes will be emitted.
            }
            while (this.Lookahead < MINLOOKAHEAD && this.Strm.AvailIn != 0);
        }

        // Compress as much as possible from the input stream, return the current
        // block state.
        // This function does not perform lazy evaluation of matches and inserts
        // new strings in the dictionary only for unmatched strings or for short
        // matches. It is used only for the fast compression options.
        internal int Deflate_fast(ZlibFlushStrategy flush)
        {
            // short hash_head = 0; // head of the hash chain
            var hash_head = 0; // head of the hash chain
            bool bflush; // set if current block must be flushed

            while (true)
            {
                // Make sure that we always have enough lookahead, except
                // at the end of the input file. We need MAX_MATCH bytes
                // for the next match, plus MIN_MATCH bytes to insert the
                // string following the next match.
                if (this.Lookahead < MINLOOKAHEAD)
                {
                    this.Fill_window();
                    if (this.Lookahead < MINLOOKAHEAD && flush == ZlibFlushStrategy.ZNOFLUSH)
                    {
                        return NeedMore;
                    }

                    if (this.Lookahead == 0)
                    {
                        break; // flush the current block
                    }
                }

                // Insert the string window[strstart .. strstart+2] in the
                // dictionary, and set hash_head to the head of the hash chain:
                if (this.Lookahead >= MINMATCH)
                {
                    this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[this.Strstart + (MINMATCH - 1)] & 0xff)) & this.HashMask;

                    // prev[strstart&w_mask]=hash_head=head[ins_h];
                    hash_head = this.Head[this.InsH] & 0xffff;
                    this.Prev[this.Strstart & this.WMask] = this.Head[this.InsH];
                    this.Head[this.InsH] = (short)this.Strstart;
                }

                // Find the longest match, discarding those <= prev_length.
                // At this point we have always match_length < MIN_MATCH
                if (hash_head != 0L && ((this.Strstart - hash_head) & 0xffff) <= this.WSize - MINLOOKAHEAD)
                {
                    // To simplify the code, we prevent matches with the string
                    // of window index 0 (in particular we have to avoid a match
                    // of the string with itself at the start of the input file).
                    if (this.Strategy != ZlibCompressionStrategy.ZHUFFMANONLY)
                    {
                        this.MatchLength = this.Longest_match(hash_head);
                    }

                    // longest_match() sets match_start
                }

                if (this.MatchLength >= MINMATCH)
                {
                    // check_match(strstart, match_start, match_length);
                    bflush = this.Tr_tally(this.Strstart - this.MatchStart, this.MatchLength - MINMATCH);

                    this.Lookahead -= this.MatchLength;

                    // Insert new strings in the hash table only if the match length
                    // is not too large. This saves time but degrades compression.
                    if (this.MatchLength <= this.MaxLazyMatch && this.Lookahead >= MINMATCH)
                    {
                        this.MatchLength--; // string at strstart already in hash table
                        do
                        {
                            this.Strstart++;

                            this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[this.Strstart + (MINMATCH - 1)] & 0xff)) & this.HashMask;

                            // prev[strstart&w_mask]=hash_head=head[ins_h];
                            hash_head = this.Head[this.InsH] & 0xffff;
                            this.Prev[this.Strstart & this.WMask] = this.Head[this.InsH];
                            this.Head[this.InsH] = (short)this.Strstart;

                            // strstart never exceeds WSIZE-MAX_MATCH, so there are
                            // always MIN_MATCH bytes ahead.
                        }
                        while (--this.MatchLength != 0);
                        this.Strstart++;
                    }
                    else
                    {
                        this.Strstart += this.MatchLength;
                        this.MatchLength = 0;
                        this.InsH = this.Window[this.Strstart] & 0xff;

                        this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[this.Strstart + 1] & 0xff)) & this.HashMask;

                        // If lookahead < MIN_MATCH, ins_h is garbage, but it does not
                        // matter since it will be recomputed at next deflate call.
                    }
                }
                else
                {
                    // No match, output a literal byte
                    bflush = this.Tr_tally(0, this.Window[this.Strstart] & 0xff);
                    this.Lookahead--;
                    this.Strstart++;
                }

                if (bflush)
                {
                    this.Flush_block_only(false);
                    if (this.Strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }
            }

            this.Flush_block_only(flush == ZlibFlushStrategy.ZFINISH);
            return this.Strm.AvailOut == 0 ? flush == ZlibFlushStrategy.ZFINISH ? FinishStarted : NeedMore : flush == ZlibFlushStrategy.ZFINISH ? FinishDone : BlockDone;
        }

        // Same as above, but achieves better compression. We use a lazy
        // evaluation for matches: a match is finally adopted only if there is
        // no better match at the next window position.
        internal int Deflate_slow(ZlibFlushStrategy flush)
        {
            // short hash_head = 0;    // head of hash chain
            var hash_head = 0; // head of hash chain
            bool bflush; // set if current block must be flushed

            // Process the input block.
            while (true)
            {
                // Make sure that we always have enough lookahead, except
                // at the end of the input file. We need MAX_MATCH bytes
                // for the next match, plus MIN_MATCH bytes to insert the
                // string following the next match.
                if (this.Lookahead < MINLOOKAHEAD)
                {
                    this.Fill_window();
                    if (this.Lookahead < MINLOOKAHEAD && flush == ZlibFlushStrategy.ZNOFLUSH)
                    {
                        return NeedMore;
                    }

                    if (this.Lookahead == 0)
                    {
                        break; // flush the current block
                    }
                }

                // Insert the string window[strstart .. strstart+2] in the
                // dictionary, and set hash_head to the head of the hash chain:
                if (this.Lookahead >= MINMATCH)
                {
                    this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[this.Strstart + (MINMATCH - 1)] & 0xff)) & this.HashMask;

                    // prev[strstart&w_mask]=hash_head=head[ins_h];
                    hash_head = this.Head[this.InsH] & 0xffff;
                    this.Prev[this.Strstart & this.WMask] = this.Head[this.InsH];
                    this.Head[this.InsH] = (short)this.Strstart;
                }

                // Find the longest match, discarding those <= prev_length.
                this.PrevLength = this.MatchLength;
                this.PrevMatch = this.MatchStart;
                this.MatchLength = MINMATCH - 1;

                if (hash_head != 0 && this.PrevLength < this.MaxLazyMatch && ((this.Strstart - hash_head) & 0xffff) <= this.WSize - MINLOOKAHEAD)
                {
                    // To simplify the code, we prevent matches with the string
                    // of window index 0 (in particular we have to avoid a match
                    // of the string with itself at the start of the input file).
                    if (this.Strategy != ZlibCompressionStrategy.ZHUFFMANONLY)
                    {
                        this.MatchLength = this.Longest_match(hash_head);
                    }

                    // longest_match() sets match_start
                    if (this.MatchLength <= 5 && (this.Strategy == ZlibCompressionStrategy.ZFILTERED || (this.MatchLength == MINMATCH && this.Strstart - this.MatchStart > 4096)))
                    {
                        // If prev_match is also MIN_MATCH, match_start is garbage
                        // but we will ignore the current match anyway.
                        this.MatchLength = MINMATCH - 1;
                    }
                }

                // If there was a match at the previous step and the current
                // match is not better, output the previous match:
                if (this.PrevLength >= MINMATCH && this.MatchLength <= this.PrevLength)
                {
                    var max_insert = this.Strstart + this.Lookahead - MINMATCH;

                    // Do not insert strings in hash table beyond this.

                    // check_match(strstart-1, prev_match, prev_length);
                    bflush = this.Tr_tally(this.Strstart - 1 - this.PrevMatch, this.PrevLength - MINMATCH);

                    // Insert in hash table all strings up to the end of the match.
                    // strstart-1 and strstart are already inserted. If there is not
                    // enough lookahead, the last two strings are not inserted in
                    // the hash table.
                    this.Lookahead -= this.PrevLength - 1;
                    this.PrevLength -= 2;
                    do
                    {
                        if (++this.Strstart <= max_insert)
                        {
                            this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[this.Strstart + (MINMATCH - 1)] & 0xff)) & this.HashMask;

                            // prev[strstart&w_mask]=hash_head=head[ins_h];
                            hash_head = this.Head[this.InsH] & 0xffff;
                            this.Prev[this.Strstart & this.WMask] = this.Head[this.InsH];
                            this.Head[this.InsH] = (short)this.Strstart;
                        }
                    }
                    while (--this.PrevLength != 0);
                    this.MatchAvailable = 0;
                    this.MatchLength = MINMATCH - 1;
                    this.Strstart++;

                    if (bflush)
                    {
                        this.Flush_block_only(false);
                        if (this.Strm.AvailOut == 0)
                        {
                            return NeedMore;
                        }
                    }
                }
                else if (this.MatchAvailable != 0)
                {
                    // If there was no match at the previous position, output a
                    // single literal. If there was a match but the current match
                    // is longer, truncate the previous match to a single literal.
                    bflush = this.Tr_tally(0, this.Window[this.Strstart - 1] & 0xff);

                    if (bflush)
                    {
                        this.Flush_block_only(false);
                    }

                    this.Strstart++;
                    this.Lookahead--;
                    if (this.Strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }
                else
                {
                    // There is no previous match to compare with, wait for
                    // the next step to decide.
                    this.MatchAvailable = 1;
                    this.Strstart++;
                    this.Lookahead--;
                }
            }

            if (this.MatchAvailable != 0)
            {
                bflush = this.Tr_tally(0, this.Window[this.Strstart - 1] & 0xff);
                this.MatchAvailable = 0;
            }

            this.Flush_block_only(flush == ZlibFlushStrategy.ZFINISH);

            return this.Strm.AvailOut == 0 ? flush == ZlibFlushStrategy.ZFINISH ? FinishStarted : NeedMore : flush == ZlibFlushStrategy.ZFINISH ? FinishDone : BlockDone;
        }

        internal int Longest_match(int cur_match)
        {
            var chain_length = this.MaxChainLength; // max hash chain length
            var scan = this.Strstart; // current string
            int match; // matched string
            int len; // length of current match
            var best_len = this.PrevLength; // best match length so far
            var limit = this.Strstart > (this.WSize - MINLOOKAHEAD) ? this.Strstart - (this.WSize - MINLOOKAHEAD) : 0;
            var nice_match = this.NiceMatch;

            // Stop when cur_match becomes <= limit. To simplify the code,
            // we prevent matches with the string of window index 0.
            var wmask = this.WMask;

            var strend = this.Strstart + MAXMATCH;
            var scan_end1 = this.Window[scan + best_len - 1];
            var scan_end = this.Window[scan + best_len];

            // The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple of 16.
            // It is easy to get rid of this optimization if necessary.

            // Do not waste too much time if we already have a good match:
            if (this.PrevLength >= this.GoodMatch)
            {
                chain_length >>= 2;
            }

            // Do not look for matches beyond the end of the input. This is necessary
            // to make deflate deterministic.
            if (nice_match > this.Lookahead)
            {
                nice_match = this.Lookahead;
            }

            do
            {
                match = cur_match;

                // Skip to next match if the match length cannot increase
                // or if the match length is less than 2:
                if (this.Window[match + best_len] != scan_end || this.Window[match + best_len - 1] != scan_end1 || this.Window[match] != this.Window[scan] || this.Window[++match] != this.Window[scan + 1])
                {
                    continue;
                }

                // The check at best_len-1 can be removed because it will be made
                // again later. (This heuristic is not always a win.)
                // It is not necessary to compare scan[2] and match[2] since they
                // are always equal when the other bytes match, given that
                // the hash keys are equal and that HASH_BITS >= 8.
                scan += 2;
                match++;

                // We check for insufficient lookahead only every 8th comparison;
                // the 256th check will be made at strstart+258.
                do
                {
                }
                while (this.Window[++scan] == this.Window[++match] && this.Window[++scan] == this.Window[++match] && this.Window[++scan] == this.Window[++match] && this.Window[++scan] == this.Window[++match] && this.Window[++scan] == this.Window[++match] && this.Window[++scan] == this.Window[++match] && this.Window[++scan] == this.Window[++match] && this.Window[++scan] == this.Window[++match] && scan < strend);

                len = MAXMATCH - (strend - scan);
                scan = strend - MAXMATCH;

                if (len > best_len)
                {
                    this.MatchStart = cur_match;
                    best_len = len;
                    if (len >= nice_match)
                    {
                        break;
                    }

                    scan_end1 = this.Window[scan + best_len - 1];
                    scan_end = this.Window[scan + best_len];
                }
            }
            while ((cur_match = this.Prev[cur_match & wmask] & 0xffff) > limit && --chain_length != 0);

            return best_len <= this.Lookahead ? best_len : this.Lookahead;
        }

        internal ZlibCompressionState DeflateInit(ZStream strm, ZlibCompressionLevel level, int bits)
            => this.DeflateInit2(strm, level, ZDEFLATED, bits, DEFMEMLEVEL, ZlibCompressionStrategy.ZDEFAULTSTRATEGY);

        internal ZlibCompressionState DeflateInit(ZStream strm, ZlibCompressionLevel level)
            => this.DeflateInit(strm, level, MAXWBITS);

        internal ZlibCompressionState DeflateInit2(ZStream strm, ZlibCompressionLevel level, int method, int windowBits, int memLevel, ZlibCompressionStrategy strategy)
        {
            var noheader = 0;

            // byte[] my_version=ZLIB_VERSION;

            //
            //  if (version == null || version[0] != my_version[0]
            //  || stream_size != sizeof(z_stream)) {
            //  return Z_VERSION_ERROR;
            //  }
            strm.Msg = null;

            if (level == ZlibCompressionLevel.ZDEFAULTCOMPRESSION)
            {
                level = (ZlibCompressionLevel)6;
            }

            if (windowBits < 0)
            {
                // undocumented feature: suppress zlib header
                noheader = 1;
                windowBits = -windowBits;
            }

            if (memLevel < 1 || memLevel > MAXMEMLEVEL || method != ZDEFLATED || windowBits < 9 || windowBits > 15 || level < ZlibCompressionLevel.ZNOCOMPRESSION || level > ZlibCompressionLevel.ZBESTCOMPRESSION || strategy < ZlibCompressionStrategy.ZDEFAULTSTRATEGY || strategy > ZlibCompressionStrategy.ZHUFFMANONLY)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            strm.Dstate = this;

            this.Noheader = noheader;
            this.WBits = windowBits;
            this.WSize = 1 << this.WBits;
            this.WMask = this.WSize - 1;

            this.HashBits = memLevel + 7;
            this.HashSize = 1 << this.HashBits;
            this.HashMask = this.HashSize - 1;
            this.HashShift = (this.HashBits + MINMATCH - 1) / MINMATCH;

            this.Window = new byte[this.WSize * 2];
            this.Prev = new short[this.WSize];
            this.Head = new short[this.HashSize];

            this.LitBufsize = 1 << (memLevel + 6); // 16K elements by default

            // We overlay pending_buf and d_buf+l_buf. This works since the average
            // output size for (length,distance) codes is <= 24 bits.
            this.PendingBuf = new byte[this.LitBufsize * 4];
            this.PendingBufSize = this.LitBufsize * 4;

            this.DBuf = this.LitBufsize;
            this.LBuf = (1 + 2) * this.LitBufsize;

            this.Level = level;

            // System.out.println("level="+level);
            this.Strategy = strategy;
            this.Method = (byte)method;

            return this.DeflateReset(strm);
        }

        internal ZlibCompressionState DeflateReset(ZStream strm)
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

            this.Status = (this.Noheader != 0) ? BUSYSTATE : INITSTATE;
            strm.Adler = Adler32.Calculate(0, null, 0, 0);

            this.LastFlush = ZlibFlushStrategy.ZNOFLUSH;

            this.Tr_init();
            this.Lm_init();
            return ZlibCompressionState.ZOK;
        }

        internal ZlibCompressionState DeflateEnd()
        {
            if (this.Status != INITSTATE && this.Status != BUSYSTATE && this.Status != FINISHSTATE)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            // Deallocate in reverse order of allocations:
            this.PendingBuf = null;
            this.Head = null;
            this.Prev = null;
            this.Window = null;

            // free
            // dstate=null;
            return this.Status == BUSYSTATE ? ZlibCompressionState.ZDATAERROR : ZlibCompressionState.ZOK;
        }

        internal ZlibCompressionState DeflateParams(ZStream strm, ZlibCompressionLevel level, ZlibCompressionStrategy strategy)
        {
            ZlibCompressionState err = ZlibCompressionState.ZOK;

            if (level == ZlibCompressionLevel.ZDEFAULTCOMPRESSION)
            {
                level = (ZlibCompressionLevel)6;
            }

            if (level < ZlibCompressionLevel.ZNOCOMPRESSION || level > ZlibCompressionLevel.ZBESTCOMPRESSION || strategy < ZlibCompressionStrategy.ZDEFAULTSTRATEGY || strategy > ZlibCompressionStrategy.ZHUFFMANONLY)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            if (ConfigTable[(int)this.Level].Func != ConfigTable[(int)level].Func && strm.TotalIn != 0)
            {
                // Flush the last buffer:
                err = strm.Deflate(ZlibFlushStrategy.ZPARTIALFLUSH);
            }

            if (this.Level != level)
            {
                this.Level = level;
                this.MaxLazyMatch = ConfigTable[(int)this.Level].MaxLazy;
                this.GoodMatch = ConfigTable[(int)this.Level].GoodLength;
                this.NiceMatch = ConfigTable[(int)this.Level].NiceLength;
                this.MaxChainLength = ConfigTable[(int)this.Level].MaxChain;
            }

            this.Strategy = strategy;
            return err;
        }

        internal ZlibCompressionState DeflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
        {
            var length = dictLength;
            var index = 0;

            if (dictionary == null || this.Status != INITSTATE)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            strm.Adler = Adler32.Calculate(strm.Adler, dictionary, 0, dictLength);

            if (length < MINMATCH)
            {
                return ZlibCompressionState.ZOK;
            }

            if (length > this.WSize - MINLOOKAHEAD)
            {
                length = this.WSize - MINLOOKAHEAD;
                index = dictLength - length; // use the tail of the dictionary
            }

            Array.Copy(dictionary, index, this.Window, 0, length);
            this.Strstart = length;
            this.BlockStart = length;

            // Insert all strings in the hash table (except for the last two bytes).
            // s->lookahead stays null, so s->ins_h will be recomputed at the next
            // call of fill_window.
            this.InsH = this.Window[0] & 0xff;
            this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[1] & 0xff)) & this.HashMask;

            for (var n = 0; n <= length - MINMATCH; n++)
            {
                this.InsH = ((this.InsH << this.HashShift) ^ (this.Window[n + (MINMATCH - 1)] & 0xff)) & this.HashMask;
                this.Prev[n & this.WMask] = this.Head[this.InsH];
                this.Head[this.InsH] = (short)n;
            }

            return ZlibCompressionState.ZOK;
        }

        internal ZlibCompressionState Compress(ZStream strm, ZlibFlushStrategy flush)
        {
            ZlibFlushStrategy old_flush;

            if (flush > ZlibFlushStrategy.ZFINISH || flush < 0)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            if (strm.INextOut == null || (strm.INextIn == null && strm.AvailIn != 0) || (this.Status == FINISHSTATE && flush != ZlibFlushStrategy.ZFINISH))
            {
                strm.Msg = ZErrmsg[ZlibCompressionState.ZNEEDDICT - ZlibCompressionState.ZSTREAMERROR];
                return ZlibCompressionState.ZSTREAMERROR;
            }

            if (strm.AvailOut == 0)
            {
                strm.Msg = ZErrmsg[ZlibCompressionState.ZNEEDDICT - ZlibCompressionState.ZBUFERROR];
                return ZlibCompressionState.ZBUFERROR;
            }

            this.Strm = strm; // just in case
            old_flush = this.LastFlush;
            this.LastFlush = flush;

            // Write the zlib header
            if (this.Status == INITSTATE)
            {
                var header = (ZDEFLATED + ((this.WBits - 8) << 4)) << 8;
                var level_flags = (((int)this.Level - 1) & 0xff) >> 1;

                if (level_flags > 3)
                {
                    level_flags = 3;
                }

                header |= level_flags << 6;
                if (this.Strstart != 0)
                {
                    header |= PRESETDICT;
                }

                header += 31 - (header % 31);

                this.Status = BUSYSTATE;
                this.PutShortMSB(header);

                // Save the adler32 of the preset dictionary:
                if (this.Strstart != 0)
                {
                    this.PutShortMSB((int)ZlibUtilities.URShift(strm.Adler, 16));
                    this.PutShortMSB((int)(strm.Adler & 0xffff));
                }

                strm.Adler = Adler32.Calculate(0, null, 0, 0);
            }

            // Flush as much pending output as possible
            if (this.Pending != 0)
            {
                strm.Flush_pending();
                if (strm.AvailOut == 0)
                {
                    // System.out.println("  avail_out==0");
                    // Since avail_out is 0, deflate will be called again with
                    // more output space, but possibly with both pending and
                    // avail_in equal to zero. There won't be anything to do,
                    // but this is not an error situation so make sure we
                    // return OK instead of BUF_ERROR at next call of deflate:
                    this.LastFlush = (ZlibFlushStrategy)(-1);
                    return ZlibCompressionState.ZOK;
                }

                // Make sure there is something to do and avoid duplicate consecutive
                // flushes. For repeated and useless calls with Z_FINISH, we keep
                // returning Z_STREAM_END instead of Z_BUFF_ERROR.
            }
            else if (strm.AvailIn == 0 && flush <= old_flush && flush != ZlibFlushStrategy.ZFINISH)
            {
                strm.Msg = ZErrmsg[ZlibCompressionState.ZNEEDDICT - ZlibCompressionState.ZBUFERROR];
                return ZlibCompressionState.ZBUFERROR;
            }

            // User must not provide more input after the first FINISH:
            if (this.Status == FINISHSTATE && strm.AvailIn != 0)
            {
                strm.Msg = ZErrmsg[ZlibCompressionState.ZNEEDDICT - ZlibCompressionState.ZBUFERROR];
                return ZlibCompressionState.ZBUFERROR;
            }

            // Start a new block or continue the current one.
            if (strm.AvailIn != 0 || this.Lookahead != 0 || (flush != ZlibFlushStrategy.ZNOFLUSH && this.Status != FINISHSTATE))
            {
                var bstate = -1;
                switch (ConfigTable[(int)this.Level].Func)
                {
                    case STORED:
                        bstate = this.Deflate_stored(flush);
                        break;

                    case FAST:
                        bstate = this.Deflate_fast(flush);
                        break;

                    case SLOW:
                        bstate = this.Deflate_slow(flush);
                        break;

                    default:
                        break;
                }

                if (bstate == FinishStarted || bstate == FinishDone)
                {
                    this.Status = FINISHSTATE;
                }

                if (bstate == NeedMore || bstate == FinishStarted)
                {
                    if (strm.AvailOut == 0)
                    {
                        this.LastFlush = (ZlibFlushStrategy)(-1); // avoid BUF_ERROR next call, see above
                    }

                    return ZlibCompressionState.ZOK;

                    // If flush != Z_NO_FLUSH && avail_out == 0, the next call
                    // of deflate should use the same flush parameter to make sure
                    // that the flush is complete. So we don't have to output an
                    // empty block here, this will be done at next call. This also
                    // ensures that for a very small output buffer, we emit at most
                    // one empty block.
                }

                if (bstate == BlockDone)
                {
                    if (flush == ZlibFlushStrategy.ZPARTIALFLUSH)
                    {
                        this.Tr_align();
                    }
                    else
                    {
                        // FULL_FLUSH or SYNC_FLUSH
                        this.Tr_stored_block(0, 0, false);

                        // For a full flush, this empty block will be recognized
                        // as a special marker by inflate_sync().
                        if (flush == ZlibFlushStrategy.ZFULLFLUSH)
                        {
                            // state.head[s.hash_size-1]=0;
                            for (var i = 0; i < this.HashSize; i++)
                            {
                                // forget history
                                this.Head[i] = 0;
                            }
                        }
                    }

                    strm.Flush_pending();
                    if (strm.AvailOut == 0)
                    {
                        this.LastFlush = (ZlibFlushStrategy)(-1); // avoid BUF_ERROR at next call, see above
                        return ZlibCompressionState.ZOK;
                    }
                }
            }

            if (flush != ZlibFlushStrategy.ZFINISH)
            {
                return ZlibCompressionState.ZOK;
            }

            if (this.Noheader != 0)
            {
                return ZlibCompressionState.ZSTREAMEND;
            }

            // Write the zlib trailer (adler32)
            this.PutShortMSB((int)ZlibUtilities.URShift(strm.Adler, 16));
            this.PutShortMSB((int)(strm.Adler & 0xffff));
            strm.Flush_pending();

            // If avail_out is zero, the application will call deflate again
            // to flush the rest.
            this.Noheader = -1; // write the trailer only once!
            return this.Pending != 0 ? ZlibCompressionState.ZOK : ZlibCompressionState.ZSTREAMEND;
        }

        private class Config
        {
            internal Config(int good_length, int max_lazy, int nice_length, int max_chain, int func)
            {
                this.GoodLength = good_length;
                this.MaxLazy = max_lazy;
                this.NiceLength = nice_length;
                this.MaxChain = max_chain;
                this.Func = func;
            }

            // reduce lazy search above this match length
            internal int GoodLength { get; set; }

            // do not perform lazy search above this match length
            internal int MaxLazy { get; set; }

            // quit search above this match length
            internal int NiceLength { get; set; }

            internal int MaxChain { get; set; }

            internal int Func { get; set; }
        }
    }
}
