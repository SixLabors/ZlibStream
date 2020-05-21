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
// |             Method | Compression |                  Mean |               Error |              StdDev |                Median | Ratio | RatioSD |    Bytes |     Gen 0 |     Gen 1 | Gen 2 |  Allocated |
// |------------------- |------------ |----------------------:|--------------------:|--------------------:|----------------------:|------:|--------:|---------:|----------:|----------:|------:|-----------:|
// | SharpZipLibDeflate |           1 |   948,804,438.4615 ns |  15,288,318.3073 ns |  12,766,438.1980 ns |   945,567,200.0000 ns |  1.00 |    0.00 | 16315059 | 3000.0000 | 1000.0000 |     - | 49035656 B |
// |   SixLaborsDeflate |           1 |   763,000,940.0000 ns |  15,120,368.6884 ns |  24,843,199.3665 ns |   771,921,000.0000 ns |  0.79 |    0.04 | 16314795 |         - |         - |     - | 33555312 B |
// |      DotNetDeflate |           1 |    58,979,298.3333 ns |     617,958.3530 ns |     578,038.6268 ns |    59,031,400.0000 ns |  0.06 |    0.00 |   825050 |         - |         - |     - |  2090579 B |
// |        ZLibManaged |           1 | 1,300,861,753.3333 ns |  13,766,846.9279 ns |  12,877,517.1579 ns | 1,296,915,200.0000 ns |  1.37 |    0.02 | 16314795 |         - |         - |     - | 83076152 B |
// |                    |             |                       |                     |                     |                       |       |         |          |           |           |       |            |
// | SharpZipLibDeflate |           6 |   448,527,580.0000 ns |   4,104,952.1749 ns |   3,839,774.8113 ns |   450,048,400.0000 ns |  1.00 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
// |   SixLaborsDeflate |           6 |   335,074,064.5161 ns |   6,407,390.9173 ns |  14,592,871.4925 ns |   328,221,200.0000 ns |  0.77 |    0.04 |   553817 |         - |         - |     - |  2097936 B |
// |      DotNetDeflate |           6 |   121,943,424.8000 ns |     652,739.5048 ns |     871,388.4903 ns |   121,880,020.0000 ns |  0.27 |    0.00 |   742721 |         - |         - |     - |  2090328 B |
// |        ZLibManaged |           6 |   482,885,114.2857 ns |   3,337,426.9603 ns |   2,958,542.0705 ns |   482,562,550.0000 ns |  1.08 |    0.01 |   553817 |         - |         - |     - | 51372664 B |
// |                    |             |                       |                     |                     |                       |       |         |          |           |           |       |            |
// | SharpZipLibDeflate |           9 | 3,408,726,524.0000 ns | 103,764,690.4266 ns | 305,952,530.8947 ns | 3,452,724,400.0000 ns | 1.000 |    0.00 |   553805 |         - |         - |     - |  2864872 B |
// |   SixLaborsDeflate |           9 | 2,678,341,764.5833 ns |  65,592,033.2611 ns | 189,248,013.3052 ns | 2,775,386,100.0000 ns | 0.795 |    0.10 |   553817 |         - |         - |     - |  2097936 B |
// |      DotNetDeflate |           9 |             0.5426 ns |           0.0154 ns |           0.0121 ns |             0.5400 ns | 0.000 |    0.00 |       -1 |         - |         - |     - |          - |
// |        ZLibManaged |           9 | 3,273,357,383.3333 ns |  16,249,629.1646 ns |  12,686,641.5966 ns | 3,269,930,200.0000 ns | 1.075 |    0.16 |   553817 |         - |         - |     - | 51372664 B |
