// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

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
        [InlineData(ZlibCompressionLevel.ZNOCOMPRESSION)]
        [InlineData(ZlibCompressionLevel.Level1)]
        [InlineData(ZlibCompressionLevel.Level2)]
        [InlineData(ZlibCompressionLevel.Level3)]
        [InlineData(ZlibCompressionLevel.Level4)]
        [InlineData(ZlibCompressionLevel.Level5)]
        [InlineData(ZlibCompressionLevel.Level6)]
        [InlineData(ZlibCompressionLevel.Level7)]
        [InlineData(ZlibCompressionLevel.ZBESTCOMPRESSION)]
        [InlineData(ZlibCompressionLevel.ZDEFAULTCOMPRESSION)]
        public void EncodeDecode(ZlibCompressionLevel compression)
        {
            const int count = 2 * 4096 * 4;
            var expected = GetBuffer(count);
            var reference = new byte[count];
            var actual = new byte[count];

            using (var compressed = new MemoryStream())
            {
                using (var deflate = new ZlibOutputStream(compressed, compression))
                {
                    deflate.Write(expected);
                }

                compressed.Position = 0;

                using (var refInflate = new InflaterInputStream(compressed))
                {
                    refInflate.IsStreamOwner = false;
                    refInflate.Read(reference);
                }

                compressed.Position = 0;

                using (var inflate = new ZlibInputStream(compressed))
                {
                    inflate.Read(actual);
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

        // Used for profiling with Rider.
        [Fact]
        public void DeflateProfileTest()
        {
            const int count = 1000 * 1000 * 4;
            var expected = GetBuffer(count);

            using (var compressed = new MemoryStream())
            using (var deflate = new ZlibOutputStream(compressed, ZlibCompressionLevel.Level6))
            {
                deflate.Write(expected, 0, expected.Length);
            }
        }

        private static byte[] GetBuffer(int length)
        {
            var data = new byte[length];
            new Random(1).NextBytes(data);

            return data;
        }
    }
}
