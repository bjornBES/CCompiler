using CCompiler.CodeGeneration;

namespace CCompiler.ABT
{
    public abstract partial class Stmt
    {
        public abstract void CGenStmt(Env env, CGenState state);

        public Reg CGenExprStmt(Env env, Expr expr, CGenState state)
        {
            int stack_size = state.StackSize;
            Reg ret = expr.CGenValue(state);
            state.CGenForceStackSizeTo(stack_size);
            return ret;
        }

        public void CGenTest(Reg ret, CGenState state)
        {
            // test Cond
            switch (ret)
            {
                case Reg.AX:
                    state.TESTL(Reg.AX);
                    break;

                case Reg.AF:
                    /// Compare Expr with 0.0
                    /// < see cref = "BinaryComparisonOp.OperateFloat(CGenState)" />
                    state.FLDZ();
                    state.FUCOMIP();
                    state.FSTP(Reg.AF);
                    break;

                default:
                    throw new InvalidProgramException();
            }
        }
    }

    public sealed partial class GotoStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            int label = state.GotoLabel(this.Label);
            state.JMP(label);
        }
    }

    public sealed partial class LabeledStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            state.CGenLabel(state.GotoLabel(this.Label));
            state.CGenForceStackSizeTo(state.StackSize);
            this.Stmt.CGenStmt(env, state);
        }
    }

    public sealed partial class ContStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            int label = state.ContinueLabel;
            state.JMP(label);
        }
    }

    public sealed partial class BreakStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            int label = state.BreakLabel;
            state.JMP(label);
        }
    }

    public sealed partial class ExprStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            if (this.ExprOpt.IsSome)
            {
                int stack_size = state.StackSize;
                this.ExprOpt.Value.CGenValue(state);
                state.CGenForceStackSizeTo(stack_size);
            }
        }
    }

    public sealed partial class CompoundStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            foreach (Tuple<Env, Decln> decln in this.Declns)
            {
                state.COMMENT($"; Current line {decln}");
                decln.Item2.CGenDecln(decln.Item1, state);
            }
            foreach (Tuple<Env, Stmt> stmt in this.Stmts)
            {
                state.COMMENT($"; Current line {stmt}");
                stmt.Item2.CGenStmt(stmt.Item1, state);
            }
        }
    }

    public sealed partial class ReturnStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            ExprType ret_type = env.GetCurrentFunction().ReturnType;

            int stack_size = state.StackSize;

            if (this.ExprOpt.IsSome)
            {
                // Evaluate the Value.
                this.ExprOpt.Value.CGenValue(state);

                // If the function returns a struct, copy it to the address given by 8(%ebp).
                if (this.ExprOpt.Value.Type is StructOrUnionType)
                {
                    state.MOVL(Reg.R1, Reg.AX);
                    state.MOVL(Reg.HL, Reg.BP, 2 * ExprType.SIZEOF_POINTER);
                    state.MOVL(Reg.CX, this.ExprOpt.Value.Type.SizeOf);
                    state.CGenMemCpy();
                    state.MOVL(Reg.AX, Reg.BP, 2 * ExprType.SIZEOF_POINTER);
                }

                // Restore stack size.
                state.CGenForceStackSizeTo(stack_size);
            }
            // Jump to end of the function.
            state.JMP(state.ReturnLabel);
        }
    }

    public sealed partial class WhileStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            int start_label = state.RequestLabel();
            int finish_label = state.RequestLabel();

            // start:
            state.CGenLabel(start_label);

            // test Cond
            Reg ret = CGenExprStmt(env, this.Cond, state);
            CGenTest(ret, state);

            // jz finish
            state.JZ(finish_label);

            // Body
            state.InLoop(start_label, finish_label);
            this.Body.CGenStmt(env, state);
            state.OutLabels();

            // jmp start
            state.JMP(start_label);

            // finish:
            state.CGenLabel(finish_label);

        }
    }

    public sealed partial class DoWhileStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            int start_label = state.RequestLabel();
            int finish_label = state.RequestLabel();
            int continue_label = state.RequestLabel();

            // start:
            state.CGenLabel(start_label);

            // Body
            state.InLoop(continue_label, finish_label);
            this.Body.CGenStmt(env, state);
            state.OutLabels();

            state.CGenLabel(continue_label);

            // test Cond
            Reg ret = CGenExprStmt(env, this.Cond, state);
            CGenTest(ret, state);

            state.JNZ(start_label);

            state.CGenLabel(finish_label);
        }
    }

    public sealed partial class ForStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            // Init
            this.Init.Map(_ => CGenExprStmt(env, _, state));

            int start_label = state.RequestLabel();
            int finish_label = state.RequestLabel();
            int continue_label = state.RequestLabel();

            // start:
            state.CGenLabel(start_label);

            // test cont
            this.Cond.Map(_ =>
            {
                Reg ret = CGenExprStmt(env, _, state);
                CGenTest(ret, state);
                return ret;
            });

            // jz finish
            state.JZ(finish_label);

            // Body
            state.InLoop(continue_label, finish_label);
            this.Body.CGenStmt(env, state);
            state.OutLabels();

            // continue:
            state.CGenLabel(continue_label);

            // Loop
            this.Loop.Map(_ => CGenExprStmt(env, _, state));

            // jmp start
            state.JMP(start_label);

            // finish:
            state.CGenLabel(finish_label);
        }
    }

    public sealed partial class SwitchStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {

            // Inside a switch statement, the initializations are ignored,
            // but the stack size should be changed.
            List<Tuple<Env, Decln>> declns;
            List<Tuple<Env, Stmt>> stmts;

            var compoundStmt = this.Stmt as CompoundStmt;
            if (compoundStmt == null)
            {
                throw new NotImplementedException();
            }

            declns = compoundStmt.Declns;
            stmts = compoundStmt.Stmts;

            // Track all case values.
            IReadOnlyList<int> values = CaseLabelsGrabber.GrabLabels(this);

            // Make sure there are no duplicates.
            if (values.Distinct().Count() != values.Count)
            {
                throw new InvalidOperationException("case labels not unique.");
            }
            // Request labels for these values.
            Dictionary<int, int> value_to_label = values.ToDictionary(value => value, value => state.RequestLabel());

            int label_finish = state.RequestLabel();

            int num_default_stmts = stmts.Count(_ => _.Item2 is DefaultStmt);
            if (num_default_stmts > 1)
            {
                throw new InvalidOperationException("duplicate defaults.");
            }
            int label_default =
                num_default_stmts == 1 ?
                state.RequestLabel() :
                label_finish;

            int saved_stack_size = state.StackSize;
            int stack_size =
                declns.Any() ?
                declns.Last().Item1.StackSize :
                saved_stack_size;

            // 1. Evaluate Expr.
            CGenExprStmt(env, this.Expr, state);

            // 2. Expand stack.
            state.CGenForceStackSizeTo(stack_size);

            // 3. Make the Jump list.
            foreach (KeyValuePair<int, int> value_label_pair in value_to_label)
            {
                state.CMPL(value_label_pair.Key, Reg.AX);
                state.JZ(value_label_pair.Value);
            }
            state.JMP(label_default);

            // 4. List all the statements.
            state.InSwitch(label_finish, label_default, value_to_label);
            foreach (Tuple<Env, Stmt> env_stmt_pair in stmts)
            {
                env_stmt_pair.Item2.CGenStmt(env_stmt_pair.Item1, state);
            }
            state.OutLabels();

            // 5. finish:
            state.CGenLabel(label_finish);

            // 6. Restore stack size.
            state.CGenForceStackSizeTo(saved_stack_size);
        }
    }

    public sealed partial class CaseStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            int label = state.CaseLabel(this.Value);
            state.CGenLabel(label);
            this.Stmt.CGenStmt(env, state);
        }
    }

    public sealed partial class DefaultStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            int label = state.DefaultLabel;
            state.CGenLabel(label);
            this.Stmt.CGenStmt(env, state);
        }
    }

    public sealed partial class IfStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            Reg ret = CGenExprStmt(env, this.Cond, state);

            int finish_label = state.RequestLabel();

            CGenTest(ret, state);

            state.JZ(finish_label);

            this.Stmt.CGenStmt(env, state);

            state.CGenLabel(finish_label);
        }
    }

    public sealed partial class IfElseStmt
    {
        public override void CGenStmt(Env env, CGenState state)
        {
            Reg ret = CGenExprStmt(env, this.Cond, state);

            CGenTest(ret, state);

            int false_label = state.RequestLabel();
            int finish_label = state.RequestLabel();

            state.JZ(false_label);

            this.TrueStmt.CGenStmt(env, state);

            state.JMP(finish_label);

            state.CGenLabel(false_label);

            this.FalseStmt.CGenStmt(env, state);

            state.CGenLabel(finish_label);
        }
    }
}
