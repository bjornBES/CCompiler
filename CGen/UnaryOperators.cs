using System;
using System.Diagnostics;
using CCompiler.CodeGeneration;

namespace CCompiler.ABT {
    public abstract partial class IncDecExpr {

        // Integral
        // Before the actual calculation, the state is set to this.
        // 
        // regs:
        // %eax = expr
        // %ebx = expr
        // %ecx = &expr
        // (Yes, both %eax and %ebx are expr.)
        // 
        // stack:
        // +-------+
        // | ..... | <- %esp
        // +-------+
        // 
        // After the calculation, the result should be in %eax,
        // and memory should be updated.
        //
        public abstract void CalcAndSaveLong(CGenState state);

        public abstract void CalcAndSaveWord(CGenState state);

        public abstract void CalcAndSaveByte(CGenState state);

        public abstract void CalcAndSavePtr(CGenState state);

        // Float
        // Before the actual calculation, the state is set to this.
        // 
        // regs:
        // %ecx = &expr
        // 
        // stack:
        // +-------+
        // | ..... | <- %esp
        // +-------+
        // 
        // float stack:
        // +-------+
        // | expr  | <- %st(1)
        // +-------+
        // |  1.0  | <- %st(0)
        // +-------+
        // 
        // After the calculation, the result should be in %st(0),
        // and memory should be updated.
        // 
        public abstract void CalcAndSaveFloat(CGenState state);

        public override sealed Reg CGenValue(CGenState state) {

            // 1. Get the address of expr.
            // 
            // regs:
            // %eax = &expr
            // 
            // stack:
            // +-------+
            // | ..... | <- %esp
            // +-------+
            // 
            string address = this.Expr.CGenAddress(state);

            // 3. Get current Value of expr.
            // 
            // 1) If expr is an integral or pointer:
            // 
            // regs:
            // %eax = expr
            // 
            // stack:
            // +-------+
            // | ..... |
            // +-------+
            // | &expr | <- %esp
            // +-------+
            // 
            // 
            // 2) If expr is a float:
            // 
            // regs:
            // %eax = &expr
            // 
            // stack:
            // +-------+
            // | ..... |
            // +-------+
            // | &expr | <- %esp
            // +-------+
            // 
            // float stack:
            // +-------+
            // | expr  | <- %st(0)
            // +-------+
            // 
            Reg ret = this.Expr.CGenValue(state);

            switch (ret) {
                case Reg.AX:
                    // expr is an integral or pointer.
                    switch (this.Expr.Type.Kind) {
                        case ExprTypeKind.CHAR:
                        case ExprTypeKind.UCHAR:
                            CalcAndSaveByte(state);
                            state.MOVL(ret, address);
                            return Reg.AX;

                        case ExprTypeKind.SHORT:
                        case ExprTypeKind.USHORT:
                            CalcAndSaveWord(state);
                            state.MOVL(ret, address);
                            return Reg.AX;

                        case ExprTypeKind.LONG:
                        case ExprTypeKind.ULONG:
                            CalcAndSaveLong(state);
                            state.MOVL(ret, address);
                            return Reg.AX;

                        case ExprTypeKind.POINTER:
                            CalcAndSavePtr(state);
                            state.MOVL(ret, address);
                            return Reg.AX;

                        default:
                            throw new InvalidProgramException();
                    }
                    /*
                case Reg.ST0:
                    // Expr is a float.

                    // 4. Pop address to %ecx.
                    // 
                    // regs:
                    // %ecx = &expr
                    // 
                    // stack:
                    // +-------+
                    // | ..... | <- %esp
                    // +-------+
                    // 
                    state.CGenPopLong(stack_size, Reg.CX);

                    // 5. Load 1.0 to FPU stack.
                    // 
                    // regs:
                    // %ecx = &expr
                    // 
                    // stack:
                    // +-------+
                    // | ..... | <- %esp
                    // +-------+
                    // 
                    // float stack:
                    // +-------+
                    // | expr  | <- %st(1)
                    // +-------+
                    // |  1.0  | <- %st(0)
                    // +-------+
                    // 
                    state.FLD1();

                    // 6. Calculate the new value and save back.
                    //    Set %st(0) to be the new or original Value.
                    // 
                    // regs:
                    // %ecx = &Expr
                    // 
                    // stack:
                    // +-------+
                    // | ..... | <- %esp
                    // +-------+
                    // 
                    // float stack:
                    // +---------------------+
                    // | expr or (epxr +- 1) | <- %st(0)
                    // +---------------------+
                    // 
                    switch (this.Expr.Type.Kind) {
                        case ExprTypeKind.FLOAT:
                            CalcAndSaveFloat(state);
                            return Reg.ST0;

                        case ExprTypeKind.DOUBLE:
                            CalcAndSaveDouble(state);
                            return Reg.ST0;

                        default:
                            throw new InvalidProgramException();
                    }
                    */
                default:
                    throw new InvalidProgramException();
            }

        }

        public override sealed string CGenAddress(CGenState state) {
            throw new InvalidOperationException(
                "Cannot get the address of an increment/decrement expression."
            );
        }
    }

    public sealed partial class PostIncrement {
        public override void CalcAndSaveLong(CGenState state) {
            state.INC(Reg.AX);
        }

        public override void CalcAndSaveWord(CGenState state) {
            state.INC(Reg.A);
        }

        public override void CalcAndSaveByte(CGenState state) {
            state.INC(Reg.AL);
        }

        public override void CalcAndSavePtr(CGenState state) {
            state.ADDL(Reg.BX, this.Expr.Type.SizeOf);
            state.MOVL(Reg.BX, 0, Reg.CX);
        }

        public override void CalcAndSaveFloat(CGenState state) {
            state.FADD(1, 0);
            state.FSTPS(0, Reg.CX);
        }

    }

    public sealed partial class PostDecrement {
        public override void CalcAndSaveLong(CGenState state) {
            state.DEC(Reg.AX);
        }

        public override void CalcAndSaveWord(CGenState state) {
            state.DEC(Reg.A);
        }

        public override void CalcAndSaveByte(CGenState state) {
            state.DEC(Reg.AL);
        }

        public override void CalcAndSavePtr(CGenState state) {
            state.SUB(Reg.BX, this.Expr.Type.SizeOf);
            state.MOVL(Reg.BX, 0, Reg.CX);
        }

        public override void CalcAndSaveFloat(CGenState state) {
            state.FSUB(1, 0);
            state.FSTPS(0, Reg.CX);
        }

    }

    public sealed partial class PreIncrement {
        public override void CalcAndSaveLong(CGenState state) {
            state.ADDL(Reg.AX, 1);
            state.MOVL(Reg.AX, 0, Reg.CX);
        }

        public override void CalcAndSaveWord(CGenState state) {
            state.ADDL(Reg.AX, 1);
            state.MOVW(Reg.A, 0, Reg.CX);
        }

        public override void CalcAndSaveByte(CGenState state) {
            state.ADDL(Reg.AX, 1);
            state.MOVB(Reg.AL, 0, Reg.CX);
        }

        public override void CalcAndSavePtr(CGenState state) {
            state.ADDL(Reg.AX, this.Expr.Type.SizeOf);
            state.MOVL(Reg.AX, 0, Reg.CX);
        }

        public override void CalcAndSaveFloat(CGenState state) {
            state.FADD(1, 0);
            state.FSTS(0, Reg.CX);
        }

    }

    public sealed partial class PreDecrement {
        public override void CalcAndSaveLong(CGenState state) {
            state.SUB(Reg.AX, 1);
            state.MOVL(Reg.AX, 0, Reg.CX);
        }

        public override void CalcAndSaveWord(CGenState state) {
            state.SUB(Reg.AX, 1);
            state.MOVW(Reg.AX, 0, Reg.CX);
        }

        public override void CalcAndSaveByte(CGenState state) {
            state.SUB(Reg.AX, 1);
            state.MOVB(Reg.AL, 0, Reg.CX);
        }

        public override void CalcAndSavePtr(CGenState state) {
            state.SUB(Reg.AX, this.Expr.Type.SizeOf);
            state.MOVL(Reg.AX, 0, Reg.CX);
        }

        public override void CalcAndSaveFloat(CGenState state) {
            state.FSUB(1, 0);
            state.FSTS(0, Reg.CX);
        }

    }

    public abstract partial class UnaryArithOp {
        public override sealed string CGenAddress(CGenState state) {
            throw new InvalidOperationException(
                "Cannot get the address of an unary arithmetic operator."
            );
        }
    }

    public sealed partial class Negative {
        public override Reg CGenValue(CGenState state) {
            Reg ret = this.Expr.CGenValue(state);
            switch (ret) {
                case Reg.AX:
                    state.NEG(Reg.AX);
                    return Reg.AX;

                case Reg.AF:
                    // TODO state.FCHS();
                    return Reg.AF;

                default:
                    throw new InvalidProgramException();
            }
        }
    }

    public sealed partial class BitwiseNot {
        public override Reg CGenValue(CGenState state) {
            Reg ret = this.Expr.CGenValue(state);
            if (ret != Reg.AX) {
                throw new InvalidProgramException();
            }
            state.NOT(Reg.AX);
            return Reg.AX;
        }
    }

    public sealed partial class LogicalNot {
        public override Reg CGenValue(CGenState state) {
            Reg ret = this.Expr.CGenValue(state);
            switch (ret) {
                case Reg.AX:
                    state.TESTL(Reg.AX);
                    state.SETE(Reg.AL);
                    state.MOVZBL(Reg.AL, Reg.AX);
                    return Reg.AX;

                case Reg.AF:
                    /// Compare Expr with 0.0
                    /// < see cref = "BinaryComparisonOp.OperateFloat(CGenState)" />
                    state.FLDZ();
                    state.FUCOMIP();
                    state.FSTP(Reg.AF);
                    state.SETE(Reg.AL);
                    state.MOVZBL(Reg.AL, Reg.AX);
                    return Reg.AX;

                default:
                    throw new InvalidProgramException();
            }
        }
    }
}
