// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SixLabors.ZlibStream
{
    internal sealed unsafe partial class Trees
    {
        /// <summary>
        /// Initializes static members of the <see cref="Trees"/> class.
        /// </summary>
        static Trees() => MakeStaticTrees();

        /// <summary>
        /// Gets the static literal tree. Since the bit lengths are imposed, there is no
        /// need for the L_CODES extra codes used during heap construction.However
        /// The codes 286 and 287 are needed to build a canonical tree (see MakeStaticTrees).
        /// </summary>
        public static CodeData[] StaticLTree { get; } = new CodeData[LCODES + 2];

        /// <summary>
        /// Gets the static distance tree. (Actually a trivial tree since all codes use 5 bits.)
        /// </summary>
        public static CodeData[] StaticDTtree { get; } = new CodeData[DCODES];

        /// <summary>
        /// Gets the static literal tree descriptor.
        /// </summary>
        public static StaticTreeDesc StaticLDesc => new StaticTreeDesc(StaticLTree, ExtraLbits, LITERALS + 1, LCODES, MAXBITS);

        /// <summary>
        /// Gets the static distance tree descriptor.
        /// </summary>
        public static StaticTreeDesc StaticDDesc => new StaticTreeDesc(StaticLTree, ExtraDbits, 0, DCODES, MAXBITS);

        /// <summary>
        /// Gets the static bit length tree descriptor.
        /// </summary>
        public static StaticTreeDesc StaticBlDesc => new StaticTreeDesc(null, ExtraBlbits, 0, BLCODES, MAXBLBITS);

        private static void MakeStaticTrees()
        {
            fixed (CodeData* staticLTreePtr = StaticLTree)
            fixed (CodeData* staticDTreePtr = StaticDTtree)
            {
                CodeData* static_ltree = staticLTreePtr;
                CodeData* static_dtree = staticDTreePtr;

                // The number of codes at each bit length for an optimal tree
                ushort* bl_count = stackalloc ushort[MAXBITS + 1];

                // Construct the codes of the static literal tree.
                int n = 0;
                while (n <= 143)
                {
                    static_ltree[n++].Len = 8;
                    bl_count[8]++;
                }

                while (n <= 255)
                {
                    static_ltree[n++].Len = 9;
                    bl_count[9]++;
                }

                while (n <= 279)
                {
                    static_ltree[n++].Len = 7;
                    bl_count[7]++;
                }

                // Codes 286 and 287 do not exist, but we must include them in the tree construction
                // to get a canonical Huffman tree(longest code all ones)
                while (n <= 287)
                {
                    static_ltree[n++].Len = 8;
                    bl_count[8]++;
                }

                Gen_codes(static_ltree, LCODES + 1, bl_count);

                // The static distance tree is trivial.
                for (n = 0; n < DCODES; n++)
                {
                    static_dtree[n].Len = 5;
                    static_dtree[n].Code = (ushort)Bi_reverse((uint)n, 5);
                }
            }
        }

        /// <summary>
        /// A data structure describing a single value and its code string.
        /// A single struct with explicit offsets is used to represent different union properties.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct CodeData
        {
            /// <summary>
            /// The frequency count.
            /// </summary>
            [FieldOffset(0)]
            public ushort Freq;

            /// <summary>
            /// The bit string.
            /// </summary>
            [FieldOffset(0)]
            public ushort Code;

            /// <summary>
            /// The father node in the Huffman tree.
            /// </summary>
            [FieldOffset(2)]
            public ushort Dad;

            /// <summary>
            /// The length of the bit string.
            /// </summary>
            [FieldOffset(2)]
            public ushort Len;
        }

        /// <summary>
        /// A static tree descriptor.
        /// </summary>
        public ref struct StaticTreeDesc
        {
            private readonly ReadOnlySpan<CodeData> staticTree;
            private readonly ReadOnlySpan<byte> extraBits;

            /// <summary>
            /// Initializes a new instance of the <see cref="StaticTreeDesc"/> struct.
            /// </summary>
            /// <param name="staticTree">static tree.</param>
            /// <param name="extraBits">extra bits.</param>
            /// <param name="extraBase">extra base.</param>
            /// <param name="maxElements">max elements.</param>
            /// <param name="maxLength">max length.</param>
            public StaticTreeDesc(
                CodeData[] staticTree,
                ReadOnlySpan<byte> extraBits,
                int extraBase,
                int maxElements,
                int maxLength)
            {
                this.staticTree = staticTree;
                this.HasTree = staticTree != null;
                this.extraBits = extraBits;
                this.ExtraBase = extraBase;
                this.MaxElements = maxElements;
                this.MaxBitLength = maxLength;
            }

            /// <summary>
            /// Gets a value indicating whether the descriptor has a tree.
            /// </summary>
            public bool HasTree { get; }

            /// <summary>
            /// Gets the base index for extra_bits.
            /// </summary>
            public int ExtraBase { get; }

            /// <summary>
            /// Gets the max number of elements in the tree
            /// </summary>
            public int MaxElements { get; }

            /// <summary>
            /// Gets the max bit length for the codes
            /// </summary>
            public int MaxBitLength { get; }

            /// <summary>
            /// Returns a readonly reference to the span of code data at index 0.
            /// </summary>
            /// <returns>A reference to the <see cref="CodeData"/> at index 0.</returns>
            [MethodImpl(InliningOptions.ShortMethod)]
            public readonly ref CodeData GetCodeDataReference()
                => ref MemoryMarshal.GetReference(this.staticTree);

            /// <summary>
            /// Returns a readonly reference to the span of extra bit lengths at index 0.
            /// </summary>
            /// <returns>A reference to the <see cref="byte"/> at index 0.</returns>
            [MethodImpl(InliningOptions.ShortMethod)]
            public readonly ref byte GetExtraBitsReference()
                => ref MemoryMarshal.GetReference(this.extraBits);
        }
    }
}
