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
        /// Compress as much as possible from the input stream, return the current
        /// block state.
        /// This function does not perform lazy evaluation of matches and inserts
        /// new strings in the dictionary only for unmatched strings or for ushort
        /// matches. It is used only for the fast compression options.
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
                    if (!this.blockOpen)
                    {
                        last = flush == ZlibFlushStrategy.ZFINISH;
                        this.Tr_emit_tree(STATICTREES, last);
                        this.blockOpen = true;
                    }

                    do
                    {
                        if (this.Pending + 8 >= this.pendingBufferSize)
                        {
                            this.Flush_pending(this.strm);
                            if (this.strm.AvailIn == 0 && flush != ZlibFlushStrategy.ZFINISH)
                            {
                                return NeedMore;
                            }
                        }

                        if (this.lookahead < MINLOOKAHEAD)
                        {
                            this.Fill_window();
                            if (this.lookahead < MINLOOKAHEAD && flush == ZlibFlushStrategy.ZNOFLUSH)
                            {
                                this.Tr_emit_end_block(StaticTree.StaticLtree, false);
                                this.blockStart = this.strStart;
                                this.blockOpen = false;
                                this.Flush_pending(this.strm);
                                return NeedMore;
                            }

                            if (this.lookahead == 0)
                            {
                                break;
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

                                    if (matchLen > MAXMATCH)
                                    {
                                        matchLen = MAXMATCH;
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

                    if (this.strm.AvailOut == 0 && flush != ZlibFlushStrategy.ZFINISH)
                    {
                        return NeedMore;
                    }

                    last = flush == ZlibFlushStrategy.ZFINISH;
                    this.Tr_emit_end_block(StaticTree.StaticLtree, last);
                    this.blockStart = this.strStart;
                    this.blockOpen = false;
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
    }
}
