namespace CCompiler.tokenizer
{
    public sealed class EmptyToken : Token
    {
        public override TokenKind Kind { get; } = TokenKind.NONE;
    }
}
