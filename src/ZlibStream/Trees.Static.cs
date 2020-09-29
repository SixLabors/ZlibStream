// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SixLabors.ZlibStream
{
    internal sealed unsafe partial class Trees
    {
        /// <summary>
        /// Initializes static members of the <see cref="Trees"/> class.
        /// </summary>
        static Trees()
        {
            MakeStaticTrees();

            StaticLDesc = new StaticTreeDesc(StaticLTree, ExtraLbits, LITERALS + 1, LCODES, MAXBITS);
            StaticDDesc = new StaticTreeDesc(StaticLTree, ExtraDbits, 0, DCODES, MAXBITS);
            StaticBlDesc = new StaticTreeDesc(null, ExtraBlbits, 0, BLCODES, MAXBLBITS);
        }

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
        /// Gets the StaticLTree descriptor.
        /// </summary>
        public static StaticTreeDesc StaticLDesc { get; }

        /// <summary>
        /// Gets the StaticDTree descriptor.
        /// </summary>
        public static StaticTreeDesc StaticDDesc { get; }

        /// <summary>
        /// Gets the StaticBTree descriptor.
        /// </summary>
        public static StaticTreeDesc StaticBlDesc { get; }

        private static void MakeStaticTrees()
        {
            // TODO: A lot of other arrays are created before we create the static trees.
            fixed (CodeData* staticLTreePtr = &StaticLTree.DangerousGetReference())
            fixed (CodeData* staticDTreePtr = &StaticDTtree.DangerousGetReference())
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
        /// Generate the codes for a given tree and bit counts (which need not be
        /// optimal).
        /// IN assertion: the array bl_count contains the bit length statistics for
        /// the given tree and the field len is set for all tree elements.
        /// OUT assertion: the field code is set for all tree elements of non zero code length.
        /// </summary>
        [MethodImpl(InliningOptions.ShortMethod)]
        private static void Gen_codes(CodeData* tree, int max_code, ushort* bl_count)
        {
            ushort* next_code = stackalloc ushort[MAXBITS + 1]; // next code value for each bit length

            ushort code = 0; // running code value
            int bits; // bit index
            int n; // code index

            // The distribution counts are first used to generate the code values
            // without bit reversal.
            for (bits = 1; bits <= MAXBITS; bits++)
            {
                next_code[bits] = code = (ushort)((code + bl_count[bits - 1]) << 1);
            }

            // Check that the bit counts in bl_count are consistent. The last code
            // must be all ones.
            for (n = 0; n <= max_code; n++)
            {
                int len = tree[n].Len;
                if (len == 0)
                {
                    continue;
                }

                // Now reverse the bits
                tree[n].Code = (ushort)Bi_reverse(next_code[len]++, len);
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
        public class StaticTreeDesc
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StaticTreeDesc"/> class.
            /// </summary>
            /// <param name="static_tree">static tree.</param>
            /// <param name="extra_bits">extra bits.</param>
            /// <param name="extra_base">extra base.</param>
            /// <param name="elems">emax lements.</param>
            /// <param name="max_length">max length.</param>
            public StaticTreeDesc(
                CodeData[] static_tree,
                int[] extra_bits,
                int extra_base,
                int elems,
                int max_length)
            {
                this.StaticTreeValue = static_tree;
                this.ExtraBits = extra_bits;
                this.ExtraBase = extra_base;
                this.Elems = elems;
                this.MaxLength = max_length;
            }

            /// <summary>
            /// Gets the static tree or null.
            /// </summary>
            public CodeData[] StaticTreeValue { get; }

            /// <summary>
            /// Gets the extra bits for each code or null.
            /// </summary>
            public int[] ExtraBits { get; }

            /// <summary>
            /// Gets the  base index for extra_bits.
            /// </summary>
            public int ExtraBase { get; }

            /// <summary>
            /// Gets the max number of elements in the tree
            /// </summary>
            public int Elems { get; }

            /// <summary>
            /// Gets the max bit length for the codes
            /// </summary>
            public int MaxLength { get; }
        }
    }
}
