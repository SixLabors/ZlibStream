// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Runtime.CompilerServices;
#if SUPPORTS_RUNTIME_INTRINSICS
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

#pragma warning disable IDE0007 // Use implicit type
namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Calculates the 32 bit Adler checksum of a given buffer according to
    /// RFC 1950. ZLIB Compressed Data Format Specification version 3.3)
    /// </summary>
    internal static class Adler32
    {
        /// <summary>
        /// The default initial seed value of a Adler32 checksum calculation.
        /// </summary>
        public const uint SeedValue = 1U;

        // Largest prime smaller than 65536
        private const uint BASE = 65521;

        // NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
        private const uint NMAX = 5552;

#if SUPPORTS_RUNTIME_INTRINSICS
        private const int MinBufferSize = 64;

        // The C# compiler emits this as a compile-time constant embedded in the PE file.
        private static ReadOnlySpan<byte> Tap1Tap2 => new byte[]
        {
            64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 49, // tap1
            48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, // tap2
            32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, // tap1
            16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 // tap2
        };
#endif

        /// <summary>
        /// Calculates the Adler32 checksum with the bytes taken from the span.
        /// </summary>
        /// <param name="buffer">The readonly span of bytes.</param>
        /// <returns>The <see cref="uint"/>.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        public static uint Calculate(ReadOnlySpan<byte> buffer)
            => Calculate(SeedValue, buffer);

        /// <summary>
        /// Calculates the Adler32 checksum with the bytes taken from the span and seed.
        /// </summary>
        /// <param name="adler">The input Adler32 value.</param>
        /// <param name="buffer">The readonly span of bytes.</param>
        /// <returns>The <see cref="uint"/>.</returns>
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        public static uint Calculate(uint adler, ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return adler;
            }

#if SUPPORTS_RUNTIME_INTRINSICS
            if (Avx2.IsSupported && buffer.Length >= MinBufferSize)
            {
                return CalculateAvx2(adler, buffer);
            }

            if (Ssse3.IsSupported && buffer.Length >= MinBufferSize)
            {
                return CalculateSse3(adler, buffer);
            }

            return CalculateScalar(adler, buffer);
#else
            return CalculateScalar(adler, buffer);
#endif
        }

        // Based on https://github.com/chromium/chromium/blob/master/third_party/zlib/adler32_simd.c
#if SUPPORTS_RUNTIME_INTRINSICS
        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private static unsafe uint CalculateAvx2(uint adler, ReadOnlySpan<byte> buffer)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            // Process the data in blocks.
            const int BLOCK_SIZE = 64;

            uint length = (uint)buffer.Length;
            uint blocks = length / BLOCK_SIZE;
            length -= blocks * BLOCK_SIZE;

            int index = 0;
            fixed (byte* bufferPtr = buffer)
            fixed (byte* tapPtr = Tap1Tap2)
            {
                index += (int)blocks * BLOCK_SIZE;
                var localBufferPtr = bufferPtr;

                // _mm_setr_epi8 on x86
                Vector256<sbyte> tap1 = Avx.LoadVector256((sbyte*)tapPtr);
                Vector256<sbyte> tap2 = Avx.LoadVector256((sbyte*)(tapPtr + 0x20));
                Vector256<byte> zero = Vector256<byte>.Zero;
                var ones = Vector256.Create((short)1);

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
                    Vector256<uint> v_ps = Vector256.CreateScalar(s1 * n);
                    Vector256<uint> v_s2 = Vector256.CreateScalar(s2);
                    Vector256<uint> v_s1 = Vector256<uint>.Zero;

                    do
                    {
                        // Load 64 input bytes.
                        Vector256<byte> bytes1 = Avx.LoadDquVector256(localBufferPtr);
                        Vector256<byte> bytes2 = Avx.LoadDquVector256(localBufferPtr + 0x20);

                        // Add previous block byte sum to v_ps.
                        v_ps = Avx2.Add(v_ps, v_s1);

                        // Horizontally add the bytes for s1, multiply-adds the
                        // bytes by [ 32, 31, 30, ... ] for s2.
                        v_s1 = Avx2.Add(v_s1, Avx2.SumAbsoluteDifferences(bytes1, zero).AsUInt32());
                        Vector256<short> mad1 = Avx2.MultiplyAddAdjacent(bytes1, tap1);
                        v_s2 = Avx2.Add(v_s2, Avx2.MultiplyAddAdjacent(mad1, ones).AsUInt32());

                        v_s1 = Avx2.Add(v_s1, Avx2.SumAbsoluteDifferences(bytes2, zero).AsUInt32());
                        Vector256<short> mad2 = Avx2.MultiplyAddAdjacent(bytes2, tap2);
                        v_s2 = Avx2.Add(v_s2, Avx2.MultiplyAddAdjacent(mad2, ones).AsUInt32());

                        localBufferPtr += BLOCK_SIZE;
                    }
                    while (--n > 0);

                    v_s2 = Avx2.Add(v_s2, Avx2.ShiftLeftLogical(v_ps, 6));

                    // Sum epi32 ints v_s1(s2) and accumulate in s1(s2).
                    const byte S2301 = 0b1011_0001;  // A B C D -> B A D C
                    const byte S1032 = 0b0100_1110;  // A B C D -> C D A B

                    v_s1 = Avx2.Add(v_s1, Avx2.Shuffle(v_s1, S1032));

                    s1 += Sse2.ConvertToUInt32(Sse2.Add(v_s1.GetLower(), v_s1.GetUpper()));

                    v_s2 = Avx2.Add(v_s2, Avx2.Shuffle(v_s2, S2301));
                    v_s2 = Avx2.Add(v_s2, Avx2.Shuffle(v_s2, S1032));

                    s2 = Sse2.ConvertToUInt32(Sse2.Add(v_s2.GetLower(), v_s2.GetUpper()));

                    // Reduce.
                    s1 %= BASE;
                    s2 %= BASE;
                }

                if (length > 0)
                {
                    if (length >= 16)
                    {
                        s2 += s1 += localBufferPtr[0];
                        s2 += s1 += localBufferPtr[1];
                        s2 += s1 += localBufferPtr[2];
                        s2 += s1 += localBufferPtr[3];
                        s2 += s1 += localBufferPtr[4];
                        s2 += s1 += localBufferPtr[5];
                        s2 += s1 += localBufferPtr[6];
                        s2 += s1 += localBufferPtr[7];
                        s2 += s1 += localBufferPtr[8];
                        s2 += s1 += localBufferPtr[9];
                        s2 += s1 += localBufferPtr[10];
                        s2 += s1 += localBufferPtr[11];
                        s2 += s1 += localBufferPtr[12];
                        s2 += s1 += localBufferPtr[13];
                        s2 += s1 += localBufferPtr[14];
                        s2 += s1 += localBufferPtr[15];

                        localBufferPtr += 16;
                        length -= 16;
                    }

                    while (length-- > 0)
                    {
                        s2 += s1 += *localBufferPtr++;
                    }

                    if (s1 >= BASE)
                    {
                        s1 -= BASE;
                    }

                    s2 %= BASE;
                }

                return s1 | (s2 << 16);
            }
        }

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private static unsafe uint CalculateSse3(uint adler, ReadOnlySpan<byte> buffer)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;

            // Process the data in blocks.
            const int BLOCK_SIZE = 32;

            uint length = (uint)buffer.Length;
            uint blocks = length / BLOCK_SIZE;
            length -= blocks * BLOCK_SIZE;

            int index = 0;
            fixed (byte* bufferPtr = buffer)
            fixed (byte* tapPtr = Tap1Tap2.Slice(32))
            {
                index += (int)blocks * BLOCK_SIZE;
                var localBufferPtr = bufferPtr;

                // _mm_setr_epi8 on x86
                Vector128<sbyte> tap1 = Sse2.LoadVector128((sbyte*)tapPtr);
                Vector128<sbyte> tap2 = Sse2.LoadVector128((sbyte*)(tapPtr + 0x10));
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
                    Vector128<uint> v_ps = Vector128.CreateScalar(s1 * n);
                    Vector128<uint> v_s2 = Vector128.CreateScalar(s2);
                    Vector128<uint> v_s1 = Vector128<uint>.Zero;

                    do
                    {
                        // Load 32 input bytes.
                        Vector128<byte> bytes1 = Sse3.LoadDquVector128(localBufferPtr);
                        Vector128<byte> bytes2 = Sse3.LoadDquVector128(localBufferPtr + 0x10);

                        // Add previous block byte sum to v_ps.
                        v_ps = Sse2.Add(v_ps, v_s1);

                        // Horizontally add the bytes for s1, multiply-adds the
                        // bytes by [ 32, 31, 30, ... ] for s2.
                        v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes1, zero).AsUInt32());
                        Vector128<short> mad1 = Ssse3.MultiplyAddAdjacent(bytes1, tap1);
                        v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad1, ones).AsUInt32());

                        v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes2, zero).AsUInt32());
                        Vector128<short> mad2 = Ssse3.MultiplyAddAdjacent(bytes2, tap2);
                        v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad2, ones).AsUInt32());

                        localBufferPtr += BLOCK_SIZE;
                    }
                    while (--n > 0);

                    v_s2 = Sse2.Add(v_s2, Sse2.ShiftLeftLogical(v_ps, 5));

                    // Sum epi32 ints v_s1(s2) and accumulate in s1(s2).
                    const byte S2301 = 0b1011_0001;  // A B C D -> B A D C
                    const byte S1032 = 0b0100_1110;  // A B C D -> C D A B

                    v_s1 = Sse2.Add(v_s1, Sse2.Shuffle(v_s1, S1032));

                    // ToScalar isn't optimized pre .NET 5
                    // https://github.com/dotnet/runtime/pull/37882
                    s1 += Sse2.ConvertToUInt32(v_s1);

                    v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, S2301));
                    v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, S1032));

                    s2 = Sse2.ConvertToUInt32(v_s2);

                    // Reduce.
                    s1 %= BASE;
                    s2 %= BASE;
                }

                if (length > 0)
                {
                    if (length >= 16)
                    {
                        s2 += s1 += localBufferPtr[0];
                        s2 += s1 += localBufferPtr[1];
                        s2 += s1 += localBufferPtr[2];
                        s2 += s1 += localBufferPtr[3];
                        s2 += s1 += localBufferPtr[4];
                        s2 += s1 += localBufferPtr[5];
                        s2 += s1 += localBufferPtr[6];
                        s2 += s1 += localBufferPtr[7];
                        s2 += s1 += localBufferPtr[8];
                        s2 += s1 += localBufferPtr[9];
                        s2 += s1 += localBufferPtr[10];
                        s2 += s1 += localBufferPtr[11];
                        s2 += s1 += localBufferPtr[12];
                        s2 += s1 += localBufferPtr[13];
                        s2 += s1 += localBufferPtr[14];
                        s2 += s1 += localBufferPtr[15];

                        localBufferPtr += 16;
                        length -= 16;
                    }

                    while (length-- > 0)
                    {
                        s2 += s1 += *localBufferPtr++;
                    }

                    if (s1 >= BASE)
                    {
                        s1 -= BASE;
                    }

                    s2 %= BASE;
                }

                return s1 | (s2 << 16);
            }
        }
#endif

        [MethodImpl(InliningOptions.HotPath | InliningOptions.ShortMethod)]
        private static unsafe uint CalculateScalar(uint adler, ReadOnlySpan<byte> buffer)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = (adler >> 16) & 0xFFFF;
            uint k;

            fixed (byte* bufferPtr = buffer)
            {
                var localBufferPtr = bufferPtr;
                uint length = (uint)buffer.Length;

                while (length > 0)
                {
                    k = length < NMAX ? length : NMAX;
                    length -= k;

                    while (k >= 16)
                    {
                        s2 += s1 += localBufferPtr[0];
                        s2 += s1 += localBufferPtr[1];
                        s2 += s1 += localBufferPtr[2];
                        s2 += s1 += localBufferPtr[3];
                        s2 += s1 += localBufferPtr[4];
                        s2 += s1 += localBufferPtr[5];
                        s2 += s1 += localBufferPtr[6];
                        s2 += s1 += localBufferPtr[7];
                        s2 += s1 += localBufferPtr[8];
                        s2 += s1 += localBufferPtr[9];
                        s2 += s1 += localBufferPtr[10];
                        s2 += s1 += localBufferPtr[11];
                        s2 += s1 += localBufferPtr[12];
                        s2 += s1 += localBufferPtr[13];
                        s2 += s1 += localBufferPtr[14];
                        s2 += s1 += localBufferPtr[15];

                        localBufferPtr += 16;
                        k -= 16;
                    }

                    while (k-- > 0)
                    {
                        s2 += s1 += *localBufferPtr++;
                    }

                    s1 %= BASE;
                    s2 %= BASE;
                }

                return (s2 << 16) | s1;
            }
        }
    }
}
