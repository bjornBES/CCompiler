using System;
using System.Collections.Generic;

namespace CCompiler.ABT
{
    public class Utils
    {

        // class StoreEntry
        // ================
        // the inner storage of entries
        // 
        public class StoreEntry
        {
            public StoreEntry(string name, ExprType type, int offset)
            {
                this.name = name;
                this.type = type;
                this.offset = offset;
            }
            public readonly string name;
            public readonly ExprType type;
            public readonly int offset;
        }

        public static int RoundUp(int value, int alignment)
        {
            return value + alignment - 1 & ~(alignment - 1);
        }

        public static Tuple<int, IReadOnlyList<int>> PackArguments(IReadOnlyList<ExprType> types)
        {
            int alignment = ExprType.SIZEOF_LONG;
            List<int> offsets = new List<int>();
            int offset = 0;
            foreach (ExprType type in types)
            {
                alignment = Math.Max(type.Alignment, alignment);
                offset = RoundUp(offset, alignment);
                offsets.Add(offset);
                offset += type.SizeOf;
            }
            offset = RoundUp(offset, alignment);
            return new Tuple<int, IReadOnlyList<int>>(offset, offsets);
        }

    }
}
