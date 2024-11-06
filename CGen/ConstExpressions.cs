using System;
using CCompiler.CodeGeneration;

namespace CCompiler.ABT {
    public abstract partial class ConstExpr {
        public override sealed string CGenAddress(CGenState state) {
            throw new InvalidOperationException("Cannot get the address of a constant");
        }
    }

    public sealed partial class ConstLong {
        public override Reg CGenValue(CGenState state) {
            state.MOVL(Reg.AX, Value);
            return Reg.AX;
        }
    }

    public sealed partial class ConstULong {
        public override Reg CGenValue(CGenState state) {
            state.MOVL(Reg.AX, (int)Value);
            return Reg.AX;
        }
    }

    public sealed partial class ConstShort {
        public override Reg CGenValue(CGenState state) {
            state.MOVL(Reg.AX, Value);
            return Reg.AX;
        }
    }

    public sealed partial class ConstUShort {
        public override Reg CGenValue(CGenState state) {
            state.MOVL(Reg.AX, Value);
            return Reg.AX;
        }
    }

    public sealed partial class ConstChar {
        public override Reg CGenValue(CGenState state) {
            state.MOVL(Reg.AX, Value);
            return Reg.AX;
        }
    }

    public sealed partial class ConstUChar {
        public override Reg CGenValue(CGenState state) {
            state.MOVL(Reg.AX, Value);
            return Reg.AX;
        }
    }

    public sealed partial class ConstPtr {
        public override Reg CGenValue(CGenState state) {
            state.MOVL(Reg.AX, (int)Value);
            return Reg.AX;
        }
    }

    public sealed partial class ConstFloat {
        /// <summary>
        /// flds addr
        /// </summary>
        public override Reg CGenValue(CGenState state) {
            byte[] bytes = BitConverter.GetBytes(Value);
            int intval = BitConverter.ToInt32(bytes, 0);
            string name = state.CGenLongConst(intval);
            // state.FLDS(name);
            return Reg.AF;
        }
    }

    public sealed partial class ConststringLiteral {
        public override Reg CGenValue(CGenState state) {
            string name = state.CGenstring(Value);
            state.MOVL(Reg.AX, name);
            return Reg.AX;
        }
    }
}