// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    internal sealed unsafe partial class Trees
    {
        /// <summary>
        /// A dynamic tree descriptor.
        /// </summary>
        public sealed class DynamicTreeDesc : IDisposable
        {
            private CodeData[] dynTreeBuffer;
            private MemoryHandle dynTreeHandle;
            private readonly CodeData* dynTreePointer;

            private bool isDisposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="DynamicTreeDesc"/> class.
            /// </summary>
            /// <param name="size">The size of the tree.</param>
            public DynamicTreeDesc(int size)
            {
                this.dynTreeBuffer = ArrayPool<CodeData>.Shared.Rent(size);
                this.dynTreeHandle = new Memory<CodeData>(this.dynTreeBuffer).Pin();
                this.dynTreePointer = (CodeData*)this.dynTreeHandle.Pointer;
            }

            /// <summary>
            /// Gets or sets the corresponding static tree.
            /// </summary>
            public StaticTreeDesc StatDesc { get; set; }

            /// <summary>
            /// Gets the largest code with non zero frequency.
            /// </summary>
            internal int MaxCode { get; private set; }

            // TODO: This might need refactoring.
            public ref CodeData this[int i]
            {
                get { return ref this.dynTreePointer[i]; }
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
            internal void Build_tree(Deflate s, DynamicTreeDesc desc)
            {
                fixed (CodeData* streePtr = &desc.StatDesc.StaticTreeValue.DangerousGetReference())
                {
                    DynamicTreeDesc tree = desc;
                    CodeData* stree = streePtr;

                    int elems = this.StatDesc.Elems;
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
                        if (stree != null)
                        {
                            s.StaticLen -= stree[node].Len;
                        }

                        // node is 0 or 1 so it does not have extra bits
                    }

                    this.MaxCode = max_code;

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
                    Gen_codes(tree, max_code, blCount);
                }
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
            private static void Pqdownheap(Deflate s, DynamicTreeDesc tree, int k)
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
            private static bool Smaller(DynamicTreeDesc tree, int n, int m, byte* depth)
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
            private static void Gen_bitlen(Deflate s, DynamicTreeDesc descr)
            {
                fixed (CodeData* streePtr = &descr.StatDesc.StaticTreeValue.DangerousGetReference())
                fixed (int* extraPtr = &descr.StatDesc.ExtraBits.DangerousGetReference())
                {
                    DynamicTreeDesc tree = descr;
                    CodeData* stree = streePtr;
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
                        if (stree != null)
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
            /// <param name="tree"></param>
            /// <param name="max_code"></param>
            /// <param name="bl_count"></param>
            [MethodImpl(InliningOptions.ShortMethod)]
            private static void Gen_codes(DynamicTreeDesc tree, int max_code, ushort* bl_count)
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

            /// <inheritdoc/>
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!this.isDisposed)
                {
                    if (disposing)
                    {
                        this.dynTreeHandle.Dispose();
                        ArrayPool<CodeData>.Shared.Return(this.dynTreeBuffer);
                    }

                    this.isDisposed = true;
                }
            }
        }
    }
}
