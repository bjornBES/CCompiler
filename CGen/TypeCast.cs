using System;
using CCompiler.CodeGeneration;

namespace CCompiler.ABT {
    public sealed partial class TypeCast {
        public override Reg CGenValue(CGenState state) {
            Reg ret = this.Expr.CGenValue(state);
            switch (this.Kind) {
                case TypeCastType.DOUBLE_TO_FLOAT:
                case TypeCastType.FLOAT_TO_DOUBLE:
                case TypeCastType.PRESERVE_INT16:
                case TypeCastType.PRESERVE_INT8:
                case TypeCastType.NOP:
                    return ret;

                case TypeCastType.DOUBLE_TO_int:
                case TypeCastType.FLOAT_TO_int:
                    state.CGenConvertFloatToLong();
                    return Reg.AX;

                case TypeCastType.int_TO_DOUBLE:
                case TypeCastType.int_TO_FLOAT:
                    state.CGenConvertLongToFloat();
                    return Reg.AF;

                case TypeCastType.INT16_TO_int:
                    state.MOVL(Reg.A, Reg.AX);
                    return ret;

                case TypeCastType.INT8_TO_INT16:
                case TypeCastType.INT8_TO_int:
                    state.MOVL(Reg.AL, Reg.AX);
                    return ret;

                case TypeCastType.UINT16_TO_Uint:
                    state.MOVL(Reg.A, Reg.AX);
                    return ret;

                case TypeCastType.UINT8_TO_UINT16:
                case TypeCastType.UINT8_TO_Uint:
                    state.MOVL(Reg.AL, Reg.AX);
                    return ret;

                default:
                    throw new InvalidProgramException();
            }
        }

        public override string CGenAddress(CGenState state) {
            throw new InvalidOperationException("Cannot get the address of a cast expression.");
        }
    }
}