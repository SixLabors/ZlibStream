// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Elskom.Generic.Libs;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using SixLabors.ZlibStream;
using ZlibStream.Tests.TestUtilities;

namespace ZlibStream.Benchmarks
{
    [Config(typeof(DeflateConfig))]
    public class DeflateCorpusBenchmark
    {
        private Dictionary<string, byte[]> data = new Dictionary<string, byte[]>();

        public IEnumerable<string> Files { get; } = new[]
        {
            Corpus.Alice,
            Corpus.CCITT,
            Corpus.CompressionPointers,
            Corpus.Electronic,
            Corpus.Excel,
            Corpus.Fields,
            Corpus.GnuManual,
            Corpus.Grammar,
            Corpus.ParadiseLost,
            Corpus.Shakespeare,
            Corpus.Sum
        };

        [GlobalSetup]
        public void SetUp()
        {
            foreach (var file in this.Files)
            {
                using (FileStream fs = File.OpenRead(Path.Combine(TestEnvironment.CorpusDirectoryFullPath, file)))
                using (var ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    this.data.Add(file, ms.ToArray());
                }
            }
        }

        [Params(1, 3, 6)]
        public int Compression { get; set; }

        [Benchmark(Baseline = true, Description = "Microsoft")]
        [ArgumentsSource(nameof(Files))]
        public long DotNetDeflate(string file)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new DotNetZlibDeflateStream(output, this.Compression))
                {
                    var buffer = this.data[file];
                    deflate.Write(buffer, 0, buffer.Length);
                }

                return output.Length;
            }
        }

        [Benchmark(Description = "SharpZipLib")]
        [ArgumentsSource(nameof(Files))]
        public long SharpZipLibDeflate(string file)
        {
            using (var output = new MemoryStream())
            {
                var deflater = new Deflater(this.Compression);
                using (var deflate = new DeflaterOutputStream(output, deflater))
                {
                    deflate.IsStreamOwner = false;
                    var buffer = this.data[file];
                    deflate.Write(buffer, 0, buffer.Length);
                }

                return output.Length;
            }
        }

        [Benchmark(Description = "SixLabors")]
        [ArgumentsSource(nameof(Files))]
        public long SixLaborsDeflate(string file)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new ZlibOutputStream(output, (CompressionLevel)this.Compression))
                {
                    var buffer = this.data[file];
                    deflate.Write(buffer, 0, buffer.Length);
                }

                return output.Length;
            }
        }

        [Benchmark(Description = "ZLibManaged")]
        [ArgumentsSource(nameof(Files))]
        public long ZlibManagedDeflate(string file)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new ZOutputStream(output, (ZlibCompression)this.Compression))
                {
                    var buffer = this.data[file];
                    deflate.Write(buffer, 0, buffer.Length);
                }

                return output.Length;
            }
        }
    }
}
