namespace CCompiler.tokenizer
{
    public abstract class Token
    {
        public override string ToString()
        {
            return Kind.ToString();
        }
        public abstract TokenKind Kind { get; }
        public int Column;
        public int Line;
    }
}