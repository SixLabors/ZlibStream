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
        [InlineData(ZlibCompressionLevel.ZBESTCOMPRESSION)]
        [InlineData(ZlibCompressionLevel.ZBESTSPEED)]
        [InlineData(ZlibCompressionLevel.ZDEFAULTCOMPRESSION)]
        [InlineData(ZlibCompressionLevel.ZNOCOMPRESSION)]
        public void EncodeDecode(ZlibCompressionLevel compression)
        {
            const int count = 4096 * 4;
            var expected = GetBuffer(count);
            var reference = new byte[count];
            var actual = new byte[count];

            using (var compressed = new MemoryStream())
            {
                using (var deflate = new ZOutputStream(compressed, compression))
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

                using (var inflate = new ZInputStream(compressed))
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

        private static byte[] GetBuffer(int length)
        {
            var data = new byte[length];
            new Random(1).NextBytes(data);

            return data;
        }
    }
}
