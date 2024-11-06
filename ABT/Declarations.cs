using CCompiler.CodeGeneration;

namespace CCompiler.ABT
{
    public enum StorageClass
    {
        AUTO,
        STATIC,
        EXTERN,
        TYPEDEF
    }

    public sealed class Decln : ExternDecln
    {
        public Decln(string name, StorageClass scs, ExprType type, Option<Initr> initr)
        {
            this.name = name;
            this.scs = scs;
            this.type = type;
            this.initr = initr;
        }

        public override string ToString()
        {
            string str = "[" + scs + "] ";
            str += name;
            str += " : " + type;
            return str;
        }

        // * function;
        // * extern function;
        // * static function;
        // * obj;
        // * obj = Init;
        // * static obj;
        // * static obj = Init;
        // * extern obj;
        // * extern obj = Init;
        public void CGenDecln(Env env, CGenState state)
        {

            if (env.IsGlobal())
            {

                if (initr.IsSome)
                {
                    Initr initr = this.initr.Value;
                    switch (scs)
                    {
                        case StorageClass.AUTO:
                            state.GLOBL(name);
                            break;

                        case StorageClass.EXTERN:
                            throw new InvalidProgramException();

                        case StorageClass.STATIC:
                            break;

                        case StorageClass.TYPEDEF:
                            // Ignore.
                            return;

                        default:
                            throw new InvalidProgramException();
                    }

                    state.DATA();

                    // state.ALIGN(ExprType.ALIGN_LONG);

                    state.CGenLabel(name);

                    int last = 0;
                    initr.Iterate(type, (offset, expr) =>
                    {
                        if (offset > last)
                        {
                            state.ZERO(offset - last);
                        }

                        if (!expr.IsConstExpr)
                        {
                            throw new InvalidOperationException("Cannot initialize with non-const expression.");
                        }

                        switch (expr.Type.Kind)
                        {
                            // TODO: without const char/short, how do I initialize?
                            case ExprTypeKind.CHAR:
                            case ExprTypeKind.UCHAR:
                            case ExprTypeKind.SHORT:
                            case ExprTypeKind.USHORT:
                                throw new NotImplementedException();
                            case ExprTypeKind.LONG:
                                state.LONG(((ConstLong)expr).Value);
                                break;

                            case ExprTypeKind.ULONG:
                                state.LONG((int)((ConstULong)expr).Value);
                                break;

                            case ExprTypeKind.POINTER:
                                state.LONG((int)((ConstPtr)expr).Value);
                                break;

                            case ExprTypeKind.FLOAT:
                                byte[] float_bytes = BitConverter.GetBytes(((ConstFloat)expr).Value);
                                int intval = BitConverter.ToInt32(float_bytes, 0);
                                state.LONG(intval);
                                break;

                            case ExprTypeKind.DOUBLE:
                                byte[] double_bytes = BitConverter.GetBytes(((ConstDouble)expr).Value);
                                int first_int = BitConverter.ToInt32(double_bytes, 0);
                                int second_int = BitConverter.ToInt32(double_bytes, 4);
                                state.LONG(first_int);
                                state.LONG(second_int);
                                break;

                            default:
                                throw new InvalidProgramException();
                        }

                        last = offset + expr.Type.SizeOf;
                    });

                }
                else
                {

                    // Global without initialization.

                    switch (scs)
                    {
                        case StorageClass.AUTO:
                            // .comm name,size,align
                            break;

                        case StorageClass.EXTERN:
                            break;

                        case StorageClass.STATIC:
                            // .local name
                            // .comm name,size,align
                            state.LOCAL(name);
                            break;

                        case StorageClass.TYPEDEF:
                            // Ignore.
                            return;

                        default:
                            throw new InvalidProgramException();
                    }

                    if (type.Kind != ExprTypeKind.FUNCTION)
                    {
                        //state.COMM(name, type.SizeOf, ExprType.ALIGN_LONG);
                    }


                }

                state.NEWLINE();

            }
            else
            {
                // stack object

                state.CGenExpandStack(env.StackSize, ToString());

                int stack_size = env.StackSize;

                // pos should be equal to stack_size, but whatever...
                int pos = env.Find(name).Value.Offset;
                if (this.initr.IsNone)
                {
                    return;
                }

                Initr initr = this.initr.Value;
                initr.Iterate(type, (offset, expr) =>
                {
                    Reg ret = expr.CGenValue(state);
                    switch (expr.Type.Kind)
                    {
                        case ExprTypeKind.CHAR:
                        case ExprTypeKind.UCHAR:
                            state.MOVB(Reg.AX, pos + offset, Reg.BP);
                            break;

                        case ExprTypeKind.SHORT:
                        case ExprTypeKind.USHORT:
                            state.MOVW(Reg.AX, pos + offset, Reg.BP);
                            break;

                        case ExprTypeKind.FLOAT:
                            state.FSTPS(pos + offset, Reg.BP);
                            break;

                        case ExprTypeKind.LONG:
                        case ExprTypeKind.ULONG:
                        case ExprTypeKind.POINTER:
                            state.MOVL(Reg.AX, pos + offset, Reg.BP);
                            break;

                        case ExprTypeKind.STRUCT_OR_UNION:
                            state.MOVL(Reg.AX, Reg.HL);
                            state.MOVL(Reg.R1, Reg.BP);
                            state.ADDL(Reg.R1, pos + offset);
                            state.MOVL(Reg.CX, expr.Type.SizeOf);
                            state.CGenMemCpy();
                            break;

                        case ExprTypeKind.ARRAY:
                        case ExprTypeKind.FUNCTION:
                            throw new InvalidProgramException($"How could a {expr.Type.Kind} be in a init list?");

                        default:
                            throw new InvalidProgramException();
                    }

                    state.CGenForceStackSizeTo(stack_size);

                });

            } // stack object
        }

        private readonly string name;
        private readonly StorageClass scs;
        private readonly ExprType type;
        private readonly Option<Initr> initr;
    }



    /// <summary>
    /// 1. Scalar: an expression, optionally enclosed in braces.
    ///    int a = 1;              // valid
    ///    int a = { 1 };          // valid
    ///    int a[] = { { 1 }, 2 }; // valid
    ///    int a = {{ 1 }};        // warning in gcc, a == 1; error in MSVC
    ///    int a = { { 1 }, 2 };   // warning in gcc, a == 1; error in MSVC
    ///    int a = { 1, 2 };       // warning in gcc, a == 1; error in MSVC
    ///    I'm following MSVC: you either put an expression, or add a single layer of brace.
    /// 
    /// 2. Union:
    ///    union A { int a; int b; };
    ///    union A u = { 1 };               // always initialize the first member, i.e. a, not b.
    ///    union A u = {{ 1 }};             // valid
    ///    union A u = another_union;       // valid
    /// 
    /// 3. Struct:
    ///    struct A { int a; int b; };
    ///    struct A = another_struct;       // valid
    ///    struct A = { another_struct };   // error, once you put a brace, the compiler assumes you want to initialize members.
    /// 
    /// From 2 and 3, once seen union or struct, either read expression or brace.
    /// 
    /// 4. Array of characters:
    ///    char a[] = { 'a', 'b' }; // valid
    ///    char a[] = "abc";        // becomes char a[4]: include '\0'
    ///    char a[3] = "abc";       // valid, ignore '\0'
    ///    char a[2] = "abc";       // warning in gcc; error in MSVC
    ///    If the aggregate contains members that are aggregates or unions, or if the first member of a union is an aggregate or union, the rules apply recursively to the subaggregates or contained unions. If the initializer of a subaggregate or contained union begins with a left brace, the initializers enclosed by that brace and its matching right brace initialize the members of the subaggregate or the first member of the contained union. Otherwise, only enough initializers from the list are taken to account for the members of the first subaggregate or the first member of the contained union; any remaining initializers are left to initialize the next member of the aggregate of which the current subaggregate or contained union is a part.
    /// </summary>
    public abstract class Initr
    {
        public enum Kind
        {
            EXPR,
            INIT_LIST
        }
        public abstract Kind kind { get; }

        public abstract Initr ConformType(MemberIterator iter);

        public Initr ConformType(ExprType type) => ConformType(new MemberIterator(type));

        public abstract void Iterate(MemberIterator iter, Action<int, Expr> action);

        public void Iterate(ExprType type, Action<int, Expr> action) => Iterate(new MemberIterator(type), action);
    }

    public class InitExpr : Initr
    {
        public InitExpr(Expr expr)
        {
            this.expr = expr;
        }
        public readonly Expr expr;
        public override Kind kind => Kind.EXPR;

        public override Initr ConformType(MemberIterator iter)
        {
            iter.Locate(this.expr.Type);
            Expr expr = TypeCast.MakeCast(this.expr, iter.CurType);
            return new InitExpr(expr);
        }

        public override void Iterate(MemberIterator iter, Action<int, Expr> action)
        {
            iter.Locate(this.expr.Type);
            int offset = iter.CurOffset;
            Expr expr = this.expr;
            action(offset, expr);
        }
    }

    public class InitList : Initr
    {
        public InitList(List<Initr> initrs)
        {
            this.initrs = initrs;
        }
        public override Kind kind => Kind.INIT_LIST;
        public readonly List<Initr> initrs;

        public override Initr ConformType(MemberIterator iter)
        {
            iter.InBrace();
            List<Initr> initrs = new List<Initr>();
            for (int i = 0; i < initrs.Count; ++i)
            {
                initrs.Add(initrs[i].ConformType(iter));
                if (i != initrs.Count - 1)
                {
                    iter.Next();
                }
            }
            iter.OutBrace();
            return new InitList(initrs);
        }

        public override void Iterate(MemberIterator iter, Action<int, Expr> action)
        {
            iter.InBrace();
            for (int i = 0; i < initrs.Count; ++i)
            {
                initrs[i].Iterate(iter, action);
                if (i != initrs.Count - 1)
                {
                    iter.Next();
                }
            }
            iter.OutBrace();
        }
    }

    public class MemberIterator
    {
        public MemberIterator(ExprType type)
        {
            trace = new List<Status> { new Status(type) };
        }

        public class Status
        {
            public Status(ExprType base_type)
            {
                this.base_type = base_type;
                indices = new List<int>();
            }

            public ExprType CurType => GetType(base_type, indices);

            public int CurOffset => GetOffset(base_type, indices);

            //public List<Tuple<ExprType, int>> GetPath(ExprType base_type, IReadOnlyList<int> indices) {
            //    ExprType Type = base_type;
            //    List<Tuple<ExprType, int>> path = new List<Tuple<ExprType, int>>();
            //    foreach (int index in indices) {
            //        switch (Type.Kind) {
            //            case ExprType.Kind.ARRAY:
            //                Type = ((ArrayType)Type).ElemType;
            //                break;
            //            case ExprType.Kind.INCOMPLETE_ARRAY:
            //            case ExprType.Kind.STRUCT_OR_UNION:
            //            default:
            //                throw new InvalidProgramException("Not an aggregate Type.");
            //        }
            //    }
            //}

            public static ExprType GetType(ExprType from_type, int to_index)
            {
                switch (from_type.Kind)
                {
                    case ExprTypeKind.ARRAY:
                        return ((ArrayType)from_type).ElemType;

                    case ExprTypeKind.INCOMPLETE_ARRAY:
                        return ((IncompleteArrayType)from_type).ElemType;

                    case ExprTypeKind.STRUCT_OR_UNION:
                        return ((StructOrUnionType)from_type).Attribs[to_index].type;

                    default:
                        throw new InvalidProgramException("Not an aggregate Type.");
                }
            }

            public static ExprType GetType(ExprType base_type, IReadOnlyList<int> indices) =>
                indices.Aggregate(base_type, GetType);

            public static int GetOffset(ExprType from_type, int to_index)
            {
                switch (from_type.Kind)
                {
                    case ExprTypeKind.ARRAY:
                        return to_index * ((ArrayType)from_type).ElemType.SizeOf;

                    case ExprTypeKind.INCOMPLETE_ARRAY:
                        return to_index * ((IncompleteArrayType)from_type).ElemType.SizeOf;

                    case ExprTypeKind.STRUCT_OR_UNION:
                        return ((StructOrUnionType)from_type).Attribs[to_index].offset;

                    default:
                        throw new InvalidProgramException("Not an aggregate Type.");
                }
            }

            public static int GetOffset(ExprType base_type, IReadOnlyList<int> indices)
            {
                int offset = 0;
                ExprType from_type = base_type;
                foreach (int to_index in indices)
                {
                    offset += GetOffset(from_type, to_index);
                    from_type = GetType(from_type, to_index);
                }
                return offset;
            }

            public List<ExprType> GetTypes(ExprType base_type, IReadOnlyList<int> indices)
            {
                List<ExprType> types = new List<ExprType> { base_type };
                ExprType from_type = base_type;
                foreach (int to_index in indices)
                {
                    from_type = GetType(from_type, to_index);
                    types.Add(from_type);
                }
                return types;
            }

            public void Next()
            {

                // From base_type to CurType.
                List<ExprType> types = GetTypes(base_type, indices);

                // We try to jump as many levels out as we can.
                do
                {
                    int index = indices.Last();
                    indices.RemoveAt(indices.Count - 1);

                    types.RemoveAt(types.Count - 1);
                    ExprType type = types.Last();

                    switch (type.Kind)
                    {
                        case ExprTypeKind.ARRAY:
                            if (index < ((ArrayType)type).NumElems - 1)
                            {
                                // There are more elements in the array.
                                indices.Add(index + 1);
                                return;
                            }
                            break;

                        case ExprTypeKind.INCOMPLETE_ARRAY:
                            indices.Add(index + 1);
                            return;

                        case ExprTypeKind.STRUCT_OR_UNION:
                            if (((StructOrUnionType)type).IsStruct && index < ((StructOrUnionType)type).Attribs.Count - 1)
                            {
                                // There are more members in the struct.
                                // (not union, since we can only initialize the first member of a union)
                                indices.Add(index + 1);
                                return;
                            }
                            break;

                        default:
                            break;
                    }

                } while (indices.Any());
            }

            /// <summary>
            /// Read an expression in the initializer list, locate the corresponding position.
            /// </summary>
            public void Locate(ExprType type)
            {
                switch (type.Kind)
                {
                    case ExprTypeKind.STRUCT_OR_UNION:
                        LocateStruct((StructOrUnionType)type);
                        return;
                    default:
                        // Even if the expression is of array Type, treat it as a scalar (pointer).
                        LocateScalar();
                        return;
                }
            }

            /// <summary>
            /// Try to match a scalar.
            /// This step doesn't check what scalar it is. Further steps would perform implicit conversions.
            /// </summary>
            private void LocateScalar()
            {
                while (!CurType.IsScalar)
                {
                    indices.Add(0);
                }
            }

            /// <summary>
            /// Try to match a given struct.
            /// Go down to find the first element of the same struct Type.
            /// </summary>
            private void LocateStruct(StructOrUnionType type)
            {
                while (!CurType.EqualType(type))
                {
                    if (CurType.IsScalar)
                    {
                        throw new InvalidOperationException("Trying to match a struct or union, but found a scalar.");
                    }

                    // Go down one level.
                    indices.Add(0);
                }
            }

            public readonly ExprType base_type;
            public readonly List<int> indices;
        }

        public ExprType CurType => trace.Last().CurType;

        public int CurOffset => trace.Select(_ => _.CurOffset).Sum();

        public void Next() => trace.Last().Next();

        public void Locate(ExprType type) => trace.Last().Locate(type);

        public void InBrace()
        {

            /// Push the current position into the stack, so that we can get back by <see cref="OutBrace"/>
            trace.Add(new Status(trace.Last().CurType));

            // For aggregate types, go inside and locate the first member.
            if (!CurType.IsScalar)
            {
                trace.Last().indices.Add(0);
            }

        }

        public void OutBrace() => trace.RemoveAt(trace.Count - 1);

        public readonly List<Status> trace;
    }
}
