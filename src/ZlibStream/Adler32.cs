// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if SUPPORTS_RUNTIME_INTRINSICS
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace SixLabors.ZlibStream
{
    internal static unsafe class Adler32
    {
        // Largest prime smaller than 65536
        private const uint BASE = 65521;

        // NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
        private const uint NMAX = 5552;

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        public static uint Calculate(uint adler, byte[] buffer, int index, int length)
        {
#if SUPPORTS_RUNTIME_INTRINSICS
            if (Sse3.IsSupported && length >= 64)
            {
                return CalculateSse(adler, buffer, index, (uint)length);
            }

            return CalculateScalar(adler, buffer, index, (uint)length);
#else
            return CalculateScalar(adler, buffer, index, (uint)length);
#endif
        }

        // TODO: Get vectorized solution working. The solution below is based on link.
        // It currently fails tests.
        // https://github.com/chromium/chromium/blob/master/third_party/zlib/adler32_simd.c
#if SUPPORTS_RUNTIME_INTRINSICS
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        public static uint CalculateSse(uint adler, byte[] buffer, int index, uint length)
        {
            if (buffer is null)
            {
                return 1U;
            }

            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            // Process the data in blocks.
            const int BLOCK_SIZE = 1 << 5;

            uint blocks = length / BLOCK_SIZE;
            length -= blocks * BLOCK_SIZE;

            fixed (byte* bufferPtr = &buffer[index])
            {
                index += (int)blocks * BLOCK_SIZE;
                var localBufferPtr = bufferPtr;

                // _mm_setr_epi8 on x86
                var tap1 = Vector128.Create(32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17);
                var tap2 = Vector128.Create(16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1);
                Vector128<byte> zero = Vector128<byte>.Zero;
                var ones = Vector128.Create((short)1);

                while (blocks > 0)
                {
                    uint n = NMAX / BLOCK_SIZE;  /* The NMAX constraint. */
                    if (n > blocks)
                    {
                        n = blocks;
                    }

                    blocks -= n;

                    // Process n blocks of data. At most NMAX data bytes can be
                    // processed before s2 must be reduced modulo BASE.
                    // These overloads of Create do not use _mm_setr_epi8 on x86 so must be reversed.
                    Vector128<int> v_ps = Vector128.CreateScalar(s1 * n).AsInt32();
                    Vector128<int> v_s2 = Vector128.CreateScalar(s2).AsInt32();
                    Vector128<int> v_s1 = Vector128<int>.Zero;

                    do
                    {
                        // Load 32 input bytes.
                        Vector128<byte> bytes1 = Sse3.LoadDquVector128(localBufferPtr);
                        Vector128<byte> bytes2 = Sse3.LoadDquVector128(localBufferPtr + 16);

                        // Add previous block byte sum to v_ps.
                        v_ps = Sse2.Add(v_ps, v_s1);

                        // Horizontally add the bytes for s1, multiply-adds the
                        // bytes by [ 32, 31, 30, ... ] for s2.
                        v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes1, zero).AsInt32());
                        Vector128<short> mad1 = Ssse3.MultiplyAddAdjacent(bytes1, tap1);
                        v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad1, ones));

                        v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes2, zero).AsInt32());
                        Vector128<short> mad2 = Ssse3.MultiplyAddAdjacent(bytes2, tap2);
                        v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad2, ones));

                        localBufferPtr += BLOCK_SIZE;
                    }
                    while (--n > 0);

                    v_s2 = Sse2.Add(v_s2, Sse2.ShiftLeftLogical(v_ps, 5));

                    // Sum epi32 ints v_s1(s2) and accumulate in s1(s2).
                    const byte S2301 = 0b1011_0001;  /* A B C D -> B A D C */
                    const byte S1032 = 0b0100_1110;  /* A B C D -> C D A B */

                    v_s1 = Sse2.Add(v_s1, Sse2.Shuffle(v_s1, S2301));
                    v_s1 = Sse2.Add(v_s1, Sse2.Shuffle(v_s1, S1032));

                    s1 += (uint)v_s1.ToScalar();

                    v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, S2301));
                    v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, S1032));

                    s2 = (uint)v_s2.ToScalar();

                    // Reduce.
                    s1 %= BASE;
                    s2 %= BASE;
                }
            }

            ref byte bufferRef = ref MemoryMarshal.GetReference<byte>(buffer);

            if (length > 0)
            {
                if (length >= 16)
                {
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    length -= 16;
                }

                while (length-- > 0)
                {
                    s2 += s1 += Unsafe.Add(ref bufferRef, index++);
                }

                if (s1 >= BASE)
                {
                    s1 -= BASE;
                }

                s2 %= BASE;
            }

            return s1 | (s2 << 16);
        }
#endif

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        public static uint CalculateScalar(uint adler, byte[] buffer, int index, uint length)
        {
            if (buffer is null)
            {
                return 1U;
            }

            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;
            uint k;

            ref byte bufferRef = ref MemoryMarshal.GetReference<byte>(buffer);

            while (length > 0)
            {
                k = length < NMAX ? length : NMAX;
                length -= k;

                while (k >= 16)
                {
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    s1 += Unsafe.Add(ref bufferRef, index++);
                    s2 += s1;
                    k -= 16;
                }

                if (k != 0)
                {
                    do
                    {
                        s1 += Unsafe.Add(ref bufferRef, index++);
                        s2 += s1;
                    }
                    while (--k != 0);
                }

                s1 %= BASE;
                s2 %= BASE;
            }

            return (s2 << 16) | s1;
        }
    }
}
