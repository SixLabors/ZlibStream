// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Runtime.CompilerServices;
#if SUPPORTS_CORE_CLR
using System.Runtime.InteropServices;
#endif

namespace SixLabors.ZlibStream
{
    /// <summary>
    /// Helpers for working with the <see cref="Array"/> type.
    /// Adapted from <see href="https://github.com/windows-toolkit/WindowsCommunityToolkit/blob/49a88c7d570854026855becbd25ae53c1873417f/Microsoft.Toolkit.HighPerformance/Extensions/ArrayExtensions.cs"/>
    /// </summary>
    internal static class ArrayExtensions
    {
        /// <summary>
        /// Returns a reference to an element at a specified index within a given <typeparamref name="T"/> array, with no bounds checks.
        /// </summary>
        /// <typeparam name="T">The type of elements in the input <typeparamref name="T"/> array instance.</typeparam>
        /// <param name="array">The input <typeparamref name="T"/> array instance.</param>
        /// <param name="i">The index of the element to retrieve within <paramref name="array"/>.</param>
        /// <returns>A reference to the element within <paramref name="array"/> at the index specified by <paramref name="i"/>.</returns>
        /// <remarks>This method doesn't do any bounds checks, therefore it is responsibility of the caller to ensure the <paramref name="i"/> parameter is valid.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T DangerousGetReferenceAt<T>(this T[] array, int i)
        {
            // TODO: NET5 WIll use MemoryMarshal.GetArrayDataReference
#if SUPPORTS_CORE_CLR
            RawArrayData arrayData = Unsafe.As<RawArrayData>(array);
            ref T r0 = ref Unsafe.As<byte, T>(ref arrayData.Data);
            ref T ri = ref Unsafe.Add(ref r0, i);

            return ref ri;
#else
            if ((uint)i < (uint)array.Length)
            {
                return ref array[i];
            }

            unsafe
            {
                return ref Unsafe.AsRef<T>(null);
            }
#endif
        }

#if SUPPORTS_CORE_CLR
        // Description taken from CoreCLR: see https://source.dot.net/#System.Private.CoreLib/src/System/Runtime/CompilerServices/RuntimeHelpers.CoreCLR.cs,285.
        // CLR arrays are laid out in memory as follows (multidimensional array bounds are optional):
        // [ sync block || pMethodTable || num components || MD array bounds || array data .. ]
        //                 ^                                 ^                  ^ returned reference
        //                 |                                 \-- ref Unsafe.As<RawArrayData>(array).Data
        //                 \-- array
        // The base size of an array includes all the fields before the array data,
        // including the sync block and method table. The reference to RawData.Data
        // points at the number of components, skipping over these two pointer-sized fields.
        [StructLayout(LayoutKind.Sequential)]
        private sealed class RawArrayData
        {
#pragma warning disable CS0649 // Unassigned fields
#pragma warning disable SA1401 // Fields should be private
            public IntPtr Length;
            public byte Data;
#pragma warning restore CS0649
#pragma warning restore SA1401
        }
#endif
    }
}
