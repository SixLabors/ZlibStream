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
                    }

                    do
                    {
                        if (this.Pending + 4 >= this.pendingBufferSize)
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
                                this.Tr_emit_end_block(StaticTree.StaticLtree);
                                this.blockStart = this.strStart;
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

                            if (dist > 0 && (dist - 1) < this.wSize - 1)
                            {
                                matchLen = this.Compare258(window + this.strStart, window + hash_head);

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

                                    int lc = matchLen - MINMATCH;
                                    int code = Tree.LengthCode[lc];

                                    this.Send_code(code + LITERALS + 1, ltree); // send the length code
                                    int extra = Tree.ExtraLbits[code];
                                    if (extra != 0)
                                    {
                                        lc -= Tree.BaseLength[code];
                                        this.Send_bits(lc, extra); // send the extra length bits
                                    }

                                    dist--; // dist is now the match distance - 1
                                    code = Tree.D_code(dist);

                                    this.Send_code(code, dtree); // send the distance code
                                    extra = Tree.ExtraDbits[code];
                                    if (extra != 0)
                                    {
                                        dist -= Tree.BaseDist[code];
                                        this.Send_bits(dist, extra); // send the extra distance bits
                                    }

                                    this.lookahead = matchLen;
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
                    this.Tr_emit_end_block(StaticTree.StaticLtree);
                    this.blockStart = this.strStart;
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
        private int Compare258(byte* src0, byte* src1)
        {
            int len = 0;
            do
            {
                if (*(ushort*)src0 != *(ushort*)src1)
                {
                    return len + ((*src0 == *src1) ? 1 : 0);
                }

                src0 += 2;
                src1 += 2;
                len += 2;

                if (*(ushort*)src0 != *(ushort*)src1)
                {
                    return len + ((*src0 == *src1) ? 1 : 0);
                }

                src0 += 2;
                src1 += 2;
                len += 2;

                if (*(ushort*)src0 != *(ushort*)src1)
                {
                    return len + ((*src0 == *src1) ? 1 : 0);
                }

                src0 += 2;
                src1 += 2;
                len += 2;

                if (*(ushort*)src0 != *(ushort*)src1)
                {
                    return len + ((*src0 == *src1) ? 1 : 0);
                }

                src0 += 2;
                src1 += 2;
                len += 2;
            }
            while (len < 256);

            src0 += 2;
            src1 += 2;
            len += 2;

            if (*(ushort*)src0 != *(ushort*)src1)
            {
                return len + ((*src0 == *src1) ? 1 : 0);
            }

            return len;
        }
    }
}
