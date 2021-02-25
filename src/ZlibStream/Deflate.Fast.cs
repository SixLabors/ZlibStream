// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ZlibStream
{
    /// <content>
    /// Contains the fast deflate implementation.
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
        internal int DeflateFast(FlushStrategy flush)
        {
            int hash_head; // head of the hash chain
            bool bflush; // set if current block must be flushed

            byte* window = this.DynamicBuffers.WindowPointer;
            ushort* head = this.DynamicBuffers.HeadPointer;
            ushort* prev = this.DynamicBuffers.PrevPointer;

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
                hash_head = 0;
                if (this.lookahead >= MINMATCH)
                {
                    hash_head = this.InsertString(prev, head, window, this.strStart);
                }

                // Find the longest match, discarding those <= prev_length.
                // At this point we have always match_length < MINMATCH
                if (hash_head != 0 && (this.strStart - hash_head) <= this.wSize - MINLOOKAHEAD)
                {
                    // To simplify the code, we prevent matches with the string
                    // of window index 0 (in particular we have to avoid a match
                    // of the string with itself at the start of the input file).
                    if (this.strategy != CompressionStrategy.HuffmanOnly)
                    {
                        this.matchLength = this.Longest_match(hash_head);

                        // longest_match() sets match_start
                    }
                }

                if (this.matchLength >= MINMATCH)
                {
                    // check_match(strstart, match_start, match_length);
                    bflush = this.Tr_tally_dist(this.strStart - this.matchStart, this.matchLength - MINMATCH);

                    this.lookahead -= this.matchLength;

                    // Insert new strings in the hash table only if the match length
                    // is not too large. This saves time but degrades compression.
                    if (this.matchLength <= this.maxLazyMatch && this.lookahead >= MINMATCH)
                    {
                        this.matchLength--; // string at strstart already in hash table
                        do
                        {
                            this.strStart++;
                            this.InsertString(prev, head, window, this.strStart);

                            // strstart never exceeds WSIZE-MAX_MATCH, so there are
                            // always MINMATCH bytes ahead.
                        }
                        while (--this.matchLength != 0);
                        this.strStart++;
                    }
                    else
                    {
                        this.strStart += this.matchLength;
                        this.matchLength = 0;

                        this.UpdateHash(window[this.strStart + 1]);

                        // If lookahead < MINMATCH, insH is garbage, but it does not
                        // matter since it will be recomputed at next deflate call.
                    }
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
                    if (this.strm.AvailableOut == 0)
                    {
                        return NeedMore;
                    }
                }
            }

            this.Flush_block_only(flush == FlushStrategy.Finish);
            return this.strm.AvailableOut == 0
                ? flush == FlushStrategy.Finish ? FinishStarted : NeedMore
                : flush == FlushStrategy.Finish ? FinishDone : BlockDone;
        }
    }
}
