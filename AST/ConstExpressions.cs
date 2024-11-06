using System;
using CCompiler.tokenizer;

namespace CCompiler.AST {

    public abstract class Literal : Expr { }

	/// <summary>
	/// May be a float or double
	/// </summary>
	public class FloatLiteral : Literal {
		public FloatLiteral(Double value, FloatSuffix floatSuffix) {
			this.Value = value;
			this.FloatSuffix = floatSuffix;
		}

		public FloatSuffix FloatSuffix { get; }

		public Double Value { get; }

        public override ABT.Expr GetExpr(ABT.Env env) {
            switch (this.FloatSuffix) {
                case FloatSuffix.F:
                    return new ABT.ConstFloat((Single)this.Value, env);

                case FloatSuffix.NONE:
                case FloatSuffix.L:
                    return new ABT.ConstDouble(this.Value, env);

                default:
                    throw new InvalidOperationException();
            }
        }
	}

	/// <summary>
	/// May be signed or unsigned
    /// C doesn't have char constant, only int constant
	/// </summary>
	public class IntLiteral : Literal {
		public IntLiteral(long value, IntSuffix suffix) {
			this.Value = value;
			this.Suffix = suffix;
		}

		public IntSuffix Suffix { get; }
		public long Value { get; }

        public override ABT.Expr GetExpr(ABT.Env env) {
            switch (this.Suffix) {
                case IntSuffix.U:
                case IntSuffix.UL:
                    return new ABT.ConstULong((uint)this.Value, env);

                case IntSuffix.NONE:
                case IntSuffix.L:
                    return new ABT.ConstLong((int)this.Value, env);

                default:
                    throw new InvalidOperationException();
            }
        }
    }

	/// <summary>
	/// string Literal
	/// </summary>
	public class stringLiteral : Expr {
		public stringLiteral(string value) {
			this.Value = value;
		}

		public string Value { get; }

		public override ABT.Expr GetExpr(ABT.Env env) {
			return new ABT.ConststringLiteral(this.Value, env);
		}
	}

}