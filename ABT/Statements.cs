using System;
using System.Collections.Generic;

namespace CCompiler.ABT
{
    //public enum StmtKind {
    //    GOTO,
    //    LABELED,
    //    CONT,
    //    BREAK,
    //    EXPR,
    //    COMPOUND,
    //    RETURN,
    //    WHILE,
    //    DO,
    //    FOR,
    //    SWITCH,
    //    CASE,
    //    DEFAULT,
    //    IF,
    //    IF_ELSE
    //}

    public abstract partial class Stmt
    {
        public Env Env { get; }

        public abstract void Accept(StmtVisitor visitor);
    }

    /// <summary>
    /// Goto Statement
    /// </summary>
    public sealed partial class GotoStmt : Stmt
    {
        public GotoStmt(string label)
        {
            Label = label;
        }

        public string Label { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Labeled Statement
    /// </summary>
    public sealed partial class LabeledStmt : Stmt
    {
        public LabeledStmt(string label, Stmt stmt)
        {
            Label = label;
            Stmt = stmt;
        }

        public string Label { get; }

        public Stmt Stmt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Continue Statement
    /// </summary>
    public sealed partial class ContStmt : Stmt
    {
        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Break Statement
    /// </summary>
    public sealed partial class BreakStmt : Stmt
    {
        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Expression Statement
    /// </summary>
    public sealed partial class ExprStmt : Stmt
    {
        public ExprStmt(Option<Expr> exprOpt)
        {
            ExprOpt = exprOpt;
        }

        public Option<Expr> ExprOpt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed partial class CompoundStmt : Stmt
    {
        public CompoundStmt(List<Tuple<Env, Decln>> declns, List<Tuple<Env, Stmt>> stmts)
        {
            Declns = declns;
            Stmts = stmts;
        }

        public List<Tuple<Env, Decln>> Declns { get; }

        public List<Tuple<Env, Stmt>> Stmts { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed partial class ReturnStmt : Stmt
    {
        public ReturnStmt(Option<Expr> exprOpt)
        {
            ExprOpt = exprOpt;
        }

        public Option<Expr> ExprOpt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// While Statement
    /// 
    /// while (Cond) {
    ///     Body
    /// }
    /// 
    /// Cond must be of scalar Type
    /// </summary>
    // +--> start: continue:
    // |        test Cond
    // |        jz finish --+
    // |        Body        |
    // +------- jmp start   |
    //      finish: <-------+
    // 
    public sealed partial class WhileStmt : Stmt
    {
        public WhileStmt(Expr cond, Stmt body)
        {
            if (!cond.Type.IsScalar)
            {
                throw new InvalidProgramException();
            }
            Cond = cond;
            Body = body;
        }

        public Expr Cond { get; }

        public Stmt Body { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Do-while Stmt
    /// 
    /// do {
    ///     Body
    /// } while (Cond);
    /// 
    /// Cond must be of scalar Type
    /// </summary>
    // +--> start:
    // |        Body
    // |    continue:
    // |        test Cond
    // +------- jnz start
    //      finish:
    public sealed partial class DoWhileStmt : Stmt
    {
        public DoWhileStmt(Stmt body, Expr cond)
        {
            Body = body;
            Cond = cond;
        }

        public Stmt Body { get; }

        public Expr Cond { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// for (Init; Cond; Loop) {
    ///     Body
    /// }
    /// 
    /// Cond must be scalar
    /// </summary>
    // 
    //          Init
    // +--> start:
    // |        test Cond
    // |        jz finish --+
    // |        Body        |
    // |    continue:       |
    // |        Loop        |
    // +------- jmp start   |
    //      finish: <-------+
    // 
    public sealed partial class ForStmt : Stmt
    {
        public ForStmt(Option<Expr> init, Option<Expr> cond, Option<Expr> loop, Stmt body)
        {
            Init = init;
            Cond = cond;
            Loop = loop;
            Body = body;
        }

        public Option<Expr> Init { get; }

        public Option<Expr> Cond { get; }

        public Option<Expr> Loop { get; }

        public Stmt Body { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Switch Statement
    /// </summary>
    //
    //     cmp Cond, value1
    //     je case1
    //     cmp Cond, value2
    //     je case2
    //     ...
    //     cmp Cond, value_n
    //     je case_n
    //     jmp default # if no default, then default = finish
    //     
    // case1:
    //     stmt
    // case2:
    //     stmt
    // ...
    // case_n:
    //     stmt
    // finish:
    public sealed partial class SwitchStmt : Stmt
    {
        public SwitchStmt(Expr expr, Stmt stmt)
        {
            Expr = expr;
            Stmt = stmt;
        }

        public Expr Expr { get; }

        public Stmt Stmt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// Case Statement
    /// </summary>
    public sealed partial class CaseStmt : Stmt
    {
        public CaseStmt(int value, Stmt stmt)
        {
            Value = value;
            Stmt = stmt;
        }

        public int Value { get; }

        public Stmt Stmt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public sealed partial class DefaultStmt : Stmt
    {
        public DefaultStmt(Stmt stmt)
        {
            Stmt = stmt;
        }

        public Stmt Stmt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// If Statement: if (Cond) stmt;
    /// If Cond is non-zero, stmt is executed.
    /// 
    /// Cond must be arithmetic or pointer Type.
    /// </summary>
    //          test Cond
    // +------- jz finish
    // |        Body
    // +--> finish:
    public sealed partial class IfStmt : Stmt
    {
        public IfStmt(Expr cond, Stmt stmt)
        {
            Cond = cond;
            Stmt = stmt;
        }

        public Expr Cond { get; }

        public Stmt Stmt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    /// <summary>
    /// If-else Statement
    /// if (Cond) {
    ///     true_stmt
    /// } else {
    ///     false_stmt
    /// }
    /// </summary>
    ///
    //          test Cond
    // +------- jz false
    // |        true_stmt
    // |        jmp finish --+
    // +--> false:           |
    //          false_stmt   |
    //      finish: <--------+
    // 
    public sealed partial class IfElseStmt : Stmt
    {
        public IfElseStmt(Expr cond, Stmt trueStmt, Stmt falseStmt)
        {
            Cond = cond;
            TrueStmt = trueStmt;
            FalseStmt = falseStmt;
        }

        public Expr Cond { get; }

        public Stmt TrueStmt { get; }

        public Stmt FalseStmt { get; }

        public override void Accept(StmtVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}