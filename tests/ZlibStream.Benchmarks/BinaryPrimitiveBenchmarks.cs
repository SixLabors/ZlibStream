// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Buffers.Binary;
using BenchmarkDotNet.Attributes;

namespace ZlibStream.Benchmarks
{
    public class BinaryPrimitiveBenchmarks
    {
        private byte[] buffer = new byte[2];

        [Benchmark]
        public void PutShort()
        {
            BinaryPrimitives.WriteInt16LittleEndian(this.buffer, 255);
        }

        [Benchmark]
        public void PutShort2()
        {
            const int w = 255;
            this.buffer[0] = (byte)w;
            this.buffer[1] = (byte)w >> 8;
        }
    }
}
