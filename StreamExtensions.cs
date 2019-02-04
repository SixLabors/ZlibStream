// Copyright (c) 2018-2019, Els_kom org.
// https://github.com/Elskom/
// All rights reserved.
// license: see LICENSE for more details.

namespace Elskom.Generic.Libs
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension for <see cref="Stream"/> that adds <see cref="Stream.CopyTo(Stream)"/> overloads
    /// accepting <see cref="BinaryWriter"/>'s.
    /// </summary>
    public static class StreamExtensions
    {
        // Thank you .NET Core (.NET Foundation) for the public Stream.CopyTo{Async} Implementations.
        // I could have never done this without you guys.

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream, using
        /// a specified buffer size.
        /// </summary>
        /// <param name="strm">
        /// The current stream for which to copy.
        /// </param>
        /// <param name="destination">
        /// The stream to which the contents of the current stream will be copied.
        /// </param>
        /// <param name="bufferSize">
        /// The size of the buffer. This value must be greater than zero. The default size
        /// is 81920.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// destination is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// bufferSize is negative or zero.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The current stream does not support reading. -or- destination does not support
        /// writing.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Either the current stream or destination were closed before the <see cref="CopyTo(Stream, BinaryWriter, int)"/>
        /// method was called.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static void CopyTo(this Stream strm, BinaryWriter destination, int bufferSize)
        {
            ValidateCopyToArgs(strm, destination, bufferSize);
            CopyToInternal(strm, destination, bufferSize);
        }

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream.
        /// </summary>
        /// <param name="strm">
        /// The current stream for which to copy.
        /// </param>
        /// <param name="destination">
        /// The stream to which the contents of the current stream will be copied.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// destination is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The current stream does not support reading. -or- destination does not support
        /// writing.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Either the current stream or destination were closed before the <see cref="CopyTo(Stream, BinaryWriter)"/>
        /// method was called.
        /// </exception>
        /// <exception cref="IOException">
        /// An I/O error occurred.
        /// </exception>
        public static void CopyTo(this Stream strm, BinaryWriter destination)
            => CopyTo(strm, destination, 81920);

#if WITH_ASNYC_STREAMS
        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another
        /// stream.
        /// </summary>
        /// <param name="strm">
        /// The current stream for which to copy.
        /// </param>
        /// <param name="destination">
        /// The stream to which the contents of the current stream will be copied.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous copy operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// destination is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Either the current stream or the destination stream is disposed.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The current stream does not support reading, or the destination stream does not
        /// support writing.
        /// </exception>
        public static Task CopyToAsync(this Stream strm, BinaryWriter destination)
            => strm.CopyToAsync(destination, 81920);

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another
        /// stream, using a cancellation token.
        /// </summary>
        /// <param name="strm">
        /// The current stream for which to copy.
        /// </param>
        /// <param name="destination">
        /// The stream to which the contents of the current stream will be copied.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous copy operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// destination is null.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Either the current stream or the destination stream is disposed.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The current stream does not support reading, or the destination stream does not
        /// support writing.
        /// </exception>
        public static Task CopyToAsync(this Stream strm, BinaryWriter destination, CancellationToken cancellationToken)
            => strm.CopyToAsync(destination, 81920, cancellationToken);

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another
        /// stream, using a specified buffer size and cancellation token.
        /// </summary>
        /// <param name="strm">
        /// The current stream for which to copy.
        /// </param>
        /// <param name="destination">
        /// The stream to which the contents of the current stream will be copied.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer. This value must be greater than zero. The
        /// default size is 81920.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous copy operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// destination is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// buffersize is negative or zero.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Either the current stream or the destination stream is disposed.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The current stream does not support reading, or the destination stream does not
        /// support writing.
        /// </exception>
        public static Task CopyToAsync(this Stream strm, BinaryWriter destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArgs(strm, destination, bufferSize);
            return CopyToAsyncInternal(strm, destination, bufferSize, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another
        /// stream, using a specified buffer size.
        /// </summary>
        /// <param name="strm">
        /// The current stream for which to copy.
        /// </param>
        /// <param name="destination">
        /// The stream to which the contents of the current stream will be copied.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer. This value must be greater than zero. The
        /// default size is 81920.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous copy operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// destination is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// buffersize is negative or zero.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Either the current stream or the destination stream is disposed.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The current stream does not support reading, or the destination stream does not
        /// support writing.
        /// </exception>
        public static Task CopyToAsync(this Stream strm, BinaryWriter destination, int bufferSize)
            => CopyToAsync(strm, destination, bufferSize, CancellationToken.None);

        private static async Task CopyToAsyncInternal(Stream strm, BinaryWriter destination, int bufferSize, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            while (true)
            {
                var bytesRead = await strm.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }

        private static Task WriteAsync(this BinaryWriter writer, byte[] buffer, int index, int count, CancellationToken cancellationToken)
        {
            if (writer == null)
            {
                var n = writer;
            }

            if (buffer == null)
            {
                var n = buffer;
            }

            if (index == 0)
            {
                var n = index;
            }

            if (count == 0)
            {
                var n = count;
            }

            if (cancellationToken == null)
            {
                var n = cancellationToken;
            }

            throw new NotImplementedException("Async writing to BinaryWriters are currently not implemented. I just do not know what I can do to fix this.");
        }

        private static Task<int> ReadAsync(this Stream strm, byte[] buffer, int index, int count, CancellationToken cancellationToken)
        {
            if (strm == null)
            {
                var n = strm;
            }

            if (buffer == null)
            {
                var n = buffer;
            }

            if (index == 0)
            {
                var n = index;
            }

            if (count == 0)
            {
                var n = count;
            }

            if (cancellationToken == null)
            {
                var n = cancellationToken;
            }

            throw new NotImplementedException("Async reading of normal streams are currently not implemented. I just do not have the original sources to patch this into the .NET Framework v4.0 target.");
        }
#endif

        private static void ValidateCopyToArgs(Stream source, BinaryWriter destination, int bufferSize)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "bufferSize must be a positive number.");
            }

            var sourceCanRead = source.CanRead;
            if (sourceCanRead && !source.CanWrite)
            {
                throw new ObjectDisposedException(nameof(source), "The current stream is disposed.");
            }

            var destinationCanWrite = destination.BaseStream.CanWrite;
            if (!destinationCanWrite && !destination.BaseStream.CanRead)
            {
                throw new ObjectDisposedException(nameof(destination), "The destination stream is disposed.");
            }

            if (!sourceCanRead)
            {
                throw new NotSupportedException("The current stream does not support reading.");
            }

            if (!destinationCanWrite)
            {
                throw new NotSupportedException("The destination stream does not support writing.");
            }
        }

        private static void CopyToInternal(Stream strm, BinaryWriter destination, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            var read = 0;
            while ((read = strm.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, read);
            }
        }
    }
}
