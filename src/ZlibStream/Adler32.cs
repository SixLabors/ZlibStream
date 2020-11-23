// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        private const int MinBufferSize = 32;
        private const byte ShuffleMaskHighToLow = 0b_11_10_11_10;
        private const byte ShuffleMaskOddToEven = 0b_11_11_01_01;

        // The C# compiler emits this as a compile-time constant embedded in the PE file.
        private static ReadOnlySpan<sbyte> UnrollMultipliers => new sbyte[]
        {
            32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17,
            16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1
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
        [MethodImpl(InliningOptions.ShortMethod)]
        public static uint Calculate(uint adler, ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return adler;
            }

#if SUPPORTS_RUNTIME_INTRINSICS
            if (Ssse3.IsSupported && buffer.Length >= MinBufferSize)
            {
                return CalculateSsse3(adler, buffer);
            }

            return CalculateScalar(adler, buffer);
#else
            return CalculateScalar(adler, buffer);
#endif
        }

        // Inspired by https://github.com/chromium/chromium/blob/master/third_party/zlib/adler32_simd.c
#if SUPPORTS_RUNTIME_INTRINSICS
        [MethodImpl(InliningOptions.ShortMethod)]
        private static uint CalculateSsse3(uint adler, ReadOnlySpan<byte> buffer)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = adler >> 16;

            ref byte bufRef = ref MemoryMarshal.GetReference(buffer);
            ref byte endRef = ref Unsafe.Add(ref bufRef, buffer.Length);

            uint vectors = (uint)Unsafe.ByteOffset(ref bufRef, ref endRef) / (uint)Vector128<byte>.Count;
            while (vectors > 0)
            {
                // Process n 128-bit vectors of data. At most NMAX data bytes can be
                // processed before s2 must be reduced modulo BASE.
                uint n = Math.Min(vectors, NMAX / (uint)Vector128<byte>.Count);
                vectors -= n;

                Vector128<uint> v_s1, v_s2;
                Vector128<short> vone;
                if (Avx2.IsSupported && n >= 4)
                {
                    // The AVX2 loop handles four 128-bit vectors (64 bytes) per iteration.
                    Vector256<sbyte> mul2 = Unsafe.As<sbyte, Vector256<sbyte>>(ref MemoryMarshal.GetReference(UnrollMultipliers));
                    Vector256<sbyte> mul1 = Avx2.Add(Vector256.Create((sbyte)32), mul2);
                    Vector256<short> wone = Vector256.Create((short)1);
                    Vector256<byte> zero = Vector256<byte>.Zero;

                    Vector256<uint> w_s1 = Vector256.CreateScalar(s1);
                    Vector256<uint> w_s2 = Vector256.CreateScalar(s2);
                    Vector256<uint> w_ps = Vector256<uint>.Zero;

                    do
                    {
                        // Load 64 input bytes.
                        Vector256<byte> bytes1 = Unsafe.As<byte, Vector256<byte>>(ref bufRef);
                        Vector256<byte> bytes2 = Unsafe.As<byte, Vector256<byte>>(ref Unsafe.Add(ref bufRef, Vector256<byte>.Count));
                        bufRef = ref Unsafe.Add(ref bufRef, Vector256<byte>.Count * 2);
                        n -= 4;

                        // We need to accumulate the previous s1 value into s2 64 times each iteration.
                        // Rather than duplicate the effort, we will keep a running total of the
                        // previous sums, then multiply the whole thing by 64 and add it to s2 at the end.
                        w_ps = Avx2.Add(w_ps, w_s1);

                        // Horizontally add the bytes for s1 -- PSADBW subtracts (zero in this case) and
                        // then sums sets of 8 adjacent bytes into 16-bit results padded to 64 bits,
                        // which we then reinterpret as 32-bit values.
                        // Multiply-add the bytes by [ 64, 63, 62, ... ] for s2 -- PMADDUBSW multiplies
                        // two byte values then adds adjacent pairs into 16-bit values. Then PMADDWD
                        // does the same with the short values, yielding one 32-bit sum for each 4
                        // bytes multiplied by their associated positional multiplier.
                        w_s1 = Avx2.Add(w_s1, Avx2.SumAbsoluteDifferences(bytes1, zero).AsUInt32());
                        Vector256<short> mad1 = Avx2.MultiplyAddAdjacent(bytes1, mul1);
                        w_s2 = Avx2.Add(w_s2, Avx2.MultiplyAddAdjacent(mad1, wone).AsUInt32());

                        w_s1 = Avx2.Add(w_s1, Avx2.SumAbsoluteDifferences(bytes2, zero).AsUInt32());
                        Vector256<short> mad2 = Avx2.MultiplyAddAdjacent(bytes2, mul2);
                        w_s2 = Avx2.Add(w_s2, Avx2.MultiplyAddAdjacent(mad2, wone).AsUInt32());
                    }
                    while (n >= 4);

                    // Here we take care of accumulating the previous sums.
                    w_s2 = Avx2.Add(w_s2, Avx2.ShiftLeftLogical(w_ps, 6));

                    // Collapse the vectors to 128-bit so they can be carried into the SSSE3 branch.
                    v_s1 = Sse2.Add(w_s1.GetLower(), w_s1.GetUpper());
                    v_s2 = Sse2.Add(w_s2.GetLower(), w_s2.GetUpper());
                    vone = wone.GetLower();
                }
                else
                {
                    // If the AVX2 loop didn't run, initialize the 128-bit vectors.
                    v_s1 = Vector128.CreateScalar(s1);
                    v_s2 = Vector128.CreateScalar(s2);
                    vone = Vector128.Create((short)1);
                }

                if (n != 0)
                {
                    // If the input length wasn't a mupliple of 64 or if AVX2 isn't supported,
                    // process the remainder using SSSE3.
                    Vector128<sbyte> mul2 = Unsafe.Add(ref Unsafe.As<sbyte, Vector128<sbyte>>(ref MemoryMarshal.GetReference(UnrollMultipliers)), 1);
                    Vector128<byte> zero = Vector128<byte>.Zero;

                    if (n >= 2)
                    {
                        // This loop mirrors the AVX2 loop above at half the vector width.
                        // It processes two 128-bit vectors (32 bytes) per iteration.
                        Vector128<sbyte> mul1 = Unsafe.As<sbyte, Vector128<sbyte>>(ref MemoryMarshal.GetReference(UnrollMultipliers));
                        Vector128<uint> v_ps = Vector128<uint>.Zero;

                        do
                        {
                            Vector128<byte> bytes1 = Unsafe.As<byte, Vector128<byte>>(ref bufRef);
                            Vector128<byte> bytes2 = Unsafe.As<byte, Vector128<byte>>(ref Unsafe.Add(ref bufRef, Vector128<byte>.Count));
                            bufRef = ref Unsafe.Add(ref bufRef, Vector128<byte>.Count * 2);
                            n -= 2;

                            v_ps = Sse2.Add(v_ps, v_s1);

                            v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes1, zero).AsUInt32());
                            Vector128<short> mad1 = Ssse3.MultiplyAddAdjacent(bytes1, mul1);
                            v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad1, vone).AsUInt32());

                            v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes2, zero).AsUInt32());
                            Vector128<short> mad2 = Ssse3.MultiplyAddAdjacent(bytes2, mul2);
                            v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad2, vone).AsUInt32());
                        }
                        while (n >= 2);

                        v_s2 = Sse2.Add(v_s2, Sse2.ShiftLeftLogical(v_ps, 5));
                    }

                    if (n != 0)
                    {
                        // If there is a trailing 128-bit vector, use a half SSSE3 iteration to finish.
                        Vector128<byte> bytes1 = Unsafe.As<byte, Vector128<byte>>(ref bufRef);
                        bufRef = ref Unsafe.Add(ref bufRef, Vector128<byte>.Count);

                        v_s2 = Sse2.Add(v_s2, Sse2.ShiftLeftLogical(v_s1, 4));

                        v_s1 = Sse2.Add(v_s1, Sse2.SumAbsoluteDifferences(bytes1, zero).AsUInt32());
                        Vector128<short> mad1 = Ssse3.MultiplyAddAdjacent(bytes1, mul2);
                        v_s2 = Sse2.Add(v_s2, Sse2.MultiplyAddAdjacent(mad1, vone).AsUInt32());
                    }
                }

                // Horizontally sum the 2 even elements in the s1 vector.
                // The odd elements will be zero because PSADBW outputs two 64-bit values.
                v_s1 = Sse2.Add(v_s1, Sse2.Shuffle(v_s1, ShuffleMaskHighToLow));
                s1 = Sse2.ConvertToUInt32(v_s1);

                // And horizontally sum the 4 elememts in the s2 vector.
                v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, ShuffleMaskOddToEven));
                v_s2 = Sse2.Add(v_s2, Sse2.Shuffle(v_s2, ShuffleMaskHighToLow));
                s2 = Sse2.ConvertToUInt32(v_s2);

                // Reduce.
                s1 %= BASE;
                s2 %= BASE;
            }

            // This handles at most 15 leftover bytes that didn't fit in the SIMD loop.
            if (Unsafe.IsAddressLessThan(ref bufRef, ref endRef))
            {
                while (!Unsafe.IsAddressGreaterThan(ref bufRef, ref Unsafe.Subtract(ref endRef, 4)))
                {
                    s2 += s1 += Unsafe.Add(ref bufRef, 0);
                    s2 += s1 += Unsafe.Add(ref bufRef, 1);
                    s2 += s1 += Unsafe.Add(ref bufRef, 2);
                    s2 += s1 += Unsafe.Add(ref bufRef, 3);

                    bufRef = ref Unsafe.Add(ref bufRef, 4);
                }

                while (Unsafe.IsAddressLessThan(ref bufRef, ref endRef))
                {
                    s2 += s1 += bufRef;
                    bufRef = ref Unsafe.Add(ref bufRef, 1);
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

        [MethodImpl(InliningOptions.ShortMethod)]
        private static uint CalculateScalar(uint adler, ReadOnlySpan<byte> buffer)
        {
            uint s1 = adler & 0xFFFF;
            uint s2 = adler >> 16;

            ref byte bufRef = ref MemoryMarshal.GetReference(buffer);
            ref byte endRef = ref Unsafe.Add(ref bufRef, buffer.Length);

            while (Unsafe.IsAddressLessThan(ref bufRef, ref endRef))
            {
                int blockBytes = Math.Min((int)Unsafe.ByteOffset(ref bufRef, ref endRef), (int)NMAX);
                ref byte blockEndRef = ref Unsafe.Add(ref bufRef, blockBytes);

                while (!Unsafe.IsAddressGreaterThan(ref bufRef, ref Unsafe.Subtract(ref blockEndRef, 16)))
                {
                    s2 += s1 += Unsafe.Add(ref bufRef, 0);
                    s2 += s1 += Unsafe.Add(ref bufRef, 1);
                    s2 += s1 += Unsafe.Add(ref bufRef, 2);
                    s2 += s1 += Unsafe.Add(ref bufRef, 3);
                    s2 += s1 += Unsafe.Add(ref bufRef, 4);
                    s2 += s1 += Unsafe.Add(ref bufRef, 5);
                    s2 += s1 += Unsafe.Add(ref bufRef, 6);
                    s2 += s1 += Unsafe.Add(ref bufRef, 7);
                    s2 += s1 += Unsafe.Add(ref bufRef, 8);
                    s2 += s1 += Unsafe.Add(ref bufRef, 9);
                    s2 += s1 += Unsafe.Add(ref bufRef, 10);
                    s2 += s1 += Unsafe.Add(ref bufRef, 11);
                    s2 += s1 += Unsafe.Add(ref bufRef, 12);
                    s2 += s1 += Unsafe.Add(ref bufRef, 13);
                    s2 += s1 += Unsafe.Add(ref bufRef, 14);
                    s2 += s1 += Unsafe.Add(ref bufRef, 15);

                    bufRef = ref Unsafe.Add(ref bufRef, 16);
                }

                while (!Unsafe.IsAddressGreaterThan(ref bufRef, ref Unsafe.Subtract(ref blockEndRef, 4)))
                {
                    s2 += s1 += Unsafe.Add(ref bufRef, 0);
                    s2 += s1 += Unsafe.Add(ref bufRef, 1);
                    s2 += s1 += Unsafe.Add(ref bufRef, 2);
                    s2 += s1 += Unsafe.Add(ref bufRef, 3);

                    bufRef = ref Unsafe.Add(ref bufRef, 4);
                }

                while (Unsafe.IsAddressLessThan(ref bufRef, ref blockEndRef))
                {
                    s2 += s1 += bufRef;
                    bufRef = ref Unsafe.Add(ref bufRef, 1);
                }

                s1 %= BASE;
                s2 %= BASE;
            }

            return (s2 << 16) | s1;
        }
    }
}
