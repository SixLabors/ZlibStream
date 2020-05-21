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
// |             Method | Compression |                  Mean |              Error |              StdDev |                Median | Ratio | RatioSD |    Bytes |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
// |------------------- |------------ |----------------------:|-------------------:|--------------------:|----------------------:|------:|--------:|---------:|----------:|----------:|------:|-----------:|
// | SharpZipLibDeflate |           1 | 1,021,612,566.1765 ns | 20,200,814.4331 ns |  48,399,861.1732 ns | 1,037,427,350.0000 ns |  1.00 |    0.00 | 16315059 | 3000.0000 | 1000.0000 |     - | 49035656 B |
// |   SixLaborsDeflate |           1 |   131,608,033.3333 ns |  1,033,111.2241 ns |     966,372.8800 ns |   131,561,325.0000 ns |  0.13 |    0.01 |   825050 |         - |         - |     - |  2097928 B |
// |      DotNetDeflate |           1 |    53,836,300.0000 ns |    311,248.4785 ns |     259,906.5760 ns |    53,878,788.8889 ns |  0.05 |    0.00 |   825050 |         - |         - |     - |  2090486 B |
// |        ZLibManaged |           1 | 1,213,970,405.0000 ns | 22,988,640.1437 ns |  26,473,752.1755 ns | 1,213,816,750.0000 ns |  1.22 |    0.09 | 16314795 |         - |         - |     - | 83079576 B |
// |                    |             |                       |                    |                     |                       |       |         |          |           |           |       |            |
// | SharpZipLibDeflate |           6 |   454,864,940.0000 ns |  8,395,919.5257 ns |   7,853,548.3335 ns |   455,084,600.0000 ns |  1.00 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
// |   SixLaborsDeflate |           6 |   287,681,200.0000 ns |    997,969.1038 ns |     884,673.6165 ns |   287,792,350.0000 ns |  0.63 |    0.01 |   659280 |         - |         - |     - |  2098608 B |
// |      DotNetDeflate |           6 |   124,441,155.7692 ns |    436,466.0863 ns |     364,468.9497 ns |   124,335,950.0000 ns |  0.27 |    0.00 |   742721 |         - |         - |     - |  2090756 B |
// |        ZLibManaged |           6 |   491,527,754.5455 ns |  8,499,167.4191 ns |  10,437,740.8637 ns |   487,268,900.0000 ns |  1.09 |    0.04 |   553817 |         - |         - |     - | 51372664 B |
// |                    |             |                       |                    |                     |                       |       |         |          |           |           |       |            |
// | SharpZipLibDeflate |           9 | 2,706,881,235.7143 ns | 39,566,104.7482 ns |  35,074,321.2825 ns | 2,692,407,600.0000 ns | 1.000 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
// |   SixLaborsDeflate |           9 |   269,531,600.0000 ns |  2,136,103.6604 ns |   1,783,743.3011 ns |   268,900,600.0000 ns | 0.100 |    0.00 |   659280 |         - |         - |     - |  2097928 B |
// |      DotNetDeflate |           9 |             0.4450 ns |          0.0042 ns |           0.0039 ns |             0.4428 ns | 0.000 |    0.00 |       -1 |         - |         - |     - |          - |
// |        ZLibManaged |           9 | 3,015,820,337.7551 ns | 92,630,177.8609 ns | 270,206,576.6429 ns | 2,897,383,800.0000 ns | 1.046 |    0.03 |   553817 |         - |         - |     - | 51372664 B |
