// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ZlibStream
{
    /// <content>
    /// Contains the RLE deflate implementation.
    /// </content>
    internal sealed unsafe partial class Deflate
    {
        /// <summary>
        /// For Z_RLE, simply look for runs of bytes, generate matches only of distance
        /// one.Do not maintain a hash table.  (It will be regenerated if this run of
        /// deflate switches away from Z_RLE.
        /// </summary>
        /// <param name="flush">The flush strategy.</param>
        /// <returns>The <see cref="int"/>.</returns>
        internal int DeflateRle(FlushStrategy flush)
        {
            bool bflush; // set if current block must be flushed
            int prev; // byte at distance one to match
            byte* scan; // scan goes up to strend for length of run
            byte* strend;
            byte* window = this.DynamicBuffers.WindowPointer;

            while (true)
            {
                // Make sure that we always have enough lookahead, except
                // at the end of the input file. We need MAX_MATCH bytes
                // for the longest run, plus one for the unrolled loop.
                if (this.lookahead <= MAXMATCH)
                {
                    this.Fill_window();
                    if (this.lookahead <= MAXMATCH && flush == FlushStrategy.NoFlush)
                    {
                        return NeedMore;
                    }
                }

                if (this.lookahead == 0)
                {
                    break;
                }

                // See how many times the previous byte repeats
                this.matchLength = 0;
                if (this.lookahead >= MINMATCH && this.strStart > 0)
                {
                    scan = window + this.strStart - 1;
                    prev = *scan;

                    if (prev == *++scan && prev == *++scan && prev == *++scan)
                    {
                        strend = window + this.strStart + MAXMATCH;
                        do
                        {
                        }
                        while (prev == *++scan && prev == *++scan
                            && prev == *++scan && prev == *++scan
                            && prev == *++scan && prev == *++scan
                            && prev == *++scan && prev == *++scan
                            && scan < strend);

                        this.matchLength = MAXMATCH - (int)(strend - scan);

                        if (this.matchLength > this.lookahead)
                        {
                            this.matchLength = this.lookahead;
                        }
                    }
                }

                // Emit match if have run of MIN_MATCH or longer, else emit literal
                if (this.matchLength >= MINMATCH)
                {
                    bflush = this.Tr_tally_dist(1, this.matchLength - MINMATCH);

                    this.lookahead -= this.matchLength;
                    this.strStart += this.matchLength;
                    this.matchLength = 0;
                }
                else
                {
                    // No match, output a literal byte
                    bflush = this.Tr_tally_lit(window[this.strStart]);
                    this.lookahead--;
                    this.strStart++;
                }

                if (bflush)
                {
                    this.Flush_block_only(false);
                    if (this.strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }
            }

            this.Flush_block_only(flush == FlushStrategy.Finish);
            return this.strm.AvailOut == 0
                ? flush == FlushStrategy.Finish ? FinishStarted : NeedMore
                : flush == FlushStrategy.Finish ? FinishDone : BlockDone;
        }
    }
}
