// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <content>
    /// Contains the stored deflate implementation.
    /// </content>
    internal sealed unsafe partial class Deflate
    {
        /// <summary>
        /// Copy without compression as much as possible from the input stream, return
        /// the current block state.
        /// This function does not insert new strings in the dictionary since
        /// uncompressible data is probably not useful. This function is used
        /// only for the level=0 compression option.
        /// NOTE: this function should be optimized to avoid extra copying from
        /// window to pending_buf.
        /// </summary>
        /// <param name="flush">The flush strategy.</param>
        /// <returns>The <see cref="int"/>.</returns>
        [MethodImpl(InliningOptions.HotPath)]
        private int DeflateStored(ZlibFlushStrategy flush)
        {
            // Smallest worthy block size when not flushing or finishing. By default
            // this is 32K.This can be as small as 507 bytes for memLevel == 1., pending_buf is limited
            // to pending_buf_size, and each stored block has a 5 byte header:
            int max_block_size = Math.Min(this.pendingBufferSize - 5, this.wSize);
            int max_start;

            // Copy as much as possible from input to output:
            while (true)
            {
                // Fill the window as much as possible:
                if (this.lookahead <= 1)
                {
                    this.Fill_window();
                    if (this.lookahead == 0 && flush == ZlibFlushStrategy.ZNOFLUSH)
                    {
                        return NeedMore;
                    }

                    if (this.lookahead == 0)
                    {
                        break; // flush the current block
                    }
                }

                this.strStart += this.lookahead;
                this.lookahead = 0;

                // Emit a stored block if pending_buf will be full:
                max_start = this.blockStart + max_block_size;
                if (this.strStart == 0 || this.strStart >= max_start)
                {
                    // strstart == 0 is possible when wraparound on 16-bit machine
                    this.lookahead = this.strStart - max_start;
                    this.strStart = max_start;

                    this.Flush_block_only(false);
                    if (this.strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }

                // Flush if we may have to slide, otherwise block_start may become
                // negative and the data will be gone:
                if (this.strStart - this.blockStart >= this.wSize - MINLOOKAHEAD)
                {
                    this.Flush_block_only(false);
                    if (this.strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }
            }

            this.Flush_block_only(flush == ZlibFlushStrategy.ZFINISH);
            return this.strm.AvailOut == 0 ? (flush == ZlibFlushStrategy.ZFINISH)
                ? FinishStarted
                : NeedMore : flush == ZlibFlushStrategy.ZFINISH ? FinishDone : BlockDone;
        }
    }
}
