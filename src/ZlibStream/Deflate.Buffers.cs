// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Buffers;

namespace SixLabors.ZlibStream
{
    internal sealed unsafe partial class Deflate
    {
        /// <summary>
        /// Contains buffers whose lengths are defined by compile time constants.
        /// </summary>
        public class ConstantBuffers : IDisposable
        {
            private bool isDisposed;

            // Number of codes at each bit length for an optimal tree
            private readonly ushort[] blCountBuffer;
            private MemoryHandle blCountHandle;

            // Heap used to build the Huffman trees
            private readonly int[] heapBuffer;
            private MemoryHandle heapHandle;

            // Depth of each subtree used as tie breaker for trees of equal frequency
            private readonly byte[] depthBuffer;
            private MemoryHandle depthHandle;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConstantBuffers"/> class.
            /// </summary>
            public ConstantBuffers()
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

            /// <summary>
            /// Gets number of codes at each bit length for an optimal tree.
            /// </summary>
            public ushort* BlCountPointer { get; private set; }

            /// <summary>
            /// Gets the pointer to the heap used to build the Huffman trees.
            /// The sons of heap[n] are heap[2*n] and heap[2*n+1]. heap[0] is not used.
            /// The same heap array is used to build all trees.
            /// </summary>
            public int* HeapPointer { get; private set; }

            /// <summary>
            /// Gets the depth of each subtree used as tie breaker for trees of equal frequency.
            /// </summary>
            public byte* DepthPointer { get; private set; }

            /// <inheritdoc/>
            public void Dispose() => this.Dispose(true);

            protected virtual void Dispose(bool disposing)
            {
                if (!this.isDisposed)
                {
                    if (disposing)
                    {
                        this.depthHandle.Dispose();
                        ArrayPool<byte>.Shared.Return(this.depthBuffer);

                        this.heapHandle.Dispose();
                        ArrayPool<int>.Shared.Return(this.heapBuffer);

                        this.blCountHandle.Dispose();
                        ArrayPool<ushort>.Shared.Return(this.blCountBuffer);
                    }

                    this.isDisposed = true;
                }
            }
        }
    }
}
