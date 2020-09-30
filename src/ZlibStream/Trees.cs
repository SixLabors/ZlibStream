// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    internal sealed unsafe partial class Trees
    {
        // Bit length codes must not exceed MAX_BL_BITS bits
        internal const int MAXBLBITS = 7;

        // end of block literal code
        internal const int ENDBLOCK = 256;

        // repeat previous bit length 3-6 times (2 bits of repeat count)
        internal const int REP36 = 16;

        // repeat a zero length 3-10 times  (3 bits of repeat count)
        internal const int REPZ310 = 17;

        // repeat a zero length 11-138 times  (7 bits of repeat count)
        internal const int REPZ11138 = 18;

        // The lengths of the bit length codes are sent in order of decreasing
        // probability, to avoid transmitting the lengths for unused bit
        // length codes.
        internal const int BufSize = 8 * 2;

        // see definition of array dist_code below
        internal const int DISTCODELEN = 512;

        // extra bits for each length code
        internal static readonly int[] ExtraLbits =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3,
            3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0,
        };

        // extra bits for each distance code
        internal static readonly int[] ExtraDbits =
        {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7,
            8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13,
        };

        // extra bits for each bit length code
        internal static readonly int[] ExtraBlbits =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 7,
        };

        internal static readonly byte[] BlOrder =
        {
            16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15,
        };

        internal static readonly byte[] DistCode =
        {
            0, 1, 2, 3, 4, 4, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8,
            9, 9, 9, 9, 9, 9, 9, 9, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11, 11,
            11, 11, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
            12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 12, 13, 13,
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
            13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14,
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
            14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14, 14,
            14, 14, 14, 14, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 0, 0, 16, 17,
            18, 18, 19, 19, 20, 20, 20, 20, 21, 21, 21, 21, 22, 22, 22, 22, 22, 22,
            22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 24, 24, 24, 24, 24, 24, 24, 24,
            24, 24, 24, 24, 24, 24, 24, 24, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
            25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
            26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
            26, 26, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
            27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28,
            28, 28, 28, 28, 28, 28, 28, 28, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
            29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
            29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
            29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29, 29,
        };

        internal static readonly byte[] LengthCode =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 12, 12, 13,
            13, 13, 13, 14, 14, 14, 14, 15, 15, 15, 15, 16, 16, 16, 16, 16, 16, 16,
            16, 17, 17, 17, 17, 17, 17, 17, 17, 18, 18, 18, 18, 18, 18, 18, 18, 19,
            19, 19, 19, 19, 19, 19, 19, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20,
            20, 20, 20, 20, 20, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21, 21,
            21, 21, 21, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22,
            22, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 24,
            24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24,
            24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 25, 25, 25, 25, 25,
            25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25,
            25, 25, 25, 25, 25, 25, 25, 25, 25, 26, 26, 26, 26, 26, 26, 26, 26, 26,
            26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26, 26,
            26, 26, 26, 26, 26, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
            27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27, 27,
            28,
        };

        internal static readonly int[] BaseLength =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56,
            64, 80, 96, 112, 128, 160, 192, 224, 0,
        };

        internal static readonly int[] BaseDist =
        {
            0, 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384,
            512, 768, 1024, 1536, 2048, 3072, 4096, 6144, 8192, 12288, 16384,
            24576,
        };

        private const int MAXBITS = 15;
        private const int BLCODES = 19;
        private const int DCODES = 30;
        private const int LITERALS = 256;
        private const int LENGTHCODES = 29;
        private const int LCODES = LITERALS + 1 + LENGTHCODES;
        private const int HEAPSIZE = (2 * LCODES) + 1;

        internal ushort[] DynTree { get; set; } // the dynamic tree

        internal int MaxCode { get; private set; } // largest code with non zero frequency

        internal StaticTreeDesc StatDesc { get; set; } // the corresponding static tree

        // Mapping from a distance to a distance code. dist is the distance - 1 and
        // must not have side effects. _dist_code[256] and _dist_code[257] are never
        // used.
        [MethodImpl(InliningOptions.ShortMethod)]
        internal static int D_code(int dist)
            => dist < 256 ? DistCode[dist] : DistCode[256 + (dist >> 7)];

        // Generate the codes for a given tree and bit counts (which need not be
        // optimal).
        // IN assertion: the array bl_count contains the bit length statistics for
        // the given tree and the field len is set for all tree elements.
        // OUT assertion: the field code is set for all tree elements of non
        //     zero code length.
        [MethodImpl(InliningOptions.ShortMethod)]
        private static void Gen_codes(ushort* tree, int max_code, ushort* bl_count)
        {
            ushort* next_code = stackalloc ushort[MAXBITS + 1]; // next code value for each bit length

            ushort code = 0; // running code value
            int bits; // bit index
            int n; // code index

            // The distribution counts are first used to generate the code values
            // without bit reversal.
            for (bits = 1; bits <= MAXBITS; bits++)
            {
                next_code[bits] = code = (ushort)((code + bl_count[bits - 1]) << 1);
            }

            // Check that the bit counts in bl_count are consistent. The last code
            // must be all ones.
            for (n = 0; n <= max_code; n++)
            {
                int n2 = n * 2;
                int len = tree[n2 + 1];
                if (len == 0)
                {
                    continue;
                }

                // Now reverse the bits
                tree[n2] = (ushort)Bi_reverse(next_code[len]++, len);
            }
        }

        /// <summary>
        /// Reverse the first len bits of a code, using straightforward code (a faster
        /// method would use a table)
        /// </summary>
        /// <param name="code">The value to invert.</param>
        /// <param name="len">Its bit length.</param>
        /// <returns>The <see cref="uint"/>.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        private static uint Bi_reverse(uint code, int len)
        {
            // IN assertion: 1 <= len <= 15
            uint res = 0;
            do
            {
                res |= code & 1U;
                code >>= 1;
                res <<= 1;
            }
            while (--len > 0);
            return res >> 1;
        }

        /// <summary>
        /// Scan a literal or distance tree to determine the frequencies of the codes
        /// in the bit length tree.
        /// </summary>
        /// <param name="s">The data compressor.</param>
        /// <param name="tree">The tree to be scanned.</param>
        /// <param name="max_code">And its largest code of non zero frequency</param>
        public static void Scan_tree(Deflate s, DynamicTreeDesc tree, int max_code)
        {
            int n; // iterates over all tree elements
            int prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[0].Len; // length of next code
            ushort count = 0; // repeat count of the current code
            int max_count = 7; // max repeat count
            int min_count = 4; // min repeat count
            DynamicTreeDesc blTree = s.DynBLTree;

            if (nextlen == 0)
            {
                max_count = 138;
                min_count = 3;
            }

            tree[max_code + 1].Len = ushort.MaxValue; // guard

            for (n = 0; n <= max_code; n++)
            {
                curlen = nextlen;
                nextlen = tree[n + 1].Len;
                if (++count < max_count && curlen == nextlen)
                {
                    continue;
                }
                else if (count < min_count)
                {
                    blTree[curlen].Freq += count;
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen)
                    {
                        blTree[curlen].Freq++;
                    }

                    blTree[REP36].Freq++;
                }
                else if (count <= 10)
                {
                    blTree[REPZ310].Freq++;
                }
                else
                {
                    blTree[REPZ11138].Freq++;
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
        private static int Build_bl_tree(Deflate s)
        {
            int max_blindex; // index of last bit length code of non zero freq

            // Determine the bit length frequencies for literal and distance trees
            Scan_tree(s, s.DynLTree, s.DynLTree.MaxCode);
            Scan_tree(s, s.DynDTree, s.DynDTree.MaxCode);

            // Build the bit length tree:
            Build_tree(s, s.DynBLTree);

            // opt_len now includes the length of the tree representations, except
            // the lengths of the bit lengths codes and the 5+5+4 bits for the counts.

            // Determine the number of bit length codes to send. The pkzip format
            // requires that at least 4 bit length codes be sent. (appnote.txt says
            // 3 but the actual value used is 4.)
            DynamicTreeDesc blTree = s.DynBLTree;
            for (max_blindex = BLCODES - 1; max_blindex >= 3; max_blindex--)
            {
                if (blTree[BlOrder[max_blindex]].Len != 0)
                {
                    break;
                }
            }

            // Update opt_len to include the bit length tree and counts
            s.OptLen += (3 * (max_blindex + 1)) + 5 + 5 + 4;

            return max_blindex;
        }

        /// <summary>
        /// Construct one Huffman tree and assigns the code bit strings and lengths.
        /// Update the total bit length for the current block.
        /// IN assertion: the field freq is set for all tree elements.
        /// OUT assertions: the fields len and code are set to the optimal bit length
        /// and corresponding code. The length opt_len is updated; static_len is
        /// also updated if stree is not null. The field max_code is set.
        /// </summary>
        /// <param name="s">The data compressor.</param>
        /// <param name="desc">The dynamic tree descriptor.</param>
        public static void Build_tree(Deflate s, DynamicTreeDesc desc)
        {
            DynamicTreeDesc tree = desc;
            StaticTreeDesc stree = tree.StatDesc;
            int elems = desc.StatDesc.Elems;
            int n, m; // iterate over heap elements
            var max_code = -1; // largest code with non zero frequency
            int node; // new node being created
            ushort* blCount = s.BlCountPointer;
            int* heap = s.HeapPointer;
            byte* depth = s.DepthPointer;

            // Construct the initial heap, with least frequent element in
            // heap[1]. The sons of heap[n] are heap[2*n] and heap[2*n+1].
            // heap[0] is not used.
            s.HeapLen = 0;
            s.HeapMax = HEAPSIZE;

            for (n = 0; n < elems; n++)
            {
                if (tree[n].Freq != 0)
                {
                    heap[++s.HeapLen] = max_code = n;
                    depth[n] = 0;
                }
                else
                {
                    tree[n].Len = 0;
                }
            }

            // The pkzip format requires that at least one distance code exists,
            // and that at least one bit should be sent even if there is only one
            // possible code. So to avoid special checks later on we force at least
            // two codes of non zero frequency.
            while (s.HeapLen < 2)
            {
                node = heap[++s.HeapLen] = max_code < 2
                    ? ++max_code
                    : 0;

                tree[node].Freq = 1;
                depth[node] = 0;
                s.OptLen--;
                if (stree.HasTree)
                {
                    s.StaticLen -= stree[node].Len;
                }

                // node is 0 or 1 so it does not have extra bits
            }

            desc.MaxCode = max_code;

            // The elements heap[heap_len/2+1 .. heap_len] are leaves of the tree,
            // establish sub-heaps of increasing lengths:
            for (n = s.HeapLen / 2; n >= 1; n--)
            {
                Pqdownheap(s, tree, n);
            }

            // Construct the Huffman tree by repeatedly combining the least two
            // frequent nodes.
            node = elems; // next internal node of the tree
            do
            {
                // TODO: PQRemove?

                // n = node of least frequency
                n = heap[1];
                heap[1] = heap[s.HeapLen--];
                Pqdownheap(s, tree, 1);
                m = heap[1]; // m = node of next least frequency

                heap[--s.HeapMax] = n; // keep the nodes sorted by frequency
                heap[--s.HeapMax] = m;

                // Create a new node father of n and m
                tree[node].Freq = (ushort)(tree[n].Freq + tree[m].Freq);
                depth[node] = (byte)(Math.Max(depth[n], depth[m]) + 1);
                tree[n].Dad = tree[m].Dad = (ushort)node;

                // and insert the new node in the heap
                heap[1] = node++;
                Pqdownheap(s, tree, 1);
            }
            while (s.HeapLen >= 2);

            heap[--s.HeapMax] = heap[1];

            // At this point, the fields freq and dad are set. We can now
            // generate the bit lengths.
            Gen_bitlen(s, tree);

            // The field len is now set, we can generate the bit codes
            Gen_codes(tree.Pointer, max_code, blCount);
        }

        /// <summary>
        /// Restore the heap property by moving down the tree starting at node k,
        /// exchanging a node with the smallest of its two sons if necessary, stopping
        /// when the heap property is re-established (each father smaller than its
        /// two sons).
        /// </summary>
        /// <param name="s">The data compressor.</param>
        /// <param name="tree">The tree to restore.</param>
        /// <param name="k">The node to move down.</param>
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void Pqdownheap(Deflate s, DynamicTreeDesc tree, int k)
        {
            int* heap = s.HeapPointer;
            byte* depth = s.DepthPointer;

            int v = heap[k];
            int heapLen = s.HeapLen;
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

        [MethodImpl(InliningOptions.ShortMethod)]
        public static bool Smaller(DynamicTreeDesc tree, int n, int m, byte* depth)
        {
            return tree[n].Freq < tree[m].Freq
                || (tree[n].Freq == tree[m].Freq && depth[n] <= depth[m]);
        }

        // Compute the optimal bit lengths for a tree and update the total bit length
        // for the current block.
        // IN assertion: the fields freq and dad are set, heap[heap_max] and
        //    above are the tree nodes sorted by increasing frequency.
        // OUT assertions: the field len is set to the optimal bit length, the
        //     array bl_count contains the frequencies for each bit length.
        //     The length opt_len is updated; static_len is also updated if stree is
        //     not null.
        public static void Gen_bitlen(Deflate s, DynamicTreeDesc descr)
        {
            fixed (int* extraPtr = descr.StatDesc.ExtraBits)
            {
                DynamicTreeDesc tree = descr;
                StaticTreeDesc stree = tree.StatDesc;
                int* extra = extraPtr;
                int extra_base = descr.StatDesc.ExtraBase;
                int max_code = descr.MaxCode;
                int max_length = descr.StatDesc.MaxLength;
                int h; // heap index
                int n, m; // iterate over the tree elements
                int bits; // bit length
                int xbits; // extra bits
                ushort f; // frequency
                int overflow = 0; // number of elements with bit length too large
                ushort* blCount = s.BlCountPointer;
                int* heap = s.HeapPointer;

                for (bits = 0; bits <= MAXBITS; bits++)
                {
                    blCount[bits] = 0;
                }

                // In a first pass, compute the optimal bit lengths (which may
                // overflow in the case of the bit length tree).
                tree[heap[s.HeapMax]].Len = 0; // root of the heap

                for (h = s.HeapMax + 1; h < HEAPSIZE; h++)
                {
                    n = heap[h];
                    bits = tree[tree[n].Dad].Len + 1;
                    if (bits > max_length)
                    {
                        bits = max_length;
                        overflow++;
                    }

                    tree[n].Len = (ushort)bits;

                    // We overwrite tree[n].Dad which is no longer needed
                    if (n > max_code)
                    {
                        continue; // not a leaf node
                    }

                    blCount[bits]++;
                    xbits = 0;
                    if (n >= extra_base)
                    {
                        xbits = extra[n - extra_base];
                    }

                    f = tree[n].Freq;
                    s.OptLen += f * (bits + xbits);
                    if (stree.HasTree)
                    {
                        s.StaticLen += f * (stree[n].Len + xbits);
                    }
                }

                if (overflow == 0)
                {
                    return;
                }

                // This happens for example on obj2 and pic of the Calgary corpus
                // Find the first bit length which could increase:
                do
                {
                    bits = max_length - 1;
                    while (blCount[bits] == 0)
                    {
                        bits--;
                    }

                    blCount[bits]--; // move one leaf down the tree
                    blCount[bits + 1] += 2; // move one overflow item as its brother
                    blCount[max_length]--;

                    // The brother of the overflow item also moves one step up,
                    // but this does not affect bl_count[max_length]
                    overflow -= 2;
                }
                while (overflow > 0);

                // Now recompute all bit lengths, scanning in increasing frequency.
                // h is still equal to HEAP_SIZE. (It is simpler to reconstruct all
                // lengths instead of fixing only the wrong ones.This idea is taken
                // from 'ar' written by Haruhiko Okumura.)
                for (bits = max_length; bits != 0; bits--)
                {
                    n = blCount[bits];
                    while (n != 0)
                    {
                        m = heap[--h];
                        if (m > max_code)
                        {
                            continue;
                        }

                        if (tree[m].Len != bits)
                        {
                            s.OptLen += (int)(ulong)(bits * tree[m].Freq);
                            s.OptLen -= (int)(ulong)(tree[m].Len * tree[m].Freq);
                            tree[m].Len = (ushort)bits;
                        }

                        n--;
                    }
                }
            }
        }

        /// <summary>
        /// Generate the codes for a given tree and bit counts (which need not be
        /// optimal).
        /// IN assertion: the array bl_count contains the bit length statistics for
        /// the given tree and the field len is set for all tree elements.
        /// OUT assertion: the field code is set for all tree elements of non
        /// zero code length.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="max_code">The max code.</param>
        /// <param name="bl_count">The bit length count.</param>
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void Gen_codes(CodeData* tree, int max_code, ushort* bl_count)
        {
            ushort* next_code = stackalloc ushort[MAXBITS + 1]; // next code value for each bit length

            ushort code = 0; // running code value
            int bits; // bit index
            int n; // code index

            // The distribution counts are first used to generate the code values
            // without bit reversal.
            for (bits = 1; bits <= MAXBITS; bits++)
            {
                next_code[bits] = code = (ushort)((code + bl_count[bits - 1]) << 1);
            }

            // Check that the bit counts in bl_count are consistent. The last code
            // must be all ones.
            for (n = 0; n <= max_code; n++)
            {
                int len = tree[n].Len;
                if (len == 0)
                {
                    continue;
                }

                // Now reverse the bits
                tree[n].Code = (ushort)Bi_reverse(next_code[len]++, len);
            }
        }

        // Determine the best encoding for the current block: dynamic trees, static
        // trees or store, and output the encoded block to the zip file.
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        public static void Tr_flush_block(Deflate s, int buf, int stored_len, bool eof)
        {
            int opt_lenb, static_lenb; // opt_len and static_len in bytes
            int max_blindex = 0; // index of last bit length code of non zero freq

            // Build the Huffman trees unless a stored block is forced
            if (s.level > 0)
            {
                // Check if the file is ascii or binary
                if (s.dataType == Deflate.ZUNKNOWN)
                {
                    Set_data_type(s);
                }

                // Construct the literal and distance trees
                Build_tree(s, s.DynLTree);

                Build_tree(s, s.DynDTree);

                // At this point, opt_len and static_len are the total bit lengths of
                // the compressed block data, excluding the tree representations.
                //
                // Build the bit length tree for the above two trees, and get the index
                // in bl_order of the last bit length code to send.
                max_blindex = Build_bl_tree(s);

                // Determine the best encoding. Compute first the block length in bytes
                opt_lenb = (s.OptLen + 3 + 7) >> 3;
                static_lenb = (s.StaticLen + 3 + 7) >> 3;

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
                Tr_stored_block(s, buf, stored_len, eof);
            }
            else if (static_lenb == opt_lenb)
            {
                Tr_emit_tree(s, Deflate.STATICTREES, eof);

                fixed (CodeData* ltree = &StaticLTree.DangerousGetReference())
                fixed (CodeData* dtree = &StaticDTtree.DangerousGetReference())
                {
                    Compress_block(s, ltree, dtree);
                }
            }
            else
            {
                Tr_emit_tree(s, Deflate.DYNTREES, eof);
                Send_all_trees(s, s.DynLTree.MaxCode + 1, s.DynDTree.MaxCode + 1, max_blindex + 1);
                Compress_block(s, s.DynLTree.Pointer, s.DynDTree.Pointer);
            }

            // The above check is made mod 2^32, for files larger than 512 MB
            // and uLong implemented on 32 bits.
            Init_block(s);

            if (eof)
            {
                s.Bi_windup();
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
        public static void Tr_align(Deflate s)
        {
            fixed (CodeData* ltree = &StaticLTree.DangerousGetReference())
            {
                Tr_emit_tree(s, Deflate.STATICTREES, false);
                Tr_emit_end_block(s, ltree, false);

                s.Bi_flush();

                // Of the 10 bits for the empty block, we have already sent
                // (10 - bi_valid) bits. The lookahead for the last real code (before
                // the EOB of the previous block) was thus at least one plus the length
                // of the EOB plus what we have just sent of the empty static block.
                if (1 + s.lastEobLen + 10 - s.biValid < 9)
                {
                    Tr_emit_tree(s, Deflate.STATICTREES, false);
                    Tr_emit_end_block(s, ltree, false);
                    s.Bi_flush();
                }

                s.lastEobLen = 7;
            }
        }

        // Send the end of a block
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void Tr_emit_end_block(Deflate s, CodeData* tree, bool last)
        {
            s.Send_code(ENDBLOCK, tree);
            if (last)
            {
                s.Bi_windup();
            }
        }

        // Emit match dist/length code.
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void Tr_emit_distance(Deflate s, CodeData* ltree, CodeData* dtree, int lc, int dist)
        {
            // code is the code to send
            // extra is number of extra bits to send
            // Here, lc is the match length - MINMATCH
            int code = LengthCode[lc];

            s.Send_code(code + LITERALS + 1, ltree); // send the length code
            int extra = ExtraLbits[code];
            if (extra != 0)
            {
                lc -= BaseLength[code];
                s.Send_bits(lc, extra); // send the extra length bits
            }

            dist--; // dist is now the match distance - 1
            code = D_code(dist);

            s.Send_code(code, dtree); // send the distance code
            extra = ExtraDbits[code];
            if (extra != 0)
            {
                dist -= BaseDist[code];
                s.Send_bits(dist, extra); // send the extra distance bits
            }
        }

        // Send a stored block
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void Tr_stored_block(Deflate s, int buf, int stored_len, bool eof)
        {
            Tr_emit_tree(s, Deflate.STOREDBLOCK, eof);
            s.Copy_block(buf, stored_len, true); // with header
        }

        // Send the start of a block
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void Tr_emit_tree(Deflate s, int type, bool eof)
        {
            s.Send_bits((type << 1) + (eof ? 1 : 0), 3); // send block type
        }

        // Initialize the tree data structures for a new zlib stream.
        public static void Tr_init(Deflate s)
        {
            s.biBuf = 0;
            s.biValid = 0;
            s.lastEobLen = 8; // enough lookahead for inflate

            // Initialize the first block of the first file:
            Init_block(s);
        }

        private static void Init_block(Deflate s)
        {
            // Initialize the trees.
            Trees.DynamicTreeDesc dynLtree = s.DynLTree;
            Trees.DynamicTreeDesc dynDtree = s.DynDTree;
            Trees.DynamicTreeDesc blTree = s.DynBLTree;

            for (int i = 0; i < LCODES; i++)
            {
                dynLtree[i].Freq = 0;
            }

            for (int i = 0; i < DCODES; i++)
            {
                dynDtree[i].Freq = 0;
            }

            for (int i = 0; i < BLCODES; i++)
            {
                blTree[i].Freq = 0;
            }

            dynLtree[ENDBLOCK].Freq = 1;
            s.OptLen = s.StaticLen = 0;
            s.lastLit = s.matches = 0;
        }

        // Set the data type to ASCII or BINARY, using a crude approximation:
        // binary if more than 20% of the bytes are <= 6 or >= 128, ascii otherwise.
        // IN assertion: the fields freq of dyn_ltree are set and the total of all
        // frequencies does not exceed 64K (to fit in an int on 16 bit machines).
        private static void Set_data_type(Deflate s)
        {
            int n = 0;
            int ascii_freq = 0;
            int bin_freq = 0;
            var dynLtree = s.DynLTree;

            while (n < 7)
            {
                bin_freq += dynLtree[n].Freq;
                n++;
            }

            while (n < 128)
            {
                ascii_freq += dynLtree[n].Freq;
                n++;
            }

            while (n < LITERALS)
            {
                bin_freq += dynLtree[n].Freq;
                n++;
            }

            s.dataType = (byte)(bin_freq > (ascii_freq >> 2)
                ? Deflate.ZBINARY
                : Deflate.ZASCII);
        }

        // Send the header for a block using dynamic Huffman trees: the counts, the
        // lengths of the bit length codes, the literal tree and the distance tree.
        // IN assertion: lcodes >= 257, dcodes >= 1, blcodes >= 4.
        private static void Send_all_trees(Deflate s, int lcodes, int dcodes, int blcodes)
        {
            int rank; // index in bl_order
            CodeData* blTree = s.DynBLTree.Pointer;
            s.Send_bits(lcodes - 257, 5); // not +255 as stated in appnote.txt
            s.Send_bits(dcodes - 1, 5);
            s.Send_bits(blcodes - 4, 4); // not -3 as stated in appnote.txt
            for (rank = 0; rank < blcodes; rank++)
            {
                s.Send_bits(blTree[BlOrder[rank]].Len, 3);
            }

            Send_tree(s, s.DynLTree.Pointer, lcodes - 1); // literal tree
            Send_tree(s, s.DynDTree.Pointer, dcodes - 1); // distance tree
        }

        /// <summary>
        /// Send a literal or distance tree in compressed form, using the codes in
        /// bl_tree.
        /// </summary>
        /// <param name="s">The data compressor.</param>
        /// <param name="tree">The tree to be scanned.</param>
        /// <param name="max_code">The max_code and its largest code of non zero frequency.</param>
        private static void Send_tree(Deflate s, CodeData* tree, int max_code)
        {
            CodeData* blTree = s.DynBLTree.Pointer;
            int n; // iterates over all tree elements
            int prevlen = -1; // last emitted length
            int curlen; // length of current code
            int nextlen = tree[0].Len; // length of next code
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
                nextlen = tree[n + 1].Len;
                if (++count < max_count && curlen == nextlen)
                {
                    continue;
                }
                else if (count < min_count)
                {
                    do
                    {
                        s.Send_code(curlen, blTree);
                    }
                    while (--count != 0);
                }
                else if (curlen != 0)
                {
                    if (curlen != prevlen)
                    {
                        s.Send_code(curlen, blTree);
                        count--;
                    }

                    s.Send_code(REP36, blTree);
                    s.Send_bits(count - 3, 2);
                }
                else if (count <= 10)
                {
                    s.Send_code(REPZ310, blTree);
                    s.Send_bits(count - 3, 3);
                }
                else
                {
                    s.Send_code(REPZ11138, blTree);
                    s.Send_bits(count - 11, 7);
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

        // Send the block data compressed using the given Huffman trees
        [MethodImpl(InliningOptions.HotPath)]
        private static void Compress_block(Deflate s, CodeData* ltree, CodeData* dtree)
        {
            int dist; // distance of matched string
            int lc; // match length or unmatched char (if dist == 0)
            int lx = 0; // running index in l_buf

            if (s.lastLit != 0)
            {
                byte* pending = s.pendingPointer;

                do
                {
                    dist = ((pending[s.dBuf + (lx * 2)] << 8) & 0xFF00) | pending[s.dBuf + (lx * 2) + 1];
                    lc = pending[s.lBuf + lx];
                    lx++;

                    if (dist == 0)
                    {
                        s.Send_code(lc, ltree); // send a literal byte
                    }
                    else
                    {
                        Tr_emit_distance(s, ltree, dtree, lc, dist);
                    } // literal or match pair ?

                    // Check that the overlay between pending_buf and d_buf+l_buf is ok:
                }
                while (lx < s.lastLit);
            }

            s.Send_code(ENDBLOCK, ltree);
            s.lastEobLen = ltree[ENDBLOCK].Len;
            s.blockOpen = false;
        }
    }
}
