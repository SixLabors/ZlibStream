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
    /// <summary>
    /// Contains methods backs with intrinsics.
    /// </summary>
    internal sealed unsafe partial class Deflate
    {
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
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

            return Compare256Scalar(src0, src1);
#else
            return Compare256Scalar(src0, src1);
#endif
        }

#if SUPPORTS_RUNTIME_INTRINSICS
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
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

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
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

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
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
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private void SlideHash(ushort* head, ushort* prev)
        {
#if SUPPORTS_RUNTIME_INTRINSICS
            if (Avx2.IsSupported)
            {
                this.SlideHashAvx2(head, prev);
                return;
            }
            else if (Sse2.IsSupported)
            {
                this.SlideHashSse2(head, prev);
                return;
            }

            this.SlideHashScalar(head, prev);
#else
            this.SlideHashScalar(head, prev);
#endif
        }

#if SUPPORTS_RUNTIME_INTRINSICS
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
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

            n = this.wSize;
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

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
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

            n = this.wSize;
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

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private void SlideHashScalar(ushort* head, ushort* prev)
        {
            int n = this.hashSize;
            int p = n;
            int m;
            do
            {
                m = head[--p];
                head[p] = (ushort)(m >= this.wSize ? (m - this.wSize) : 0);
            }
            while (--n != 0);

            n = this.wSize;
            p = n;
            do
            {
                m = prev[--p];
                prev[p] = (ushort)(m >= this.wSize ? (m - this.wSize) : 0);

                // If n is not on any hash chain, prev[n] is garbage but
                // its value will never be used.
            }
            while (--n != 0);
        }
    }
}
