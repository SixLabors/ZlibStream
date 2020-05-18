// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using SixLabors.ZlibStream;

namespace ZlibStream.Benchmarks
{
    [Config(typeof(DeflateConfig))]
    public class ZlibDeflateBenchmark
    {
        private byte[] data;

        [GlobalSetup]
        public void SetUp()
        {
            // Equivalent to 3.5MP RGBA image
            this.data = GetImageBytes(3500, 3500); // GetBuffer(1000 * 1000 * 4);
        }

        [Params(1, 6, 9)]
        public int Compression { get; set; }

        [Benchmark(Baseline = true)]
        public long SharpZipLibDeflate()
        {
            using (var output = new MemoryStream())
            {
                // Defaults to compression -1, buffer 512.
                var deflater = new Deflater(this.Compression);
                using (var deflate = new DeflaterOutputStream(output, deflater))
                {
                    deflate.IsStreamOwner = false;
                    deflate.Write(this.data, 0, this.data.Length);
                }

                return output.Length;
            }
        }

        [Benchmark]
        public long SixLaborsDeflate()
        {
            using (var output = new MemoryStream())
            {
                // Defaults to compression -1, buffer 512.
                using (var deflate = new ZlibOutputStream(output, (ZlibCompressionLevel)this.Compression))
                {
                    deflate.Write(this.data, 0, this.data.Length);
                }

                return output.Length;
            }
        }

        [Benchmark]
        public long DotNetDeflate()
        {
            // DeflateStream does not actually provide this level of compression
            // maxing out at 6 Optimal.
            if (this.Compression == 9)
            {
                return -1;
            }

            using (var output = new MemoryStream())
            {
                // Defaults to compression -1, buffer 512.
                using (var deflate = new DotNetZlibDeflateStream(output, this.Compression))
                {
                    deflate.Write(this.data, 0, this.data.Length);
                }

                return output.Length;
            }
        }

        [Benchmark(Description = "ZLibManaged")]
        public long ZlibManagedDeflate()
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new Elskom.Generic.Libs.ZOutputStream(output, (Elskom.Generic.Libs.ZlibCompression)this.Compression))
                {
                    deflate.Write(this.data, 0, this.data.Length);
                }

                return output.Length;
            }
        }

        private static byte[] GetImageBytes(int width, int height)
        {
            var bytes = new byte[width * height * 4];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width * 4; x += 4)
                {
                    int i = 4 * y * width;
                    bytes[i + x] = (byte)((x + y) % 256); // R
                    bytes[i + x + 1] = 0; // G
                    bytes[i + x + 2] = 0; // B
                    bytes[i + x + 3] = 255; // A
                }
            }

            return bytes;
        }

        private static byte[] GetBuffer(int length)
        {
            var data = new byte[length];
            new Random(1).NextBytes(data);

            return data;
        }
    }
}

// BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.815 (1909/November2018Update/19H2)
// Intel Core i7-8650U CPU 1.90GHz(Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
// .NET Core SDK = 3.1.201
//
// [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT
//  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT
//
//
// |             Method | Compression |                  Mean |              Error |             StdDev | Ratio |    Bytes |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
// |------------------- |------------ |----------------------:|-------------------:|-------------------:|------:|---------:|----------:|----------:|------:|-----------:|
// | SharpZipLibDeflate |           1 |   946,960,571.4286 ns |  8,561,002.2950 ns |  7,589,105.5464 ns |  1.00 | 16315059 | 3000.0000 | 1000.0000 |     - | 49035656 B |
// |   SixLaborsDeflate |           1 |   745,533,160.0000 ns |  2,576,961.4457 ns |  2,410,491.3352 ns |  0.79 | 16313211 |         - |         - |     - | 33555312 B |
// |      DotNetDeflate |           1 |    53,980,694.2857 ns |    829,608.5618 ns |    735,426.3813 ns |  0.06 |   825050 |         - |         - |     - |  2090328 B |
// |        ZLibManaged |           1 | 1,162,060,064.2857 ns |  3,610,128.1579 ns |  3,200,284.5791 ns |  1.23 | 16314795 |         - |         - |     - | 83076152 B |
// |                    |             |                       |                    |                    |       |          |           |           |       |            |
// | SharpZipLibDeflate |           6 |   408,324,214.2857 ns |  3,157,340.3311 ns |  2,798,899.9644 ns |  1.00 |   553805 |         - |         - |     - |  2864872 B |
// |   SixLaborsDeflate |           6 |   313,137,800.0000 ns |  1,638,431.2085 ns |  1,368,164.2640 ns |  0.77 |   553817 |         - |         - |     - |  2097936 B |
// |      DotNetDeflate |           6 |   113,716,003.0769 ns |    253,379.1416 ns |    211,583.0588 ns |  0.28 |   742721 |         - |         - |     - |  2092363 B |
// |        ZLibManaged |           6 |   449,391,535.7143 ns |  1,408,366.6981 ns |  1,248,480.3942 ns |  1.10 |   553817 |         - |         - |     - | 51373112 B |
// |                    |             |                       |                    |                    |       |          |           |           |       |            |
// | SharpZipLibDeflate |           9 | 2,493,848,806.6667 ns | 29,688,718.2607 ns | 27,770,845.4813 ns | 1.000 |   553805 |         - |         - |     - |  2864872 B |
// |   SixLaborsDeflate |           9 | 2,122,100,550.0000 ns | 32,807,721.3513 ns | 29,083,190.4365 ns | 0.851 |   553817 |         - |         - |     - |  2097936 B |
// |      DotNetDeflate |           9 |             0.4276 ns |          0.0041 ns |          0.0038 ns | 0.000 |       -1 |         - |         - |     - |          - |
// |        ZLibManaged |           9 | 2,664,216,461.5385 ns |  5,081,563.3279 ns |  4,243,335.5240 ns | 1.067 |   553817 |         - |         - |     - | 51372664 B |
