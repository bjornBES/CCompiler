public struct Token
{
    public TokenType Type;
#nullable disable
    public string Value;
#nullable enable
    public int Line;
    public int Column;

    public string FormatToken()
    {
        string value = Value;
        if (string.IsNullOrEmpty(value))
        {
            value = "NULL";
        }
        return $"{Type.ToString().PadRight(15, ' ')} expr = {value} line = {Line} column = {Column}";
    }
    public int? bin_prec()
    {
        /*
        switch (t)
        {
            case TokenType.minus:
            case TokenType.plus:
                return 0;
            case TokenType.fslash:
            case TokenType.star:
                return 1;
            default:
            return null;
        }
         */
        return null;
    }
}
