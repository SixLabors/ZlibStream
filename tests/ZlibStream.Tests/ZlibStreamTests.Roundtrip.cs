using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using SixLabors;
using Xunit;

namespace ZlibStream.Tests
{
    public partial class ZlibStreamTests
    {
        [Fact]
        public void EncodeDecode()
        {
            var expected = GetBuffer(4096 * 4);
            var reference = new byte[4096 * 4];
            var actual = new byte[4096 * 4];
            using (var compressed = new MemoryStream())
            {
                using (var deflate = new ZOutputStream(compressed, ZlibCompression.ZDEFAULTCOMPRESSION))
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

                if (e != r)
                {
                    throw new Exception();
                }

                if (e != a)
                {
                    throw new Exception();
                }
            }

            Assert.Equal(expected, actual);
        }

        private static byte[] GetBuffer(int length)
        {
            var data = new byte[length];
            new Random(1).NextBytes(data);

            return data;
        }
    }
}
