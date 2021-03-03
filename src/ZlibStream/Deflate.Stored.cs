// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

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
        private int DeflateStored(FlushMode flush)
        {
            // Smallest worthy block size when not flushing or finishing. By default
            // this is 32K.This can be as small as 507 bytes for memLevel == 1., pending_buf is limited
            // to pending_buf_size, and each stored block has a 5 byte header:
            int max_block_size = Math.Min(this.DynamicBuffers.PendingSize - 5, this.wSize);
            int max_start;

            // Copy as much as possible from input to output:
            while (true)
            {
                // Fill the window as much as possible:
                if (this.lookahead <= 1)
                {
                    this.Fill_window();
                    if (this.lookahead == 0 && flush == FlushMode.NoFlush)
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
                    if (this.strm.AvailableOut == 0)
                    {
                        return NeedMore;
                    }
                }

                // Flush if we may have to slide, otherwise block_start may become
                // negative and the data will be gone:
                if (this.strStart - this.blockStart >= this.wSize - MINLOOKAHEAD)
                {
                    this.Flush_block_only(false);
                    if (this.strm.AvailableOut == 0)
                    {
                        return NeedMore;
                    }
                }
            }

            this.Flush_block_only(flush == FlushMode.Finish);
            return this.strm.AvailableOut == 0 ? (flush == FlushMode.Finish)
                ? FinishStarted
                : NeedMore : flush == FlushMode.Finish ? FinishDone : BlockDone;
        }
    }
}
