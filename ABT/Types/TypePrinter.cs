using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CCompiler.ABT {
    public abstract partial class ExprType {
        public virtual int Precedence => 0;

        public abstract string Decl(string name, int precedence);

        public string Decl(string name) => Decl(name, 0);

        public string Decl() => Decl("");
    }

    public partial class VoidType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}void {name}".TrimEnd(' ');
    }

    public partial class CharType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}char {name}".TrimEnd(' ');
    }

    public partial class UCharType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}unsigned char {name}".TrimEnd(' ');
    }

    public partial class ShortType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}short {name}".TrimEnd(' ');
    }

    public partial class UShortType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}unsigned short {name}".TrimEnd(' ');
    }

    public partial class LongType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}long {name}".TrimEnd(' ');
    }

    public partial class ULongType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}unsigned long {name}".TrimEnd(' ');
    }

    public partial class FloatType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}float {name}".TrimEnd(' ');
    }

    public partial class DoubleType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}double {name}".TrimEnd(' ');
    }

    public partial class PointerType {
        public override string Decl(string name, int precedence) {
            if (precedence > Precedence) {
                name = $"({name})";
            }
            return RefType.Decl($"*{DumpQualifiers()}{name}", Precedence);
        }
    }

    public partial class IncompleteArrayType {
        public override string Decl(string name, int precedence) {
            if (precedence > Precedence) {
                name = $"({name})";
            }
            return ElemType.Decl($"{name}[]", Precedence);
        }
    }

    public partial class ArrayType {
        public override string Decl(string name, int precedence) {
            if (precedence > Precedence) {
                name = $"({name})";
            }
            return ElemType.Decl($"{name}[{NumElems}]", Precedence);
        }
    }

    public partial class StructOrUnionType {
        public override string Decl(string name, int precedence) =>
            $"{DumpQualifiers()}{_layout.TypeName} {name}".TrimEnd(' ');
    }

    public partial class FunctionType {
        public override string Decl(string name, int precedence) {
            if (precedence > Precedence) {
                name = $"({name})";
            }

            string str = "";
            if (Args.Count == 0) {
                if (HasVarArgs) {
                    str = "(...)";
                } else {
                    str = "(void)";
                }
            } else {
                str = Args[0].type.Decl();
                for (int i = 1; i < Args.Count; ++i) {
                    str += $", {Args[i].type.Decl()}";
                }
                if (HasVarArgs) {
                    str += ", ...";
                }
                str = $"({str})";
            }

            return ReturnType.Decl($"{name}{str}", Precedence);
        }
    }
}
