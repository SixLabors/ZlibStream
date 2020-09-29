// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Buffers;

namespace SixLabors.ZlibStream
{
    internal sealed unsafe partial class Trees
    {
        /// <summary>
        /// A dynamic tree descriptor.
        /// </summary>
        public sealed class DynamicTreeDesc : IDisposable
        {
            private readonly CodeData[] dynTreeBuffer;
            private MemoryHandle dynTreeHandle;
            private bool isDisposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="DynamicTreeDesc"/> class.
            /// </summary>
            /// <param name="size">The size of the tree.</param>
            public DynamicTreeDesc(int size)
            {
                this.dynTreeBuffer = ArrayPool<CodeData>.Shared.Rent(size);
                this.dynTreeHandle = new Memory<CodeData>(this.dynTreeBuffer).Pin();
                this.Pointer = (CodeData*)this.dynTreeHandle.Pointer;
            }

            /// <summary>
            /// Gets or sets the corresponding static tree.
            /// </summary>
            public StaticTreeDesc StatDesc { get; set; }

            /// <summary>
            /// Gets the pointer to the tree code data.
            /// </summary>
            public CodeData* Pointer { get; }

            /// <summary>
            /// Gets or sets the largest code with non zero frequency.
            /// </summary>
            public int MaxCode { get; set; }

            // TODO: This might need refactoring.
            public ref CodeData this[int i]
            {
                get { return ref this.Pointer[i]; }
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
