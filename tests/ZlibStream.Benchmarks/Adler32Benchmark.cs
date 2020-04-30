// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using BenchmarkDotNet.Attributes;
using SixLabors.ZlibStream;

namespace ZlibStream.Benchmarks
{
    [Config(typeof(ShortRun))]
    public class Adler32Benchmark
    {
        private byte[] data;

        [Params(1024, 2048, 4096)]
        public int Count { get; set; }

        [GlobalSetup]
        public void SetUp()
        {
            this.data = new byte[this.Count];
            new Random(1).NextBytes(this.data);
        }

        [Benchmark(Baseline = true)]
        public long SharpZipLibUpdate()
        {
            var adler32 = new ICSharpCode.SharpZipLib.Checksum.Adler32();
            adler32.Update(this.data);
            return adler32.Value;
        }

        [Benchmark]
        public long SixLaborsVectorUpdate()
        {
            return Adler32.Calculate(1, this.data, 0, this.data.Length);
        }

        [Benchmark]
        public long SixLaborsScalarUpdate()
        {
            return Adler32.CalculateScalar(1, this.data, 0, (uint)this.data.Length);
        }

        [Benchmark]
        public long ZlibManagedUpdate()
        {
            return ZlibManagedAdler32.Calculate(1, this.data, 0, this.data.Length);
        }
    }

    // Reference implementation taken from zlib.managed.
    internal static class ZlibManagedAdler32
    {
        // largest prime smaller than 65536
        private const int BASE = 65521;

        // NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
        private const int NMAX = 5552;

        internal static long Calculate(long adler, byte[] buf, int index, int len)
        {
            if (buf == null)
            {
                return 1L;
            }

            var s1 = adler & 0xFFFF;
            var s2 = (adler >> 16) & 0xFFFF;
            int k;

            while (len > 0)
            {
                k = len < NMAX ? len : NMAX;
                len -= k;
                while (k >= 16)
                {
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    s1 += buf[index++] & 0xFF;
                    s2 += s1;
                    k -= 16;
                }

                if (k != 0)
                {
                    do
                    {
                        s1 += buf[index++] & 0xFF;
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
