// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using SixLabors.ZlibStream;
using Xunit;

namespace ZlibStream.Tests
{
    public class Adler32Tests
    {
        [Theory]
        [InlineData(1024)]
        [InlineData(2034)]
        [InlineData(4096)]
        public void MatchesReference(int length)
        {
            var data = GetBuffer(length);
            var refAdler32 = new ICSharpCode.SharpZipLib.Checksum.Adler32();
            refAdler32.Update(data);

            long expected = refAdler32.Value;
            long actual = Adler32.Calculate(1, data, 0, data.Length);

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
