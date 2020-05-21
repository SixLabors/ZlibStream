// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Runtime.CompilerServices;

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// General utility class for performing Zlib operations.
    /// </summary>
    internal static class ZlibUtilities
    {
        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number.
        /// </summary>
        /// <param name="number">Number to operate on.</param>
        /// <param name="bits">Amount of bits to shift.</param>
        /// <returns>The resulting number from the shift operation.</returns>
        [MethodImpl(InliningOptions.ShortMethod)]
        public static int URShift(int number, int bits)
            => number >= 0 ? number >> bits : (number >> bits) + (2 << ~bits);
    }
}
