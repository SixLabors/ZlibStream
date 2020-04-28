// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

namespace SixLabors
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Class to support zlib stuff.
    /// </summary>
    public static class SupportClass
    {
        /// <summary>
        /// This method returns the literal value received.
        /// </summary>
        /// <param name="literal">The literal to return.</param>
        /// <returns>The received value.</returns>
        public static long Identity(long literal) => literal;

        /// <summary>
        /// This method returns the literal value received.
        /// </summary>
        /// <param name="literal">The literal to return.</param>
        /// <returns>The received value.</returns>
        public static ulong Identity(ulong literal) => literal;

        /// <summary>
        /// This method returns the literal value received.
        /// </summary>
        /// <param name="literal">The literal to return.</param>
        /// <returns>The received value.</returns>
        public static float Identity(float literal) => literal;

        /// <summary>
        /// This method returns the literal value received.
        /// </summary>
        /// <param name="literal">The literal to return.</param>
        /// <returns>The received value.</returns>
        public static double Identity(double literal) => literal;

        /*******************************/

        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number.
        /// </summary>
        /// <param name="number">Number to operate on.</param>
        /// <param name="bits">Ammount of bits to shift.</param>
        /// <returns>The resulting number from the shift operation.</returns>
        public static int URShift(int number, int bits) => number >= 0 ? number >> bits : (number >> bits) + (2 << ~bits);

        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number.
        /// </summary>
        /// <param name="number">Number to operate on.</param>
        /// <param name="bits">Ammount of bits to shift.</param>
        /// <returns>The resulting number from the shift operation.</returns>
        public static int URShift(int number, long bits) => URShift(number, (int)bits);

        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number.
        /// </summary>
        /// <param name="number">Number to operate on.</param>
        /// <param name="bits">Ammount of bits to shift.</param>
        /// <returns>The resulting number from the shift operation.</returns>
        public static long URShift(long number, int bits) => number >= 0 ? number >> bits : (number >> bits) + (2L << ~bits);

        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number.
        /// </summary>
        /// <param name="number">Number to operate on.</param>
        /// <param name="bits">Ammount of bits to shift.</param>
        /// <returns>The resulting number from the shift operation.</returns>
        public static long URShift(long number, long bits) => URShift(number, (int)bits);

        /*******************************/

        /// <summary>Reads a number of characters from the current source Stream and writes the data to the target array at the specified index.</summary>
        /// <exception cref="ArgumentNullException">When <paramref name="sourceStream"/> or <paramref name="target"/> are <see langword="null"/>.</exception>
        /// <param name="sourceStream">The source Stream to read from.</param>
        /// <param name="target">Contains the array of characteres read from the source Stream.</param>
        /// <param name="start">The starting index of the target array.</param>
        /// <param name="count">The maximum number of characters to read from the source Stream.</param>
        /// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source Stream. Returns -1 if the end of the stream is reached.</returns>
        public static int ReadInput(Stream sourceStream, byte[] target, int start, int count)
        {
            if (sourceStream == null)
            {
                throw new ArgumentNullException(nameof(sourceStream));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            // Returns 0 bytes if not enough space in target
            if (target.Length == 0)
            {
                return 0;
            }

            // var receiver = new byte[target.Length];
            return sourceStream.Read(target, start, count);

            //// Returns -1 if EOF
            //if (bytesRead == 0)
            //{
            //    return -1;
            //}

            //for (var i = start; i < start + bytesRead; i++)
            //{
            //    target[i] = receiver[i];
            //}

            //return bytesRead;
        }

        /// <summary>Reads a number of characters from the current source TextReader and writes the data to the target array at the specified index.</summary>
        /// <exception cref="ArgumentNullException">When <paramref name="sourceTextReader"/> or <paramref name="target"/> are <see langword="null"/>.</exception>
        /// <param name="sourceTextReader">The source TextReader to read from.</param>
        /// <param name="target">Contains the array of characteres read from the source TextReader.</param>
        /// <param name="start">The starting index of the target array.</param>
        /// <param name="count">The maximum number of characters to read from the source TextReader.</param>
        /// <returns>The number of characters read. The number will be less than or equal to count depending on the data available in the source TextReader. Returns -1 if the end of the stream is reached.</returns>
        public static int ReadInput(TextReader sourceTextReader, byte[] target, int start, int count)
        {
            if (sourceTextReader == null)
            {
                throw new ArgumentNullException(nameof(sourceTextReader));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            // Returns 0 bytes if not enough space in target
            if (target.Length == 0)
            {
                return 0;
            }

            var charArray = new char[target.Length];
            var bytesRead = sourceTextReader.Read(charArray, start, count);

            // Returns -1 if EOF
            if (bytesRead == 0)
            {
                return -1;
            }

            for (var index = start; index < start + bytesRead; index++)
            {
                target[index] = (byte)charArray[index];
            }

            return bytesRead;
        }

        /// <summary>
        /// Converts a string to an array of bytes.
        /// </summary>
        /// <param name="sourceString">The string to be converted.</param>
        /// <returns>The new array of bytes.</returns>
        public static byte[] ToByteArray(string sourceString) => Encoding.UTF8.GetBytes(sourceString);

        /// <summary>
        /// Converts an array of bytes to an array of chars.
        /// </summary>
        /// <param name="byteArray">The array of bytes to convert.</param>
        /// <returns>The new array of chars.</returns>
        public static char[] ToCharArray(byte[] byteArray) => Encoding.UTF8.GetChars(byteArray);
    }
}
