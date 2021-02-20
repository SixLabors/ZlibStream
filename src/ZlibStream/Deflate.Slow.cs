// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ZlibStream
{
    /// <content>
    /// Contains the slow deflate implementation.
    /// </content>
    internal sealed unsafe partial class Deflate
    {
        /// <summary>
        /// Same as fast, but achieves better compression. We use a lazy
        /// evaluation for matches: a match is finally adopted only if there is
        /// no better match at the next window position.
        /// </summary>
        /// <param name="flush">The flush strategy.</param>
        /// <returns>The <see cref="int"/>.</returns>
        private int DeflateSlow(FlushStrategy flush)
        {
            int hash_head = 0; // head of hash chain
            bool bflush; // set if current block must be flushed

            byte* window = this.DynamicBuffers.WindowPointer;
            ushort* head = this.DynamicBuffers.HeadPointer;
            ushort* prev = this.DynamicBuffers.PrevPointer;

            // Process the input block.
            while (true)
            {
                // Make sure that we always have enough lookahead, except
                // at the end of the input file. We need MAX_MATCH bytes
                // for the next match, plus MINMATCH bytes to insert the
                // string following the next match.
                if (this.lookahead < MINLOOKAHEAD)
                {
                    this.Fill_window();
                    if (this.lookahead < MINLOOKAHEAD && flush == FlushStrategy.NoFlush)
                    {
                        return NeedMore;
                    }

                    if (this.lookahead == 0)
                    {
                        break; // flush the current block
                    }
                }

                // Insert the string window[strstart .. strstart+2] in the
                // dictionary, and set hash_head to the head of the hash chain:
                if (this.lookahead >= MINMATCH)
                {
                    hash_head = this.InsertString(prev, head, window, this.strStart);
                }

                // Find the longest match, discarding those <= prev_length.
                this.prevLength = this.matchLength;
                this.prevMatch = this.matchStart;
                this.matchLength = MINMATCH - 1;

                if (hash_head != 0 && this.prevLength < this.maxLazyMatch
                    && this.strStart - hash_head <= this.wSize - MINLOOKAHEAD)
                {
                    // To simplify the code, we prevent matches with the string
                    // of window index 0 (in particular we have to avoid a match
                    // of the string with itself at the start of the input file).
                    if (this.strategy != CompressionStrategy.HuffmanOnly)
                    {
                        this.matchLength = this.Longest_match(hash_head);
                    }

                    // longest_match() sets match_start
                    if (this.matchLength <= 5 && (this.strategy == CompressionStrategy.Filtered
                        || (this.matchLength == MINMATCH && this.strStart - this.matchStart > 4096)))
                    {
                        // If prev_match is also MINMATCH, match_start is garbage
                        // but we will ignore the current match anyway.
                        this.matchLength = MINMATCH - 1;
                    }
                }

                // If there was a match at the previous step and the current
                // match is not better, output the previous match:
                if (this.prevLength >= MINMATCH && this.matchLength <= this.prevLength)
                {
                    int max_insert = this.strStart + this.lookahead - MINMATCH;

                    // Do not insert strings in hash table beyond this.

                    // check_match(strstart-1, prev_match, prev_length);
                    bflush = this.Tr_tally_dist(this.strStart - 1 - this.prevMatch, this.prevLength - MINMATCH);

                    // Insert in hash table all strings up to the end of the match.
                    // strstart-1 and strstart are already inserted. If there is not
                    // enough lookahead, the last two strings are not inserted in
                    // the hash table.
                    this.lookahead -= this.prevLength - 1;
                    this.prevLength -= 2;
                    do
                    {
                        if (++this.strStart <= max_insert)
                        {
                            hash_head = this.InsertString(prev, head, window, this.strStart);
                        }
                    }
                    while (--this.prevLength != 0);
                    this.matchAvailable = 0;
                    this.matchLength = MINMATCH - 1;
                    this.strStart++;

                    if (bflush)
                    {
                        this.Flush_block_only(false);
                        if (this.strm.AvailOut == 0)
                        {
                            return NeedMore;
                        }
                    }
                }
                else if (this.matchAvailable != 0)
                {
                    // If there was no match at the previous position, output a
                    // single literal. If there was a match but the current match
                    // is longer, truncate the previous match to a single literal.
                    bflush = this.Tr_tally_lit(window[this.strStart - 1]);

                    if (bflush)
                    {
                        this.Flush_block_only(false);
                    }

                    this.strStart++;
                    this.lookahead--;
                    if (this.strm.AvailOut == 0)
                    {
                        return NeedMore;
                    }
                }
                else
                {
                    // There is no previous match to compare with, wait for
                    // the next step to decide.
                    this.matchAvailable = 1;
                    this.strStart++;
                    this.lookahead--;
                }
            }

            if (this.matchAvailable != 0)
            {
                _ = this.Tr_tally_lit(window[this.strStart - 1]);
                this.matchAvailable = 0;
            }

            this.Flush_block_only(flush == FlushStrategy.Finish);

            return this.strm.AvailOut == 0
                ? flush == FlushStrategy.Finish ? FinishStarted : NeedMore
                : flush == FlushStrategy.Finish ? FinishDone : BlockDone;
        }
    }
}
