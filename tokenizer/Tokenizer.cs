using System.Collections.Immutable;

namespace CCompiler.tokenizer
{
    public class Tokenizer
    {
#nullable disable
        public string m_src;
        public int m_index = 0;
        public int m_line = 1;
        public int m_column = 1;
        public List<Token> m_tokens = new List<Token>();
        public ImmutableList<FSA> FSAs;
        public void Build(string src)
        {
            FSAs = ImmutableList.Create<FSA>(
                new FSAFloat(),
                new FSAInt(),
                new FSAOperator(),
                new FSAIdentifier(),
                new FSASpace(),
                new FSANewLine(),
                new FSACharConst(),
                new FSAstring()
                );
            m_line = 1;
            m_column = 1;
            m_src = src;
            m_index = 0;
            Lex();
        }
        public void Lex()
        {
            while (peek().HasValue)
            {
                FSAs.ForEach(fsa =>
                {
                    if (!peek().HasValue)
                    {
                        return;
                    }
                    fsa.ReadChar(peek().Value);
                });
                    consume();

                // if no running
                if (FSAs.FindIndex(fsa => fsa.GetStatus() == FSAStatus.RUNNING) == -1)
                {
                    int idx = FSAs.FindIndex(fsa => fsa.GetStatus() == FSAStatus.END);
                    if (idx != -1)
                    {
                        Token token = FSAs[idx].RetrieveToken();
                        token.Column = m_column;
                        token.Line = m_line;
                        if (token.Kind != TokenKind.NONE)
                        {
                            m_tokens.Add(token);
                        }
                        m_index--;

                        if (FSAs[idx].GetType() == typeof(FSANewLine))
                        {
                            m_line++;
                            m_column = 1;
                        }

                        FSAs.ForEach(fsa => fsa.Reset());
                    }
                    else
                    {
                        Console.WriteLine("error");
                    }
                }
            }

            FSAs.ForEach(fsa => fsa.ReadEOF());
            // find END
            int idx2 = FSAs.FindIndex(fsa => fsa.GetStatus() == FSAStatus.END);
            if (idx2 != -1)
            {
                Token token = FSAs[idx2].RetrieveToken();
                if (token.Kind != TokenKind.NONE)
                {
                    m_tokens.Add(token);
                }
            }
            else
            {
                Console.WriteLine("error");
            }

            m_tokens.Add(new EmptyToken());
        }

        public char? peek(int offset = 0)
        {
            if (m_index + offset >= m_src.Length)
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
}