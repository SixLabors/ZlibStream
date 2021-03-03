// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using SixLabors.ZlibStream;
using Xunit;

namespace ZlibStream.Tests
{
    public partial class ZlibStreamTests
    {
        [Theory]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.Level1)]
        [InlineData(CompressionLevel.Level2)]
        [InlineData(CompressionLevel.Level3)]
        [InlineData(CompressionLevel.Level4)]
        [InlineData(CompressionLevel.Level5)]
        [InlineData(CompressionLevel.Level6)]
        [InlineData(CompressionLevel.Level7)]
        [InlineData(CompressionLevel.BestCompression)]
        [InlineData(CompressionLevel.DefaultCompression)]
        public void EncodeDecode(CompressionLevel level)
        {
            foreach (CompressionStrategy strategy in (CompressionStrategy[])Enum.GetValues(typeof(CompressionStrategy)))
            {
                const int count = 2 * 4096 * 4;
                byte[] expected = GetBuffer(count);
                byte[] reference = new byte[count];
                byte[] actual = new byte[count];

                using (var compressed = new MemoryStream())
                {
                    var options = new ZlibOptions { CompressionStrategy = strategy, CompressionLevel = level };
                    using (var deflate = new ZlibOutputStream(compressed, options))
                    {
                        deflate.Write(expected, 0, expected.Length);
                    }

                    compressed.Position = 0;

                    using (var refInflate = new InflaterInputStream(compressed))
                    {
                        refInflate.IsStreamOwner = false;
                        refInflate.Read(reference, 0, reference.Length);
                    }

                    compressed.Position = 0;

                    using (var inflate = new ZlibInputStream(compressed))
                    {
                        inflate.Read(actual, 0, actual.Length);
                    }
                }

                for (int i = 0; i < expected.Length; i++)
                {
                    byte e = expected[i];
                    byte r = reference[i];
                    byte a = actual[i];

                    Assert.Equal(e, r);
                    Assert.Equal(e, a);
                }
            }
        }

        // Used for profiling with Rider.
        [Fact]
        public void DeflateProfileTest()
        {
            const int count = 1000 * 1000 * 4;
            var expected = GetBuffer(count);

            using (var compressed = new MemoryStream())
            using (var deflate = new ZlibOutputStream(compressed, CompressionLevel.Level6))
            {
                deflate.Write(expected, 0, expected.Length);
            }
        }

        [Fact]
        public void DeflateMemoryProfileTest()
        {
            var expected = GetImageBytes(3500, 3500);

            using (var compressed = new MemoryStream())
            using (var deflate = new ZlibOutputStream(compressed, CompressionLevel.Level1))
            {
                deflate.Write(expected, 0, expected.Length);
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
