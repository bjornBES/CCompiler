using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CCompiler.ABT;

namespace CCompiler.ABT
{
    // Expr 
    // ========================================================================

    /// <summary>
    /// The cdecl calling convention:
    /// 1. arguments are passed on the stack, right to left.
    /// 2. int values and pointer values are returned in %eax.
    /// 3. floats are returned in %st(0).
    /// 4. when calling a function, %st(0) ~ %st(7) are all free.
    /// 5. functions are free to use %eax, %ecx, %edx, because caller needs to save them.
    /// 6. stack must be aligned to 4 bytes (before gcc 4.5, for gcc 4.5+, aligned to 16 bytes).
    /// </summary>

    public abstract partial class Expr
    {
        protected Expr() { }

        /// <summary>
        /// Whether the Value is known at compile time.
        /// </summary>
        public virtual bool IsConstExpr => false;

        /// <summary>
        /// Whether the expression refers to an object (that can be assigned to).
        /// </summary>
        public abstract bool IsLValue { get; }

        public abstract Env Env { get; }

        public abstract ExprType Type { get; }
    }

    public sealed partial class Variable : Expr
    {
        public Variable(ExprType type, string name, Env env)
        {
            Name = name;
            Env = env;
            Type = type;
        }

        public string Name { get; }

        public override Env Env { get; }

        public override ExprType Type { get; }

        public override bool IsLValue => !(Type is FunctionType);
    }

    public sealed partial class AssignList : Expr
    {
        public AssignList(ImmutableList<Expr> exprs)
        {
            if (exprs.Count == 0)
            {
                throw new InvalidOperationException("Need at least one expression.");
            }
            Exprs = exprs;
        }

        public ImmutableList<Expr> Exprs { get; }

        public override Env Env => Exprs.Last().Env;

        public override bool IsLValue => false;

        public override ExprType Type => Exprs.Last().Type;
    }

    public sealed partial class Assign : Expr
    {
        public Assign(Expr left, Expr right)
        {
            Left = left;
            Right = right;

            if (!Left.IsLValue)
            {
                throw new InvalidOperationException("Can only assign to lvalue.");
            }
        }

        public Expr Left { get; }

        public Expr Right { get; }

        public override Env Env => Right.Env;

        public override bool IsLValue => false;

        public override ExprType Type => Left.Type.GetQualifiedType(false, false);
    }

    public sealed partial class ConditionalExpr : Expr
    {
        public ConditionalExpr(Expr cond, Expr trueExpr, Expr falseExpr, ExprType type)
        {
            Cond = cond;
            TrueExpr = trueExpr;
            FalseExpr = falseExpr;
            Type = type;
        }

        public readonly Expr Cond;

        public readonly Expr TrueExpr;

        public readonly Expr FalseExpr;

        public override bool IsLValue => false;

        public override ExprType Type { get; }

        public override Env Env => FalseExpr.Env;
    }

    public sealed partial class FuncCall : Expr
    {
        public FuncCall(Expr func, FunctionType funcType, List<Expr> args)
        {
            Func = func;
            FuncType = funcType;
            Args = args;
        }

        public Expr Func { get; }

        public FunctionType FuncType { get; }

        public IReadOnlyList<Expr> Args { get; }

        public override ExprType Type => FuncType.ReturnType;

        public override Env Env => Args.Any() ? Args.Last().Env : Func.Env;

        public override bool IsLValue => false;
    }

    /// <summary>
    /// Expr.name: Expr must be a struct or union.
    /// </summary>
    public sealed partial class Attribute : Expr
    {
        public Attribute(Expr expr, string name, ExprType type)
        {
            Expr = expr;
            Name = name;
            Type = type;
        }

        public Expr Expr { get; }

        public string Name { get; }

        public override Env Env => Expr.Env;

        public override ExprType Type { get; }

        // You might want to think of some special case like this.
        // struct EvilStruct {
        //     int a[10];
        // } evil;
        // evil.a <--- is this an lvalue?
        // Yes, it is. It cannot be assigned, but that's because of the wrong Type.
        public override bool IsLValue => Expr.IsLValue;
    }

    /// <summary>
    /// &amp;Expr: get the address of Expr.
    /// </summary>
    public sealed partial class Reference : Expr
    {
        public Reference(Expr expr)
        {
            Expr = expr;
            Type = new PointerType(expr.Type);
        }

        public Expr Expr { get; }

        public override Env Env => Expr.Env;

        public override ExprType Type { get; }

        // You might want to think of some special case like this.
        // int *a;
        // &(*a) = 3; // Is this okay?
        // But this should lead to an error: lvalue required.
        // The 'reference' operator only gets the 'current address'.
        public override bool IsLValue => false;
    }

    /// <summary>
    /// *Expr: Expr must be a pointer.
    /// 
    /// Arrays and functions are implicitly converted to pointers.
    /// 
    /// This is an lvalue, so it has an address.
    /// </summary>
    public sealed partial class Dereference : Expr
    {
        public Dereference(Expr expr, ExprType type)
        {
            Expr = expr;
            Type = type;
        }

        public Expr Expr { get; }

        public override Env Env => Expr.Env;

        public override bool IsLValue => true;

        public override ExprType Type { get; }
    }
}