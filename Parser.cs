using System.Linq.Expressions;

public partial class Parser
{
#nullable disable
    public Token[] m_token;
    public int m_index = 0;
    List<NodeStmt> m_stmts = new List<NodeStmt>();
    bool m_Unsigned = false;
    bool m_Pointer = false;

    NodeTerm Parse_term()
    {
        NodeTerm term = null;
        if (peek().HasValue && peek().Value.Type == TokenType.ident)
        {
            term = new NodeTerm();
            term.term = new NodeTermExpr()
            {
                Ident = consume(),
            };
        }
        else if (peek().HasValue && peek().Value.Type == TokenType.int_lit)
        {
            term = new NodeTerm();
            term.term = new NodeTermIntLit()
            {
                Int_lit = consume(),
            };
        }

        return term;
    }
    NodeExpr Parse_expr(int min_proc = 0)
    {
        NodeTerm term_lhs = Parse_term();
        if (term_lhs == null)
        {
            return null;
        }

        NodeExpr expr_lhs = new NodeExpr() { expr = term_lhs };

        while (true)
        {
            Token? curr_tok = peek();
            int? prec;
            if(curr_tok.HasValue)
            {
                prec = curr_tok.Value.bin_prec();
                if(!prec.HasValue | prec < min_proc)
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return expr_lhs;
    }
    NodeStmtAssing Parse_assing()
    {
        Token type = consume();
        NodeTerm name = Parse_term();
        AssingmentOperator assingmentOperator = AssingmentOperator._null;
        if (!peek().HasValue) expected_expr();
        switch (peek().Value.Type)
        {
            case TokenType.eq:
                assingmentOperator = AssingmentOperator.assignment;
                consume();
                break;
            default:
                break;
        }
        NodeTerm expr = Parse_term();
        return new NodeStmtAssing()
        {
            Type = new NodeTermType()
            {
                Token = type,
            },
            expr = expr,
            Name = name,
            Operator = assingmentOperator,
        };
    }
    NodeStmtFunc Parse_func()
    {
        Token returnType = consume();
        NodeTerm name = Parse_term();

        try_consume_err(TokenType.open_paren);
        try_consume_err(TokenType.close_paren);

        return new NodeStmtFunc()
        {
            Name = name,
            Return_Type = new NodeTermType()
            {
                Token = returnType,
            },
        };
    }
    NodeStmtReturn Parse_return()
    {
        try_consume_err(TokenType._return);
        NodeExpr expr = Parse_expr();
        try_consume_err(TokenType.semi);

        return new NodeStmtReturn()
        {
            Expr = expr,
        };
    }
    NodeScope Parse_Scope()
    {
        List<NodeStmt> stmts = new List<NodeStmt>();
        try_consume_err(TokenType.open_curly);
        while (true)
        {
            stmts.Add(Parse_Stmt());
            if(try_consume(TokenType.close_curly))
            {
                break;
            }
        }

        return new NodeScope()
        {
            Stmts = stmts.ToArray(),
        };
    }
    public NodeStmt Parse_Stmt()
    {
        if (peek().Value.Type == TokenType._int)
        {
            if (peek(2).HasValue && peek(2).Value.Type == TokenType.open_paren)
            {
                return new NodeStmt() { stmt = Parse_func() };
            }
            else if (peek(2).HasValue && peek(2).Value.Type == TokenType.eq)
            {
                return new NodeStmt() { stmt = Parse_assing() };
            }
        }
        else if (peek().Value.Type == TokenType.open_curly)
        {
            return new NodeStmt() { stmt = Parse_Scope() };
        }
        else if (peek().Value.Type == TokenType._return)
        {
            return new NodeStmt() { stmt = Parse_return() };
        }

        return null;
    }
#nullable enable

    public NodeProg Parse_Prog(Token[] tokens)
    {
        NodeProg nodeProg = new NodeProg();
        m_token = tokens;
        while (peek().HasValue)
        {
            m_stmts.Add(Parse_Stmt());
        }
        nodeProg.stmts = m_stmts.ToArray();
        return nodeProg;
    }

    public Token? peek(int offset = 0)
    {
        if (m_index + offset >= m_token.Length)
        {
            return null;
        }
        return m_token[m_index + offset];
    }
    public Token consume()
    {
        return m_token[m_index++];
    }
    public bool try_consume(TokenType expectedType)
    {
        return peek().HasValue && consume().Type == expectedType;
    }
    public bool try_consume_err(TokenType expectedType)
    {
        if (!peek().HasValue || consume().Type != expectedType)
        {
            expected_error(expectedType);
        }
        return true;
    }

    void expected_expr ()
    {
        int goingBack = -1;
        for (int i = m_index; m_token.Length > i; i--)
        {
            if (peek(i).HasValue) break;
            goingBack--;
        }
#nullable disable
        Console.WriteLine($"[Parser Error]\nExpected Expression on line {peek(goingBack).Value.Line}:{peek(goingBack).Value.Column}");
#nullable enable
        Environment.Exit(1);
    }
    void expected_error(TokenType expectedtoken)
    {
        int goingBack = -1;
        for (int i = m_index; m_token.Length > i; i--)
        {
            if (peek(i).HasValue) break;
            goingBack--;
        }
#nullable disable
        Console.WriteLine($"[Parser Error]\nExpected `{expectedtoken}` on line {peek(goingBack).Value.Line}:{peek(goingBack).Value.Column}");
#nullable enable
        Environment.Exit(1);
    }
}
