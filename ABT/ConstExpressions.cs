using System;

namespace CCompiler.ABT
{

    /// <summary>
    /// Compile-time constant. Cannot get the address.
    /// </summary>
    public abstract partial class ConstExpr : Expr
    {
        protected ConstExpr(Env env)
        {
            Env = env;
        }

        public override sealed Env Env { get; }

        public override sealed bool IsConstExpr => true;

        public override sealed bool IsLValue => false;
    }

    public sealed partial class ConstLong : ConstExpr
    {
        public ConstLong(int value, Env env)
            : base(env)
        {
            Value = value;
        }
        public int Value { get; }

        public override string ToString() => $"{Value}";

        private static ExprType _type = new LongType(true);
        public override ExprType Type => _type;
    }

    public sealed partial class ConstULong : ConstExpr
    {
        public ConstULong(uint value, Env env)
            : base(env)
        {
            Value = value;
        }
        public uint Value { get; }

        public override string ToString() => $"{Value}u";

        private static ExprType _type = new ULongType(true);
        public override ExprType Type => _type;
    }

    public sealed partial class ConstShort : ConstExpr
    {
        public ConstShort(short value, Env env)
            : base(env)
        {
            Value = value;
        }

        public short Value { get; }

        private static ExprType _type = new ShortType(true);
        public override ExprType Type => _type;
    }

    public sealed partial class ConstUShort : ConstExpr
    {
        public ConstUShort(ushort value, Env env)
            : base(env)
        {
            Value = value;
        }

        public ushort Value { get; }

        private static ExprType _type = new UShortType(true);
        public override ExprType Type => _type;
    }

    public sealed partial class ConstChar : ConstExpr
    {
        public ConstChar(sbyte value, Env env)
            : base(env)
        {
            Value = value;
        }

        public sbyte Value { get; }

        private static ExprType _type = new CharType(true);
        public override ExprType Type => _type;
    }

    public sealed partial class ConstUChar : ConstExpr
    {
        public ConstUChar(byte value, Env env)
            : base(env)
        {
            Value = value;
        }

        public byte Value { get; }

        private static ExprType _type = new UCharType(true);
        public override ExprType Type => _type;
    }

    public sealed partial class ConstPtr : ConstExpr
    {
        public ConstPtr(uint value, ExprType type, Env env)
            : base(env)
        {
            Value = value;
            Type = type;
        }

        public uint Value { get; }

        public override ExprType Type { get; }

        public override string ToString() =>
            $"({Type} *)0x{Value.ToString("X8")}";
    }

    public sealed partial class ConstFloat : ConstExpr
    {
        public ConstFloat(float value, Env env)
            : base(env)
        {
            Value = value;
        }

        public float Value { get; }

        public override string ToString() => $"{Value}f";

        private static ExprType _type = new FloatType(true);
        public override ExprType Type => _type;
    }

    public sealed partial class ConststringLiteral : ConstExpr
    {
        public ConststringLiteral(string value, Env env)
            : base(env)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString() => $"\"{Value}\"";

        private static ExprType _type = new PointerType(new CharType(true), true);
        public override ExprType Type => _type;
    }

}
