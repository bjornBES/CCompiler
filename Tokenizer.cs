public partial class Tokenizer
{
#nullable disable
    public string m_src;
    public int m_index = 0;
    public int m_line = 1;
    public int m_column = 1;
    public List<Token> Build(string src)
    {
        m_src = src;
        string buffer = "";
        List<Token> tokens = new List<Token>();
        while (peek().HasValue)
        {
            buffer = "";
            if (char.IsLetter(peek().Value))
            {
                int saveColumn = m_column;
                buffer += consume();
                while (peek().HasValue && char.IsLetterOrDigit(peek().Value))
                {
                    buffer += consume();
                }

                switch (buffer)
                {
                    case "int": tokens.Add(new Token() { Type = TokenType._int, Column = saveColumn, Line = m_line }); break;
                    case "return": tokens.Add(new Token() { Type = TokenType._return, Column = saveColumn, Line = m_line }); break;
                    default: tokens.Add(new Token() { Type = TokenType.ident, Value = buffer, Column = saveColumn, Line = m_line }); break;
                }
            }
            else if (char.IsDigit(peek().Value))
            {
                int saveColumn = m_column;
                buffer += consume();
                while (peek().HasValue && char.IsDigit(peek().Value))
                {
                    buffer += consume();
                }
                tokens.Add(new Token() { Type = TokenType.int_lit, Value = buffer, Column = saveColumn, Line = m_line });
            }
            else if (peek().Value == '\n')
            {
                consume();
                m_column = 1;
                m_line++;
            }
            else if (char.IsWhiteSpace(peek().Value))
            {
                consume();
            }
            else
            {
                switch (peek().Value)
                {
                    case '(':
                        tokens.Add(new Token() { Type = TokenType.open_paren, Column = m_column, Line = m_line });
                        consume();
                        break;
                    case ')':
                        tokens.Add(new Token() { Type = TokenType.close_paren, Column = m_column, Line = m_line });
                        consume();
                        break;
                    case '{':
                        tokens.Add(new Token() { Type = TokenType.open_curly, Column = m_column, Line = m_line });
                        consume();
                        break;
                    case '}':
                        tokens.Add(new Token() { Type = TokenType.close_curly, Column = m_column, Line = m_line });
                        consume();
                        break;
                    case ';':
                        tokens.Add(new Token() { Type = TokenType.semi, Column = m_column, Line = m_line });
                        consume();
                        break;
                    case '=':
                        tokens.Add(new Token() { Type = TokenType.eq, Column = m_column, Line = m_line });
                        consume();
                        break;
                    default:
                        Console.WriteLine("Invalid token " + peek().Value);
                        Environment.Exit(1);
                        break;
                }
            }
        }
        m_index = 0;
        return tokens;
    }

    public char? peek(int offset = 0)
    {
        if(m_index + offset >= m_src.Length)
        {
            return null;
        }
        return m_src[offset + m_index];
    }
    public char consume()
    {
        m_column++;
        return m_src[m_index++];
    }
}
