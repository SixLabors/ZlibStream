// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;

namespace SixLabors.ZlibStream
{
    /// <content>
    /// Contains the buffers used during deflate.
    /// </content>
    internal sealed unsafe partial class Deflate
    {
        /// <summary>
        /// Contains buffers whose lengths are defined by compile time constants.
        /// </summary>
        public sealed class FixedLengthBuffers : IDisposable
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
            /// Initializes a new instance of the <see cref="FixedLengthBuffers"/> class.
            /// </summary>
            public FixedLengthBuffers()
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
            public ushort* BlCountPointer { get; }

            /// <summary>
            /// Gets the pointer to the heap used to build the Huffman trees.
            /// The sons of heap[n] are heap[2*n] and heap[2*n+1]. heap[0] is not used.
            /// The same heap array is used to build all trees.
            /// </summary>
            public int* HeapPointer { get; }

            /// <summary>
            /// Gets the depth of each subtree used as tie breaker for trees of equal frequency.
            /// </summary>
            public byte* DepthPointer { get; }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!this.isDisposed)
                {
                    this.depthHandle.Dispose();
                    ArrayPool<byte>.Shared.Return(this.depthBuffer);

                    this.heapHandle.Dispose();
                    ArrayPool<int>.Shared.Return(this.heapBuffer);

                    this.blCountHandle.Dispose();
                    ArrayPool<ushort>.Shared.Return(this.blCountBuffer);

                    this.isDisposed = true;
                }
            }
        }

        /// <summary>
        /// Contains buffers whose lengths are defined by parameters passed
        /// to the containing <see cref="Deflate"/> instance.
        /// </summary>
        public sealed class DynamicLengthBuffers : IDisposable
        {
            private MemoryHandle windowHandle;

            // Link to older string with same hash index. To limit the size of this
            // array to 64K, this link is maintained only for the last 32K strings.
            // An index in this array is thus a window index modulo 32K.
            private readonly ushort[] prevBuffer;
            private MemoryHandle prevHandle;

            // Heads of the hash chains or NIL.
            private readonly ushort[] headBuffer;
            private MemoryHandle headHandle;

            private MemoryHandle pendingHandle;

            private bool isDisposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="DynamicLengthBuffers"/> class.
            /// </summary>
            /// <param name="wSize">The size of the sliding window.</param>
            /// <param name="hashSize">The size of the hash chain.</param>
            /// <param name="pendingSize">The size of the pending buffer.</param>
            public DynamicLengthBuffers(int wSize, int hashSize, int pendingSize)
            {
                this.WindowBuffer = ArrayPool<byte>.Shared.Rent(wSize * 2);
                this.windowHandle = new Memory<byte>(this.WindowBuffer).Pin();
                this.WindowPointer = (byte*)this.windowHandle.Pointer;

                this.prevBuffer = ArrayPool<ushort>.Shared.Rent(wSize);
                this.prevHandle = new Memory<ushort>(this.prevBuffer).Pin();
                this.PrevPointer = (ushort*)this.prevHandle.Pointer;

                this.headBuffer = ArrayPool<ushort>.Shared.Rent(hashSize);
                this.headHandle = new Memory<ushort>(this.headBuffer).Pin();
                this.HeadPointer = (ushort*)this.headHandle.Pointer;

                // We overlay pending_buf and d_buf+l_buf. This works since the average
                // output size for (length,distance) codes is <= 24 bits.
                this.PendingSize = pendingSize;
                this.PendingBuffer = ArrayPool<byte>.Shared.Rent(pendingSize);
                this.pendingHandle = new Memory<byte>(this.PendingBuffer).Pin();
                this.PendingPointer = (byte*)this.pendingHandle.Pointer;
            }

            /// <summary>
            /// Gets the sliding window. Input bytes are read into the second half of the window,
            /// and move to the first half later to keep a dictionary of at least wSize
            /// bytes. With this organization, matches are limited to a distance of
            /// wSize-MAX_MATCH bytes, but this ensures that IO is always
            /// performed with a length multiple of the block size. Also, it limits
            /// the window size to 64K, which is quite useful on MSDOS.
            /// To do: use the user input buffer as sliding window.
            /// </summary>
            public byte[] WindowBuffer { get; }

            /// <summary>
            /// Gets the pointer to the sliding window.
            /// </summary>
            public byte* WindowPointer { get; }

            /// <summary>
            /// Gets the pointer to the buffer of older strings with the same hash index.
            /// </summary>
            public ushort* PrevPointer { get; }

            /// <summary>
            /// Gets the pointer to the head of the hash chain.
            /// </summary>
            public ushort* HeadPointer { get; }

            /// <summary>
            /// Gets the output still pending.
            /// </summary>
            public byte[] PendingBuffer { get; }

            /// <summary>
            /// Gets the size of the pending buffer.
            /// </summary>
            public int PendingSize { get; }

            /// <summary>
            /// Gets the pointer to the pending output buffer.
            /// </summary>
            public byte* PendingPointer { get; }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!this.isDisposed)
                {
                    this.windowHandle.Dispose();
                    ArrayPool<byte>.Shared.Return(this.WindowBuffer);

                    this.prevHandle.Dispose();
                    ArrayPool<ushort>.Shared.Return(this.prevBuffer);

                    this.headHandle.Dispose();
                    ArrayPool<ushort>.Shared.Return(this.headBuffer);

                    this.pendingHandle.Dispose();
                    ArrayPool<byte>.Shared.Return(this.PendingBuffer);

                    this.isDisposed = true;
                }
            }
        }
    }
}
