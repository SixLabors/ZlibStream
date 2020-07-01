// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.IO;
using BenchmarkDotNet.Attributes;
using Elskom.Generic.Libs;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using SixLabors.ZlibStream;

namespace ZlibStream.Benchmarks
{
    [Config(typeof(DeflateConfig))]
    public class DeflateSparseBenchmark
    {
        private static byte[] data = GetImageBytes(3500, 3500);

        [Params(1, 3, 6)]
        public int Compression { get; set; }

        [Benchmark(Baseline = true, Description = "Microsoft")]
        public long DotNetDeflate()
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new DotNetZlibDeflateStream(output, this.Compression))
                {
                    var buffer = data;
                    deflate.Write(buffer, 0, buffer.Length);
                }

                return output.Length;
            }
        }

        [Benchmark(Description = "SharpZipLib")]
        public long SharpZipLibDeflate()
        {
            using (var output = new MemoryStream())
            {
                var deflater = new Deflater(this.Compression);
                using (var deflate = new DeflaterOutputStream(output, deflater))
                {
                    deflate.IsStreamOwner = false;
                    var buffer = data;
                    deflate.Write(buffer, 0, buffer.Length);
                }

                return output.Length;
            }
        }

        [Benchmark(Description = "SixLabors")]
        public long SixLaborsDeflate()
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new ZlibOutputStream(output, (CompressionLevel)this.Compression))
                {
                    var buffer = data;
                    deflate.Write(buffer, 0, buffer.Length);
                }

                return output.Length;
            }
        }

        [Benchmark(Description = "ZLibManaged")]
        public long ZlibManagedDeflate()
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new ZOutputStream(output, (ZlibCompression)this.Compression))
                {
                    var buffer = data;
                    deflate.Write(buffer, 0, buffer.Length);
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
    }
}
