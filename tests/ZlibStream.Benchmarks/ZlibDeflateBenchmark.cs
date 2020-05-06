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

        // TODO: Enable when BMDN stops throwing.
        // [Benchmark(Description = "ZLibManaged")]
        //public long ZlibManagedDeflate()
        //{
        //    using (var output = new MemoryStream())
        //    {
        //        // Defaults to compression -1, buffer 512.
        //        using (var deflate = new Elskom.Generic.Libs.ZOutputStream(output, this.Compression))
        //        {
        //            deflate.Write(this.data, 0, this.data.Length);
        //        }

        //        return output.Length;
        //    }
        //}

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
