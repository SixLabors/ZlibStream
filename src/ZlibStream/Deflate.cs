// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

// Uncomment to us DeflateQuick
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

        private const int STOREDBLOCK = 0;
        private const int STATICTREES = 1;
        private const int DYNTREES = 2;

        // The three kinds of block type
        private const int ZBINARY = 0;
        private const int ZASCII = 1;
        private const int ZUNKNOWN = 2;

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
            "need dictionary", "stream end", string.Empty, "file error", "stream error",
            "data error", "insufficient memory", "buffer error", "incompatible version",
            string.Empty,
        };

        // State
        private ZStream strm; // pointer back to this zlib stream
        private int status; // as the name implies

        private int pendingBufferSize; // size of pending_buf
        private byte[] pendingBuffer; // output still pending
        private MemoryHandle pendingHandle;
        private byte* pendingPointer;

        private byte dataType; // UNKNOWN, BINARY or ASCII
        private byte method; // STORED (for zip only) or DEFLATED
        private ZlibFlushStrategy lastFlush; // value of flush param for previous deflate call

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

        private ZlibCompressionLevel level; // compression level (1..9)
        private ZlibCompressionStrategy strategy; // favor or force Huffman coding

        // Use a faster search when the previous match is longer than this
        private int goodMatch;

        // Stop searching when current match exceeds this
        private int niceMatch;

        // Used by trees.
        private readonly ushort[] dynLtreeBuffer; // literal and length tree
        private MemoryHandle dynLtreeHandle;
        private readonly ushort* dynLtreePointer;

        private readonly ushort[] dynDtreeBuffer; // distance tree
        private MemoryHandle dynDtreeHandle;
        private readonly ushort* dynDtreePointer;

        private readonly ushort[] blTreeBuffer; // Huffman tree for bit lengths
        private MemoryHandle bltreeHandle;
        private readonly ushort* blTreePointer;

        private readonly Tree lDesc = new Tree(); // desc for literal tree
        private readonly Tree dDesc = new Tree(); // desc for distance tree
        private readonly Tree blDesc = new Tree(); // desc for bit length tree

        // number of codes at each bit length for an optimal tree
        private readonly ushort[] blCountBuffer;
        private MemoryHandle blCountHandle;

        // heap used to build the Huffman trees
        private readonly int[] heapBuffer;
        private MemoryHandle heapHandle;

        // Depth of each subtree used as tie breaker for trees of equal frequency
        private readonly byte[] depthBuffer;
        private MemoryHandle depthHandle;

        private int matches; // number of string matches in current block

        private int lastEobLen;  // bit length of EOB code for last block

        // Output buffer. bits are inserted starting at the bottom (least
        // significant bits).
        private ushort biBuf;

        // Number of valid bits in bi_buf.  All bits above the last valid bit
        // are always zero.
        private int biValid;

        private int lBuf; // index for literals or lengths */

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

        private int lastLit; // running index in l_buf

        // Buffer for distances. To simplify the code, d_buf and l_buf have
        // the same number of elements. To use different lengths, an extra flag
        // array would be necessary.
        private int dBuf; // index of pendig_buf

        // Whether or not a block is currently open for the QUICK deflation scheme.
        // This is set to true if there is an active block, or false if the block was just
        // closed.
        private bool blockOpen;

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

            this.dynLtreeBuffer = ArrayPool<ushort>.Shared.Rent(HEAPSIZE * 2); // literal and length tree
            this.dynLtreeHandle = new Memory<ushort>(this.dynLtreeBuffer).Pin();
            this.dynLtreePointer = (ushort*)this.dynLtreeHandle.Pointer;

            this.dynDtreeBuffer = ArrayPool<ushort>.Shared.Rent(((2 * DCODES) + 1) * 2); // Distance tree
            this.dynDtreeHandle = new Memory<ushort>(this.dynDtreeBuffer).Pin();
            this.dynDtreePointer = (ushort*)this.dynDtreeHandle.Pointer;

            this.blTreeBuffer = ArrayPool<ushort>.Shared.Rent(((2 * BLCODES) + 1) * 2); // Huffman tree for bit lengths
            this.bltreeHandle = new Memory<ushort>(this.blTreeBuffer).Pin();
            this.blTreePointer = (ushort*)this.bltreeHandle.Pointer;
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

        [MethodImpl(InliningOptions.ShortMethod)]
        private static bool Smaller(ushort* tree, int n, int m, byte* depth)
        {
            int n2 = 2 * n;
            int m2 = 2 * m;
            return tree[n2] < tree[m2] || (tree[n2] == tree[m2] && depth[n] <= depth[m]);
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

        // Initialize the tree data structures for a new zlib stream.
        private void Tr_init()
        {
            this.lDesc.DynTree = this.dynLtreeBuffer;
            this.lDesc.StatDesc = StaticTree.StaticLDesc;

            this.dDesc.DynTree = this.dynDtreeBuffer;
            this.dDesc.StatDesc = StaticTree.StaticDDesc;

            this.blDesc.DynTree = this.blTreeBuffer;
            this.blDesc.StatDesc = StaticTree.StaticBlDesc;

            this.biBuf = 0;
            this.biValid = 0;
            this.lastEobLen = 8; // enough lookahead for inflate

            // Initialize the first block of the first file:
            this.Init_block();
        }

        private void Init_block()
        {
            // Initialize the trees.
            ushort* dynLtree = this.dynLtreePointer;
            ushort* dynDtree = this.dynDtreePointer;
            ushort* blTree = this.blTreePointer;

            for (int i = 0; i < LCODES; i++)
            {
                dynLtree[i * 2] = 0;
            }

            for (int i = 0; i < DCODES; i++)
            {
                dynDtree[i * 2] = 0;
            }

            for (int i = 0; i < BLCODES; i++)
            {
                blTree[i * 2] = 0;
            }

            dynLtree[ENDBLOCK * 2] = 1;
            this.OptLen = this.StaticLen = 0;
            this.lastLit = this.matches = 0;
        }

        /// <summary>
        /// Restore the heap property by moving down the tree starting at node k,
        /// exchanging a node with the smallest of its two sons if necessary, stopping
        /// when the heap property is re-established (each father smaller than its
        /// two sons).
        /// </summary>
        /// <param name="tree">The tree to restore.</param>
        /// <param name="k">The node to move down.</param>
        [MethodImpl(InliningOptions.ShortMethod)]
        public void Pqdownheap(ushort* tree, int k)
        {
            int* heap = this.HeapPointer;
            byte* depth = this.DepthPointer;
            int v = heap[k];
            int heapLen = this.HeapLen;
            int j = k << 1; // left son of k
            while (j <= heapLen)
            {
                // Set j to the smallest of the two sons:
                if (j < heapLen && Smaller(tree, heap[j + 1], heap[j], depth))
                {
                    j++;
                }

                // Exit if v is smaller than both sons
                if (Smaller(tree, v, heap[j], depth))
                {
                    break;
                }

                // Exchange v with the smallest son
                heap[k] = heap[j];
                k = j;

                // And continue down the tree, setting j to the left son of k
                j <<= 1;
            }

            heap[k] = v;
        }

        /// <summary>
        /// Scan a literal or distance tree to determine the frequencies of the codes
        /// in the bit length tree.
        /// </summary>
        /// <param name="tree">The tree to be scanned.</param>
        /// <param name="max_code">And its largest code of non zero frequency</param>
        private void Scan_tree(ushort* tree, int max_code)
        {
            int n; // iterates over all tree elements
            int prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[(0 * 2) + 1]; // length of next code
            int count = 0; // repeat count of the current code
            int max_count = 7; // max repeat count
            int min_count = 4; // min repeat count
            ushort* blTree = this.blTreePointer;

            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }

            tree[((max_code + 1) * 2) + 1] = ushort.MaxValue; // guard

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
                    blTree[curlen * 2] = (ushort)(blTree[curlen * 2] + count);
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen)
                    {
                        blTree[curlen * 2]++;
                    }

                    blTree[REP36 * 2]++;
                }
                else if (count <= 10)
                {
                    blTree[REPZ310 * 2]++;
                }
                else
                {
                    blTree[REPZ11138 * 2]++;
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
        private int Build_bl_tree()
        {
            int max_blindex; // index of last bit length code of non zero freq

            // Determine the bit length frequencies for literal and distance trees
            this.Scan_tree(this.dynLtreePointer, this.lDesc.MaxCode);
            this.Scan_tree(this.dynDtreePointer, this.dDesc.MaxCode);

            // Build the bit length tree:
            this.blDesc.Build_tree(this);

            // opt_len now includes the length of the tree representations, except
            // the lengths of the bit lengths codes and the 5+5+4 bits for the counts.

            // Determine the number of bit length codes to send. The pkzip format
            // requires that at least 4 bit length codes be sent. (appnote.txt says
            // 3 but the actual value used is 4.)
            ushort* blTree = this.blTreePointer;
            for (max_blindex = BLCODES - 1; max_blindex >= 3; max_blindex--)
            {
                if (blTree[(Tree.BlOrder[max_blindex] * 2) + 1] != 0)
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
        private void Send_all_trees(int lcodes, int dcodes, int blcodes)
        {
            int rank; // index in bl_order
            ushort* blTree = this.blTreePointer;
            this.Send_bits(lcodes - 257, 5); // not +255 as stated in appnote.txt
            this.Send_bits(dcodes - 1, 5);
            this.Send_bits(blcodes - 4, 4); // not -3 as stated in appnote.txt
            for (rank = 0; rank < blcodes; rank++)
            {
                this.Send_bits(blTree[(Tree.BlOrder[rank] * 2) + 1], 3);
            }

            this.Send_tree(this.dynLtreePointer, lcodes - 1); // literal tree
            this.Send_tree(this.dynDtreePointer, dcodes - 1); // distance tree
        }

        // Send a literal or distance tree in compressed form, using the codes in
        // bl_tree.
        private void Send_tree(ushort* tree, int max_code)
        {
            ushort* blTree = this.blTreePointer;
            int n; // iterates over all tree elements
            int prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[(0 * 2) + 1]; // length of next code
            int count = 0; // repeat count of the current code
            int max_count = 7; // max repeat count
            int min_count = 4; // min repeat count

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
                        this.Send_code(curlen, blTree);
                    }
                    while (--count != 0);
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen)
                    {
                        this.Send_code(curlen, blTree);
                        count--;
                    }

                    this.Send_code(REP36, blTree);
                    this.Send_bits(count - 3, 2);
                }
                else if (count <= 10)
                {
                    this.Send_code(REPZ310, blTree);
                    this.Send_bits(count - 3, 3);
                }
                else
                {
                    this.Send_code(REPZ11138, blTree);
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
            ushort* s = (ushort*)&this.pendingPointer[this.Pending];
            s[0] = (ushort)w;
            this.Pending += 2;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void PutShortMSB(int b)
        {
            this.PutByte((byte)(b >> 8));
            this.PutByte((byte)b);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void Send_code(int c, ushort* tree)
            => this.Send_bits(tree[c * 2], tree[(c * 2) + 1]);

        [MethodImpl(InliningOptions.ShortMethod)]
        private void Send_code(int c, ushort[] tree)
            => this.Send_bits(tree[c * 2], tree[(c * 2) + 1]);

        [MethodImpl(InliningOptions.ShortMethod)]
        private void Send_bits(int value, int length)
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

        // Send one empty static block to give enough lookahead for inflate.
        // This takes 10 bits, of which 7 may remain in the bit buffer.
        // The current inflate code requires 9 bits of lookahead. If the
        // last two codes for the previous block (real code plus EOB) were coded
        // on 5 bits or less, inflate may have only 5+3 bits of lookahead to decode
        // the last real code. In this case we send two empty static blocks instead
        // of one. (There are no problems if the previous block is stored or fixed.)
        // To simplify the code, we assume the worst case of last real code encoded
        // on one bit only.
        [MethodImpl(InliningOptions.ShortMethod)]
        private void Tr_align()
        {
            this.Tr_emit_tree(STATICTREES, false);
            this.Tr_emit_end_block(StaticTree.StaticLtree, false);

            this.Bi_flush();

            // Of the 10 bits for the empty block, we have already sent
            // (10 - bi_valid) bits. The lookahead for the last real code (before
            // the EOB of the previous block) was thus at least one plus the length
            // of the EOB plus what we have just sent of the empty static block.
            if (1 + this.lastEobLen + 10 - this.biValid < 9)
            {
                this.Tr_emit_tree(STATICTREES, false);
                this.Tr_emit_end_block(StaticTree.StaticLtree, false);
                this.Bi_flush();
            }

            this.lastEobLen = 7;
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
            this.dynLtreePointer[(Tree.LengthCode[len] + LITERALS + 1) * 2]++;
            this.dynDtreePointer[Tree.D_code(dist) * 2]++;

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
            this.dynLtreePointer[c * 2]++;

            return this.lastLit == this.litBufsize - 1;
        }

        // Send the block data compressed using the given Huffman trees
        [MethodImpl(InliningOptions.HotPath)]
        private void Compress_block(ushort* ltree, ushort* dtree)
        {
            int dist; // distance of matched string
            int lc; // match length or unmatched char (if dist == 0)
            int lx = 0; // running index in l_buf

            if (this.lastLit != 0)
            {
                byte* pending = this.pendingPointer;

                do
                {
                    dist = ((pending[this.dBuf + (lx * 2)] << 8) & 0xFF00) | pending[this.dBuf + (lx * 2) + 1];
                    lc = pending[this.lBuf + lx];
                    lx++;

                    if (dist == 0)
                    {
                        this.Send_code(lc, ltree); // send a literal byte
                    }
                    else
                    {
                        this.Tr_emit_distance(ltree, dtree, lc, dist);
                    } // literal or match pair ?

                    // Check that the overlay between pending_buf and d_buf+l_buf is ok:
                }
                while (lx < this.lastLit);
            }

            this.Send_code(ENDBLOCK, ltree);
            this.lastEobLen = ltree[(ENDBLOCK * 2) + 1];
            this.blockOpen = false;
        }

        // Set the data type to ASCII or BINARY, using a crude approximation:
        // binary if more than 20% of the bytes are <= 6 or >= 128, ascii otherwise.
        // IN assertion: the fields freq of dyn_ltree are set and the total of all
        // frequencies does not exceed 64K (to fit in an int on 16 bit machines).
        private void Set_data_type()
        {
            int n = 0;
            int ascii_freq = 0;
            int bin_freq = 0;
            ushort* dynLtree = this.dynLtreePointer;

            while (n < 7)
            {
                bin_freq += dynLtree[n * 2];
                n++;
            }

            while (n < 128)
            {
                ascii_freq += dynLtree[n * 2];
                n++;
            }

            while (n < LITERALS)
            {
                bin_freq += dynLtree[n * 2];
                n++;
            }

            this.dataType = (byte)(bin_freq > (ascii_freq >> 2) ? ZBINARY : ZASCII);
        }

        // Flush the bit buffer, keeping at most 7 bits in it.
        [MethodImpl(InliningOptions.ShortMethod)]
        private void Bi_flush()
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
        private void Bi_windup()
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
        private void Copy_block(int buf, int len, bool header)
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

        [MethodImpl(InliningOptions.ShortMethod)]
        private void Flush_block_only(bool eof)
        {
            this.Tr_flush_block(this.blockStart >= 0 ? this.blockStart : -1, this.strStart - this.blockStart, eof);
            this.blockStart = this.strStart;
            this.Flush_pending(this.strm);
        }

        // Send a stored block
        [MethodImpl(InliningOptions.ShortMethod)]
        private void Tr_stored_block(int buf, int stored_len, bool eof)
        {
            this.Tr_emit_tree(STOREDBLOCK, eof);
            this.Copy_block(buf, stored_len, true); // with header
        }

        // Send the start of a block
        [MethodImpl(InliningOptions.ShortMethod)]
        private void Tr_emit_tree(int type, bool eof)
        {
            this.Send_bits((type << 1) + (eof ? 1 : 0), 3); // send block type
        }

        // Send the end of a block
        [MethodImpl(InliningOptions.ShortMethod)]
        private void Tr_emit_end_block(ushort[] tree, bool last)
        {
            this.Send_code(ENDBLOCK, tree);
            if (last)
            {
                this.Bi_windup();
            }
        }

        // Emit match dist/length code.
        [MethodImpl(InliningOptions.ShortMethod)]
        private void Tr_emit_distance(ushort* ltree, ushort* dtree, int lc, int dist)
        {
            // code is the code to send
            // extra is number of extra bits to send
            // Here, lc is the match length - MINMATCH
            int code = Tree.LengthCode[lc];

            this.Send_code(code + LITERALS + 1, ltree); // send the length code
            int extra = Tree.ExtraLbits[code];
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
        }

        // Determine the best encoding for the current block: dynamic trees, static
        // trees or store, and output the encoded block to the zip file.
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private void Tr_flush_block(int buf, int stored_len, bool eof)
        {
            int opt_lenb, static_lenb; // opt_len and static_len in bytes
            int max_blindex = 0; // index of last bit length code of non zero freq

            // Build the Huffman trees unless a stored block is forced
            if (this.level > 0)
            {
                // Check if the file is ascii or binary
                if (this.dataType == ZUNKNOWN)
                {
                    this.Set_data_type();
                }

                // Construct the literal and distance trees
                this.lDesc.Build_tree(this);

                this.dDesc.Build_tree(this);

                // At this point, opt_len and static_len are the total bit lengths of
                // the compressed block data, excluding the tree representations.

                // Build the bit length tree for the above two trees, and get the index
                // in bl_order of the last bit length code to send.
                max_blindex = this.Build_bl_tree();

                // Determine the best encoding. Compute first the block length in bytes
                opt_lenb = (this.OptLen + 3 + 7) >> 3;
                static_lenb = (this.StaticLen + 3 + 7) >> 3;

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
                this.Tr_emit_tree(STATICTREES, eof);

                fixed (ushort* ltree = StaticTree.StaticLtree)
                {
                    fixed (ushort* dtree = StaticTree.StaticDtree)
                    {
                        this.Compress_block(ltree, dtree);
                    }
                }
            }
            else
            {
                this.Tr_emit_tree(DYNTREES, eof);
                this.Send_all_trees(this.lDesc.MaxCode + 1, this.dDesc.MaxCode + 1, max_blindex + 1);
                this.Compress_block(this.dynLtreePointer, this.dynDtreePointer);
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
                    this.matchStart = cur_match;
                    best_len = len;
                    if (len >= nice_match)
                    {
                        break;
                    }

                    scan_end = *(ushort*)&scan[best_len - 1];
                }
            }
            while ((cur_match = prev[cur_match & wmask]) > limit && --chain_length != 0);

            return Math.Min(best_len, this.lookahead);
        }

        internal ZlibCompressionState DeflateInit(ZStream strm, ZlibCompressionLevel level, int bits)
            => this.DeflateInit2(strm, level, ZDEFLATED, bits, DEFMEMLEVEL, ZlibCompressionStrategy.ZDEFAULTSTRATEGY);

        internal ZlibCompressionState DeflateInit(ZStream strm, ZlibCompressionLevel level)
            => this.DeflateInit(strm, level, MAXWBITS);

        internal ZlibCompressionState DeflateInit2(
            ZStream strm,
            ZlibCompressionLevel level,
            int method,
            int windowBits,
            int memLevel,
            ZlibCompressionStrategy strategy)
        {
            int noheader = 0;
            strm.Msg = null;

            if (level == ZlibCompressionLevel.ZDEFAULTCOMPRESSION)
            {
                level = ZlibCompressionLevel.Level6;
            }

            if (level == ZlibCompressionLevel.ZNOCOMPRESSION)
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
                || level < ZlibCompressionLevel.ZNOCOMPRESSION
                || level > ZlibCompressionLevel.ZBESTCOMPRESSION
                || strategy < ZlibCompressionStrategy.ZDEFAULTSTRATEGY
                || strategy > ZlibCompressionStrategy.ZRLE)
            {
                return ZlibCompressionState.ZSTREAMERROR;
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

        private ZlibCompressionState DeflateReset(ZStream strm)
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

            this.lastFlush = ZlibFlushStrategy.ZNOFLUSH;

            this.Tr_init();
            this.Lm_init();
            return ZlibCompressionState.ZOK;
        }

        internal ZlibCompressionState DeflateEnd()
        {
            if (this.status != INITSTATE && this.status != BUSYSTATE && this.status != FINISHSTATE)
            {
                return ZlibCompressionState.ZSTREAMERROR;
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

            this.bltreeHandle.Dispose();
            ArrayPool<ushort>.Shared.Return(this.blTreeBuffer);

            this.dynDtreeHandle.Dispose();
            ArrayPool<ushort>.Shared.Return(this.dynDtreeBuffer);

            this.dynLtreeHandle.Dispose();
            ArrayPool<ushort>.Shared.Return(this.dynLtreeBuffer);

            this.depthHandle.Dispose();
            ArrayPool<byte>.Shared.Return(this.depthBuffer);

            this.heapHandle.Dispose();
            ArrayPool<int>.Shared.Return(this.heapBuffer);

            this.blCountHandle.Dispose();
            ArrayPool<ushort>.Shared.Return(this.blCountBuffer);

            // free
            // dstate=null;
            return this.status == BUSYSTATE ? ZlibCompressionState.ZDATAERROR : ZlibCompressionState.ZOK;
        }

        internal ZlibCompressionState DeflateParams(ZStream strm, ZlibCompressionLevel level, ZlibCompressionStrategy strategy)
        {
            ZlibCompressionState err = ZlibCompressionState.ZOK;

            if (level == ZlibCompressionLevel.ZDEFAULTCOMPRESSION)
            {
                level = ZlibCompressionLevel.Level6;
            }

            if (level < ZlibCompressionLevel.ZNOCOMPRESSION
                || level > ZlibCompressionLevel.ZBESTCOMPRESSION
                || strategy < ZlibCompressionStrategy.ZDEFAULTSTRATEGY
                || strategy > ZlibCompressionStrategy.ZHUFFMANONLY)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            if (ConfigTable[(int)this.level].Func != ConfigTable[(int)level].Func && strm.TotalIn != 0)
            {
                // Flush the last buffer:
                err = strm.Deflate(ZlibFlushStrategy.ZPARTIALFLUSH);
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

        internal ZlibCompressionState DeflateSetDictionary(ZStream strm, byte[] dictionary, int dictLength)
        {
            int length = dictLength;
            int index = 0;

            if (dictionary == null || this.status != INITSTATE)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            strm.Adler = Adler32.Calculate(strm.Adler, dictionary.AsSpan(0, dictLength));

            if (length < MINMATCH)
            {
                return ZlibCompressionState.ZOK;
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

            return ZlibCompressionState.ZOK;
        }

        internal ZlibCompressionState Compress(ZStream strm, ZlibFlushStrategy flush)
        {
            ZlibFlushStrategy old_flush;

            if (flush > ZlibFlushStrategy.ZFINISH || flush < 0)
            {
                return ZlibCompressionState.ZSTREAMERROR;
            }

            if (strm.INextOut == null
                || (strm.INextIn == null && strm.AvailIn != 0)
                || (this.status == FINISHSTATE && flush != ZlibFlushStrategy.ZFINISH))
            {
                strm.Msg = ZErrmsg[ZlibCompressionState.ZNEEDDICT - ZlibCompressionState.ZSTREAMERROR];
                return ZlibCompressionState.ZSTREAMERROR;
            }

            if (strm.AvailOut == 0)
            {
                strm.Msg = ZErrmsg[ZlibCompressionState.ZNEEDDICT - ZlibCompressionState.ZBUFERROR];
                return ZlibCompressionState.ZBUFERROR;
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
                    this.lastFlush = (ZlibFlushStrategy)(-1);
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
            if (this.status == FINISHSTATE && strm.AvailIn != 0)
            {
                strm.Msg = ZErrmsg[ZlibCompressionState.ZNEEDDICT - ZlibCompressionState.ZBUFERROR];
                return ZlibCompressionState.ZBUFERROR;
            }

            // Start a new block or continue the current one.
            if (strm.AvailIn != 0
                || this.lookahead != 0
                || (flush != ZlibFlushStrategy.ZNOFLUSH && this.status != FINISHSTATE))
            {
                int bstate = -1;

                if (this.strategy == ZlibCompressionStrategy.ZRLE)
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
                        this.lastFlush = (ZlibFlushStrategy)(-1); // avoid BUF_ERROR next call, see above
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
                        this.lastFlush = (ZlibFlushStrategy)(-1); // avoid BUF_ERROR at next call, see above
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
            this.PutShortMSB((int)strm.Adler >> 16);
            this.PutShortMSB((int)strm.Adler);
            this.Flush_pending(strm);

            // If avail_out is zero, the application will call deflate again
            // to flush the rest.
            this.Noheader = -1; // write the trailer only once!
            return this.Pending != 0 ? ZlibCompressionState.ZOK : ZlibCompressionState.ZSTREAMEND;
        }

        // Flush as much pending output as possible. All deflate() output goes
        // through this function so some applications may wish to modify it
        // to avoid allocating a large strm->next_out buffer and copying into it.
        // (See also read_buf()).
        internal void Flush_pending(ZStream strm)
        {
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
        /// Update a hash value with the given input byte
        /// IN  assertion: all calls to UPDATE_HASH are made with consecutive input
        /// characters, so that a running hash key can be computed from the previous
        /// key instead of complete recalculation each time.
        /// </summary>
        /// <param name="val">The input byte.</param>
        [MethodImpl(InliningOptions.ShortMethod)]
        private uint UpdateHash(uint val) => (val * 2654435761U) >> (32 - this.hashBits);

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
