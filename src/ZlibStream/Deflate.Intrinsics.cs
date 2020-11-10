// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Numerics;
using System.Runtime.CompilerServices;
#if SUPPORTS_RUNTIME_INTRINSICS
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace SixLabors.ZlibStream
{
    /// <content>
    /// Contains methods backed with intrinsics.
    /// </content>
    internal sealed unsafe partial class Deflate
    {
        [MethodImpl(InliningOptions.ShortMethod)]
        private static int Compare256(byte* src0, byte* src1)
        {
#if SUPPORTS_RUNTIME_INTRINSICS
            if (Avx2.IsSupported)
            {
                return Compare256Avx2(src0, src1);
            }
            else if (Sse2.IsSupported)
            {
                return Compare256Sse2(src0, src1);
            }
            else
#endif
            {
                return Compare256Scalar(src0, src1);
            }
        }

#if SUPPORTS_RUNTIME_INTRINSICS
        [MethodImpl(InliningOptions.ShortMethod)]
        private static int Compare256Avx2(byte* src0, byte* src1)
        {
            int len = 0;

            do
            {
                Vector256<byte> ymm_src0 = Avx.LoadVector256(src0);
                Vector256<byte> ymm_src1 = Avx.LoadVector256(src1);

                // non-identical bytes = 00, identical bytes = FF
                Vector256<byte> ymm_cmp = Avx2.CompareEqual(ymm_src0, ymm_src1);

                int mask = Avx2.MoveMask(ymm_cmp);
                if ((uint)mask != uint.MaxValue)
                {
                    // Invert bits so identical = 0
                    int match_byte = BitOperations.TrailingZeroCount(~mask);
                    return len + match_byte;
                }

                ymm_src0 = Avx.LoadVector256(src0 + 32);
                ymm_src1 = Avx.LoadVector256(src1 + 32);
                ymm_cmp = Avx2.CompareEqual(ymm_src0, ymm_src1);

                mask = Avx2.MoveMask(ymm_cmp);
                if ((uint)mask != uint.MaxValue)
                {
                    int match_byte = BitOperations.TrailingZeroCount(~mask);
                    return len + 32 + match_byte;
                }

                src0 += 64;
                src1 += 64;
                len += 64;
            }
            while (len < 256);

            return 256;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static int Compare256Sse2(byte* src0, byte* src1)
        {
            int len = 0;

            do
            {
                Vector128<byte> ymm_src0 = Sse2.LoadVector128(src0);
                Vector128<byte> ymm_src1 = Sse2.LoadVector128(src1);

                // non-identical bytes = 00, identical bytes = FF
                Vector128<byte> ymm_cmp = Sse2.CompareEqual(ymm_src0, ymm_src1);

                int mask = Sse2.MoveMask(ymm_cmp);
                if ((ushort)mask != ushort.MaxValue)
                {
                    // Invert bits so identical = 0
                    int match_byte = BitOperations.TrailingZeroCount(~mask);
                    return len + match_byte;
                }

                ymm_src0 = Sse2.LoadVector128(src0 + 16);
                ymm_src1 = Sse2.LoadVector128(src1 + 16);
                ymm_cmp = Sse2.CompareEqual(ymm_src0, ymm_src1);

                mask = Sse2.MoveMask(ymm_cmp);
                if ((uint)mask != uint.MaxValue)
                {
                    int match_byte = BitOperations.TrailingZeroCount(~mask);
                    return len + 16 + match_byte;
                }

                src0 += 32;
                src1 += 32;
                len += 32;
            }
            while (len < 256);

            return 256;
        }
#endif

        [MethodImpl(InliningOptions.ShortMethod)]
        private static int Compare256Scalar(byte* src0, byte* src1)
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
            return 256;
        }

        /// <summary>
        /// Slide the hash table (could be avoided with 32 bit values
        /// at the expense of memory usage). We slide even when level == 0
        /// to keep the hash table consistent if we switch back to level > 0
        /// later. (Using level 0 permanently is not an optimal usage of
        /// zlib, so we don't care about this pathological case.)
        /// </summary>
        /// <param name="head">Heads of the hash chains or NIL.</param>
        /// <param name="prev">Link to older string with same hash index.</param>
        [MethodImpl(InliningOptions.ShortMethod)]
        private void SlideHash(ushort* head, ushort* prev)
        {
#if SUPPORTS_RUNTIME_INTRINSICS
            if (Avx2.IsSupported)
            {
                this.SlideHashAvx2(head, prev);
            }
            else if (Sse2.IsSupported)
            {
                this.SlideHashSse2(head, prev);
            }
            else
#endif
            {
                this.SlideHashScalar(head, prev);
            }
        }

#if SUPPORTS_RUNTIME_INTRINSICS
        [MethodImpl(InliningOptions.ShortMethod)]
        private void SlideHashAvx2(ushort* head, ushort* prev)
        {
            ushort wsize = (ushort)this.wSize;
            var xmm_wsize = Vector256.Create(wsize);

            int n = this.hashSize;
            ushort* p = &head[n] - 16;
            do
            {
                Vector256<ushort> value = Avx.LoadVector256(p);
                Vector256<ushort> result = Avx2.SubtractSaturate(value, xmm_wsize);
                Avx.Store(p, result);

                p -= 16;
                n -= 16;
            }
            while (n > 0);

            n = wsize;
            p = &prev[n] - 16;
            do
            {
                Vector256<ushort> value = Avx.LoadVector256(p);
                Vector256<ushort> result = Avx2.SubtractSaturate(value, xmm_wsize);
                Avx.Store(p, result);

                p -= 16;
                n -= 16;
            }
            while (n > 0);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private void SlideHashSse2(ushort* head, ushort* prev)
        {
            ushort wsize = (ushort)this.wSize;
            var xmm_wsize = Vector128.Create(wsize);

            int n = this.hashSize;
            ushort* p = &head[n] - 8;
            do
            {
                Vector128<ushort> value = Sse2.LoadVector128(p);
                Vector128<ushort> result = Sse2.SubtractSaturate(value, xmm_wsize);
                Sse2.Store(p, result);

                p -= 8;
                n -= 8;
            }
            while (n > 0);

            n = wsize;
            p = &prev[n] - 8;
            do
            {
                Vector128<ushort> value = Sse2.LoadVector128(p);
                Vector128<ushort> result = Sse2.SubtractSaturate(value, xmm_wsize);
                Sse2.Store(p, result);

                p -= 8;
                n -= 8;
            }
            while (n > 0);
        }
#endif

        [MethodImpl(InliningOptions.ShortMethod)]
        private void SlideHashScalar(ushort* head, ushort* prev)
        {
            int wsize = this.wSize;
            int n = this.hashSize;
            int p = n;
            int m;
            do
            {
                m = head[--p];
                head[p] = (ushort)(m >= wsize ? (m - wsize) : 0);
            }
            while (--n != 0);

            n = wsize;
            p = n;
            do
            {
                m = prev[--p];
                prev[p] = (ushort)(m >= wsize ? (m - wsize) : 0);

                // If n is not on any hash chain, prev[n] is garbage but
                // its value will never be used.
            }
            while (--n != 0);
        }

        /// <summary>
        /// Update a hash value with the given input byte
        /// IN  assertion: all calls to UPDATE_HASH are made with consecutive input
        /// characters, so that a running hash key can be computed from the previous
        /// key instead of complete recalculation each time.
        /// </summary>
        /// <param name="val">The input byte.</param>
        [MethodImpl(InliningOptions.ShortMethod)]
        private uint UpdateHash(uint val)
        {
#if SUPPORTS_RUNTIME_INTRINSICS
            if (Sse42.IsSupported)
            {
                return Sse42.Crc32(0U, val);
            }
            else
#endif
            {
                return (val * 2654435761U) >> 16;
            }
        }
    }
}
