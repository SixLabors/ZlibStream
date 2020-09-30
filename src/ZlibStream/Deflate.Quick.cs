// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <content>
    /// Contains the quick deflate implementation.
    /// </content>
    internal sealed unsafe partial class Deflate
    {
        /// <summary>
        /// The deflate_quick deflate strategy, designed to be used when cycles are
        /// at a premium.
        /// </summary>
        /// <param name="flush">The flush strategy.</param>
        /// <returns>The <see cref="int"/>.</returns>
        [MethodImpl(InliningOptions.HotPath)]
        internal int DeflateQuick(FlushStrategy flush)
        {
            int hash_head; // head of the hash chain
            int dist;
            int matchLen;
            bool last;

            byte* window = this.windowPointer;
            ushort* head = this.headPointer;
            ushort* prev = this.prevPointer;

            fixed (Trees.CodeData* ltree = Trees.StaticLTree)
            fixed (Trees.CodeData* dtree = Trees.StaticDTtree)
            {
                if (!this.blockOpen && this.lookahead > 0)
                {
                    // Start new block when we have lookahead data, so that if no
                    // input data is given an empty block will not be written.
                    last = flush == FlushStrategy.Finish;
                    this.QuickStartBlock(last);
                }

                do
                {
                    if (this.Pending + 12 >= this.pendingBufferSize)
                    {
                        this.Flush_pending(this.strm);
                        if (this.strm.AvailIn == 0 && flush != FlushStrategy.Finish)
                        {
                            // Break to emit end block and return need_more
                            break;
                        }
                    }

                    if (this.lookahead < MINLOOKAHEAD)
                    {
                        this.Fill_window();
                        if (this.lookahead < MINLOOKAHEAD && flush == FlushStrategy.NoFlush)
                        {
                            // Always emit end block, in case next call is with Z_FINISH
                            // and we need to emit start of last block
                            this.QuickEndBlock(ltree, false);
                            return NeedMore;
                        }

                        if (this.lookahead == 0)
                        {
                            break;
                        }

                        if (!this.blockOpen)
                        {
                            // Start new block when we have lookahead data, so that if no
                            // input data is given an empty block will not be written.
                            last = flush == FlushStrategy.Finish;
                            this.QuickStartBlock(last);
                        }
                    }

                    if (this.lookahead >= MINMATCH)
                    {
                        hash_head = this.InsertString(prev, head, window, this.strStart);
                        dist = this.strStart - hash_head;

                        if (dist > 0 && dist < this.wSize - MINLOOKAHEAD)
                        {
                            matchLen = Compare258(window + this.strStart, window + hash_head);

                            if (matchLen >= MINMATCH)
                            {
                                if (matchLen > this.lookahead)
                                {
                                    matchLen = this.lookahead;
                                }

                                Trees.Tr_emit_distance(this, ltree, dtree, matchLen - MINMATCH, dist);
                                this.lookahead -= matchLen;
                                this.strStart += matchLen;
                                continue;
                            }
                        }
                    }

                    this.Send_code(window[this.strStart], ltree); // send a literal byte
                    this.strStart++;
                    this.lookahead--;
                }
                while (this.strm.AvailOut != 0);

                last = flush == FlushStrategy.Finish;
                this.QuickEndBlock(ltree, last);
                this.Flush_pending(this.strm);

                if (last)
                {
                    return this.strm.AvailOut == 0
                        ? this.strm.AvailIn == 0 ? FinishStarted : NeedMore
                        : FinishDone;
                }

                return BlockDone;
            }
        }

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private static int Compare258(byte* src0, byte* src1)
        {
            if (*(ushort*)src0 != *(ushort*)src1)
            {
                return (*src0 == *src1) ? 1 : 0;
            }

            return Compare256(src0 + 2, src1 + 2) + 2;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void QuickStartBlock(bool last)
        {
            Trees.Tr_emit_tree(this, STATICTREES, last);
            this.blockStart = this.strStart;
            this.blockOpen = true;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void QuickEndBlock(Trees.CodeData* lTree, bool last)
        {
            if (this.blockOpen)
            {
                Trees.Tr_emit_end_block(this, lTree, last);
                this.blockStart = this.strStart;
                this.blockOpen = false;
            }
        }
    }
}
