using CCompiler.CodeGeneration;

namespace CCompiler.ABT
{
    public abstract partial class BinaryOp
    {
        public override sealed string CGenAddress(CGenState state)
        {
            throw new InvalidOperationException("Cannot get the address of a binary operator.");
        }
    }

    public abstract partial class BinaryOpSupportingIntegralOperands
    {
        /// <summary>
        /// Before calling this method, %eax = Left, %ebx = Right
        /// This method should let %eax = %eax op %ebx
        /// </summary>
        public abstract void OperateLong(CGenState state);

        /// <summary>
        /// Before calling this method, %eax = Left, %ebx = Right
        /// This method should let %eax = %eax op %ebx
        /// </summary>
        public abstract void OperateULong(CGenState state);

        // %eax = left, %ebx = right, stack unchanged
        private void CGenPrepareIntegralOperands(CGenState state)
        {
            // 1. Load Left to EAX.
            // 
            // regs:
            // %eax = Left
            // 
            // stack:
            // +-----+
            // | ... | <- %esp
            // +-----+
            // 
            if (this.Left.CGenValue(state) != Reg.AX)
            {
                throw new InvalidOperationException();
            }

            // 2. Push Left to stack.
            // 
            // regs:
            // %eax = Left
            // 
            // stack:
            // +-----+
            // | ... |
            // +-----+
            // | Left | <- %esp has decreased by 4
            // +-----+
            // 
            int stack_size = state.CGenPushLong(Reg.AX);

            // 3. Load Right to EAX.
            // 
            // regs:
            // %eax = Right
            // 
            // stack:
            // +-----+
            // | ... |
            // +-----+
            // | Left | <- %esp
            // +-----+
            // 
            if (this.Right.CGenValue(state) != Reg.AX)
            {
                throw new InvalidOperationException();
            }

            // 4. Move Right into EBX. Pop Left from stack, into EAX.
            // 
            // regs:
            // %eax = Left
            // %ebx = Right
            // 
            // stack:
            // +-----+
            // | ... | <- %esp has moved back.
            // +-----+
            // 
            state.MOVL(Reg.AX, Reg.BX);
            state.CGenPopLong(stack_size, Reg.AX);
        }

        private Reg CGenLong(CGenState state)
        {
            CGenPrepareIntegralOperands(state);
            OperateLong(state);
            return Reg.AX;
        }

        private Reg CGenULong(CGenState state)
        {
            CGenPrepareIntegralOperands(state);
            OperateULong(state);
            return Reg.AX;
        }

        /// <summary>
        /// 1. %eax = left, %ebx = right, stack unchanged
        /// 2. Operate{Long, ULong}
        /// </summary>
        protected void CGenIntegral(CGenState state)
        {
            // %eax = left, %ebx = right, stack unchanged
            CGenPrepareIntegralOperands(state);

            if (this.Type is LongType)
            {
                // %eax = left op right, stack unchanged
                OperateLong(state);
            }
            else if (this.Type is ULongType)
            {
                // %eax = left op right, stack unchanged
                OperateULong(state);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    public abstract partial class BinaryOpSupportingOnlyIntegralOperands
    {
        public override sealed Reg CGenValue(CGenState state)
        {
            CGenIntegral(state);
            return Reg.AX;
        }
    }

    public abstract partial class BinaryOpSupportingArithmeticOperands
    {
        /// <summary>
        /// Before: %st(0) = left, %st(1) = right, stack unchanged.
        /// After: 'left op right' stored in the correct register.
        /// </summary>
        public abstract void OperateFloat(CGenState state);

        /// <summary>
        /// 1. %st(0) = left, %st(1) = right, stack unchanged
        /// 2. OperateDouble
        /// </summary>
        public void CGenFloat(CGenState state)
        {
            // 1. Load Left to ST0. Now the float stack should only contain one element.
            //
            // memory stack:
            // +-----+
            // |     |
            // | ... | <- %esp
            // +-----+
            //
            // float stack:
            // +-----+
            // | Left | <- %st(0)
            // +-----+
            // 
            var ret = this.Left.CGenValue(state);
            if (ret != Reg.AF)
            {
                throw new InvalidOperationException();
            }

            // 2. Pop from float stack, push into memory stack. Now the float stack should be empty.
            //
            // memory stack:
            // +-----+
            // |     |
            // | ... |
            // | Left | <- %esp has decreased by 4
            // +-----+
            //
            // float stack:
            // +-----+    empty
            // 
            //int stack_size = state.CGenPushFloatP();

            // 3. Load Right to ST0. Now the float stack should only contain one element.
            //
            // memory stack:
            // +-----+
            // |     |
            // | ... |
            // | Left | <- %esp
            // +-----+
            //
            // float stack:
            // +-----+
            // | Right | <- %st(0)
            // +-----+
            // 
            ret = this.Right.CGenValue(state);
            if (ret != Reg.AF)
            {
                throw new InvalidOperationException();
            }

            // 4. Pop double from memory stack, push into float stack. Now both Left and Right are in float stack.
            //
            // memory stack:
            // +-----+
            // |     |
            // | ... | <- %esp
            // +-----+
            //
            // float stack:
            // +-----+
            // | Right | <- %st(1)
            // +-----+
            // | Left | <- %st(0)
            // +-----+
            // 
            //state.CGenPopFloat(stack_size);

            // 5. Perform operation. FPU would pop both operands and push answer back in.
            //
            // memory stack:
            // +-----+
            // |     |
            // | ... | <- %esp
            // +-----+
            //
            // float stack:
            // +-----+
            // | ans | <- %st(0)
            // +-----+
            // 
            OperateFloat(state);
        }

        /// <summary>
        /// 1. %st(0) = left, %st(1) = right, stack unchanged
        /// 2. Operate{Float, Double}
        /// </summary>
        public void CGenArithmetic(CGenState state)
        {
            if (this.Type is FloatType)
            {
                CGenFloat(state);
            }
            else
            {
                CGenIntegral(state);
            }
        }
    }

    public abstract partial class BinaryArithmeticOp
    {
        public override sealed Reg CGenValue(CGenState state)
        {
            CGenArithmetic(state);
            if (this.Type is FloatType || this.Type is DoubleType)
            {
                return Reg.AF;
            }
            else if (this.Type is LongType || this.Type is ULongType)
            {
                return Reg.AX;
            }
            else
            {
                throw new InvalidOperationException("Invalid operand type.");
            }
        }
    }

    public abstract partial class BinaryComparisonOp
    {
        public abstract void SetLong(CGenState state);

        public abstract void SetULong(CGenState state);

        public abstract void SetFloat(CGenState state);

        public abstract void SetDouble(CGenState state);

        public override sealed void OperateLong(CGenState state)
        {
            state.CMPL(Reg.BX, Reg.AX);
            SetLong(state);
            state.MOVZBL(Reg.AL, Reg.AX);
        }

        /// <summary>
        /// <para>Before: %eax = left, %ebx = right, stack unchanged.</para>
        /// <para>After: with SetULong, %eax = left op right, stack unchanged.</para>
        /// </summary>
        public override sealed void OperateULong(CGenState state)
        {
            state.CMPL(Reg.BX, Reg.AX);
            SetULong(state);
            state.MOVZBL(Reg.AL, Reg.AX);
        }

        /// <summary>
        /// <para>Before: %st(0) = left, %st(1) = right, stack unchanged.</para>
        /// <para>After: with SetFloat, %eax = left op right, stack unchanged.</para>
        /// </summary>
        public override sealed void OperateFloat(CGenState state)
        {
            // In the beginning, %st(0) = Left, %st(1) = Right.
            // 
            // float stack:
            // +-----+
            // | rhs | <- %st(1)
            // +-----+
            // | lfs | <- %st(0)
            // +-----+
            // 

            // 1. Do comparison between %st(0) and %st(1).
            //    Pop one Value from FPU stack.
            // 
            // float stack:
            // +-----+
            // | rhs | <- %st(0)
            // +-----+
            // 
            state.FUCOMIP();

            // 2. Pop another Value from FPU stack.
            // 
            // float stack:
            // +-----+ empty
            // 
            state.FSTP(Reg.AF);

            // 3. Set bit based on comparison result.
            SetFloat(state);
            state.MOVZBL(Reg.AL, Reg.AX);
        }

        public override sealed Reg CGenValue(CGenState state)
        {
            CGenArithmetic(state);
            return Reg.AX;
        }
    }

    public sealed partial class Multiply
    {
        public override void OperateLong(CGenState state)
        {
            state.IMUL(Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.MUL(Reg.AX, Reg.BX);
        }

        public override void OperateFloat(CGenState state)
        {
            state.FMULP();
        }

    }

    public sealed partial class Divide
    {
        public override void OperateLong(CGenState state)
        {
            state.IDIVL(Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.DIVL(Reg.BX);
        }

        public override void OperateFloat(CGenState state)
        {
            //state.FDIV();
        }
    }

    public sealed partial class Modulo
    {
        public override void OperateLong(CGenState state)
        {
            state.IDIVL(Reg.BX);
            state.MOVL(Reg.DX, Reg.AX);
        }

        public override void OperateULong(CGenState state)
        {
            state.DIVL(Reg.BX);
            state.MOVL(Reg.DX, Reg.AX);
        }
    }

    public sealed partial class Xor
    {
        public override void OperateLong(CGenState state)
        {
            state.XORL(Reg.BX, Reg.AX);
        }

        public override void OperateULong(CGenState state)
        {
            state.XORL(Reg.BX, Reg.AX);
        }
    }

    public sealed partial class BitwiseOr
    {
        public override void OperateLong(CGenState state)
        {
            state.ORL(Reg.AX, Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.ORL(Reg.AX, Reg.BX);
        }
    }

    public sealed partial class BitwiseAnd
    {
        public override void OperateLong(CGenState state)
        {
            state.ANDL(Reg.AX, Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.ANDL(Reg.AX, Reg.BX);
        }
    }

    public sealed partial class LShift
    {
        public override void OperateLong(CGenState state)
        {
            state.SALL(Reg.AX, Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.SALL(Reg.AX, Reg.BX);
        }
    }

    public sealed partial class RShift
    {
        public override void OperateLong(CGenState state)
        {
            state.SARL(Reg.AX, Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.SHRL(Reg.AX, Reg.BX);
        }
    }

    public sealed partial class Add
    {
        public override void OperateLong(CGenState state)
        {
            state.ADDL(Reg.AX, Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.ADDL(Reg.AX, Reg.BX);
        }

        public override void OperateFloat(CGenState state)
        {
            state.FADD(Reg.AF, Reg.BF);
        }
    }

    public sealed partial class Sub
    {
        public override void OperateLong(CGenState state)
        {
            state.SUB(Reg.AX, Reg.BX);
        }

        public override void OperateULong(CGenState state)
        {
            state.SUB(Reg.AX, Reg.BX);
        }

        public override void OperateFloat(CGenState state)
        {
            state.FSUBP();
        }

    }



    public sealed partial class GEqual
    {
        public override void SetLong(CGenState state)
        {
            state.SETGE(Reg.AL);
        }

        public override void SetULong(CGenState state)
        {
            state.SETNB(Reg.AL);
        }

        public override void SetFloat(CGenState state)
        {
            state.SETNB(Reg.AL);
        }

        public override void SetDouble(CGenState state)
        {
            state.SETNB(Reg.AL);
        }
    }

    public sealed partial class Greater
    {
        public override void SetLong(CGenState state)
        {
            state.SETG(Reg.AL);
        }

        public override void SetULong(CGenState state)
        {
            state.SETA(Reg.AL);
        }

        public override void SetFloat(CGenState state)
        {
            state.SETA(Reg.AL);
        }

        public override void SetDouble(CGenState state)
        {
            state.SETA(Reg.AL);
        }
    }

    public sealed partial class LEqual
    {
        public override void SetLong(CGenState state)
        {
            state.SETLE(Reg.AL);
        }

        public override void SetULong(CGenState state)
        {
            state.SETNA(Reg.AL);
        }

        public override void SetFloat(CGenState state)
        {
            state.SETNA(Reg.AL);
        }

        public override void SetDouble(CGenState state)
        {
            state.SETNA(Reg.AL);
        }
    }

    public sealed partial class Less
    {
        public override void SetLong(CGenState state)
        {
            state.SETL(Reg.AL);
        }

        public override void SetULong(CGenState state)
        {
            state.SETB(Reg.AL);
        }

        public override void SetFloat(CGenState state)
        {
            state.SETB(Reg.AL);
        }

        public override void SetDouble(CGenState state)
        {
            state.SETB(Reg.AL);
        }
    }

    public sealed partial class Equal
    {
        public override void SetLong(CGenState state)
        {
            state.SETE(Reg.AL);
        }

        public override void SetULong(CGenState state)
        {
            state.SETE(Reg.AL);
        }

        public override void SetFloat(CGenState state)
        {
            state.SETE(Reg.AL);
        }

        public override void SetDouble(CGenState state)
        {
            state.SETE(Reg.AL);
        }
    }

    public sealed partial class NotEqual
    {
        public override void SetLong(CGenState state)
        {
            state.SETNE(Reg.AL);
        }

        public override void SetULong(CGenState state)
        {
            state.SETNE(Reg.AL);
        }

        public override void SetFloat(CGenState state)
        {
            state.SETNE(Reg.AL);
        }

        public override void SetDouble(CGenState state)
        {
            state.SETNE(Reg.AL);
        }
    }

    public sealed partial class LogicalAnd
    {
        public override Reg CGenValue(CGenState state)
        {
            int label_reset = state.label_idx;
            state.label_idx++;
            int label_finish = state.label_idx;
            state.label_idx++;

            Reg ret = this.Left.CGenValue(state);
            switch (ret)
            {
                case Reg.AX:
                    state.TESTL(Reg.AX);
                    state.JZ(label_reset);
                    break;

                case Reg.AF:
                    state.FLDZ();
                    state.FUCOMIP();
                    state.FSTP(Reg.AF);
                    state.JZ(label_reset);
                    break;

                default:
                    throw new InvalidProgramException();
            }

            ret = this.Right.CGenValue(state);
            switch (ret)
            {
                case Reg.AX:
                    state.TESTL(Reg.AX);
                    state.JZ(label_reset);
                    break;

                case Reg.AF:
                    state.FLDZ();
                    state.FUCOMIP();
                    state.FSTP(Reg.AF);
                    state.JZ(label_reset);
                    break;

                default:
                    throw new InvalidProgramException();
            }

            state.MOVL(1, Reg.AX);

            state.JMP(label_finish);

            state.CGenLabel(label_reset);

            state.MOVL(0, Reg.AX);

            state.CGenLabel(label_finish);

            return Reg.AX;
        }
    }

    /// <summary>
    /// Left || Right: can only take scalars (to compare with 0).
    /// 
    /// After semantic analysis, each operand can only be
    /// long, ulong, float, double.
    /// Pointers are casted to ulongs.
    /// 
    /// if Left != 0:
    ///     return 1
    /// else:
    ///     return Right != 0
    /// 
    /// Generate the assembly in this fashion,
    /// then every route would only have one jump.
    /// 
    ///        +---------+   1
    ///        | cmp lhs |-------+
    ///        +---------+       |
    ///             |            |
    ///             | 0          |
    ///             |            |
    ///        +----+----+   1   |
    ///        | cmp rhs |-------+
    ///        +---------+       |
    ///             |            |
    ///             | 0          |
    ///             |            |
    ///        +----+----+       |
    ///        | eax = 0 |       |
    ///        +---------+       |
    ///             |            |
    ///   +---------+            |
    ///   |                      |
    ///   |         +------------+ label_set
    ///   |         |
    ///   |    +---------+
    ///   |    | eax = 1 |
    ///   |    +---------+
    ///   |         |
    ///   +---------+ label_finish
    ///             |
    /// 
    /// </summary>
    public sealed partial class LogicalOr
    {
        public override Reg CGenValue(CGenState state)
        {
            int label_set = state.label_idx;
            state.label_idx++;
            int label_finish = state.label_idx;
            state.label_idx++;

            Reg ret = this.Left.CGenValue(state);
            switch (ret)
            {
                case Reg.AX:
                    state.TESTL(Reg.AX);
                    state.JNZ(label_set);
                    break;

                case Reg.AF:
                    state.FLDZ();
                    state.FUCOMIP();
                    state.FSTP(Reg.AF);
                    state.JNZ(label_set);
                    break;

                default:
                    throw new InvalidProgramException();
            }

            ret = this.Right.CGenValue(state);
            switch (ret)
            {
                case Reg.AX:
                    state.TESTL(Reg.AX);
                    state.JNZ(label_set);
                    break;

                case Reg.AF:
                    state.FLDZ();
                    state.FUCOMIP();
                    state.FSTP(Reg.AF);
                    state.JNZ(label_set);
                    break;

                default:
                    throw new InvalidProgramException();
            }

            state.MOVL(0, Reg.AX);

            state.JMP(label_finish);

            state.CGenLabel(label_set);

            state.MOVL(1, Reg.AX);

            state.CGenLabel(label_finish);

            return Reg.AX;
        }
    }
}
