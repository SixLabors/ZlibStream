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
        internal int DeflateQuick(ZlibFlushStrategy flush)
        {
            int hash_head; // head of the hash chain
            int dist;
            int matchLen;
            bool last;

            byte* window = this.windowPointer;
            ushort* head = this.headPointer;
            ushort* prev = this.prevPointer;

            fixed (ushort* ltree = StaticTree.StaticLtree)
            {
                fixed (ushort* dtree = StaticTree.StaticDtree)
                {
                    if (!this.blockOpen && this.lookahead > 0)
                    {
                        // Start new block when we have lookahead data, so that if no
                        // input data is given an empty block will not be written.
                        last = flush == ZlibFlushStrategy.ZFINISH;
                        this.QuickStartBlock(last);
                    }

                    do
                    {
                        if (this.Pending + 4 >= this.pendingBufferSize)
                        {
                            this.Flush_pending(this.strm);
                            if (this.strm.AvailIn == 0 && flush != ZlibFlushStrategy.ZFINISH)
                            {
                                // Break to emit end block and return need_more
                                break;
                            }
                        }

                        if (this.lookahead < MINLOOKAHEAD)
                        {
                            this.Fill_window();
                            if (this.lookahead < MINLOOKAHEAD && flush == ZlibFlushStrategy.ZNOFLUSH)
                            {
                                // Always emit end block, in case next call is with Z_FINISH
                                // and we need to emit start of last block
                                this.QuickEndBlock(false);
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
                                last = flush == ZlibFlushStrategy.ZFINISH;
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

                                    this.Tr_emit_distance(ltree, dtree, matchLen - MINMATCH, dist);
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

                    last = flush == ZlibFlushStrategy.ZFINISH;
                    this.QuickEndBlock(last);
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
            this.Tr_emit_tree(STATICTREES, last);
            this.blockStart = this.strStart;
            this.blockOpen = true;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void QuickEndBlock(bool last)
        {
            this.Tr_emit_end_block(StaticTree.StaticLtree, last);
            this.blockStart = this.strStart;
            this.blockOpen = false;
        }
    }
}
