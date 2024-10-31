public class Generator
{
#nullable disable
    public NodeProg m_NodeProg;
    public List<string> m_output = new List<string>();

    public List<Var> m_vars = new List<Var>();
    public int m_stack_size = 0;
    public Stack<int> m_scopes = new Stack<int>();
    public int m_label_count = 0;
    public int m_argument_size = 0;
    string gen_term(NodeTerm term)
    {
        if (IsType<NodeTermIntLit>(term.term))
        {
            NodeTermIntLit nodeTermIntLit = GetType<NodeTermIntLit>(term.term);
            return nodeTermIntLit.Int_lit.Value;
        }
        else if (IsType<NodeTermExpr>(term.term))
        {
            NodeTermExpr nodeTermExpr = GetType<NodeTermExpr>(term.term);
            return nodeTermExpr.Ident.Value;
        }

        return "";
    }
    string gen_expr(NodeExpr nodeExpr)
    {
        if (IsType<NodeTerm>(nodeExpr.expr))
        {
            return gen_term(GetType<NodeTerm>(nodeExpr.expr));
        }

        return "";
    }

    public void gen_func(Stmt stmt)
    {
        NodeStmtFunc nodeStmtFunc = GetType<NodeStmtFunc>(stmt);

        AddToOutput($".global _{GetType<NodeTermExpr>(nodeStmtFunc.Name.term).Ident.Value}:", "");
        AddToOutput($"enter");
        m_argument_size = 0;
    }
    public void gen_scope(Stmt stmt)
    {
        NodeScope nodeScope = GetType<NodeScope>(stmt);
        begin_scope();
        for (int i = 0; i < nodeScope.Stmts.Length; i++)
        {
            if (nodeScope.Stmts[i] == null) continue;
            gen_stmt(nodeScope.Stmts[i].stmt);
        }
        end_scope();
    }
    public void gen_return(Stmt stmt)
    {
        NodeStmtReturn nodeStmtReturn = GetType<NodeStmtReturn>(stmt);

        pop("BP");
        AddToOutput($"mov".PadRight(16, ' ') + $"\tA,\t{gen_expr(nodeStmtReturn.Expr)}");
        AddToOutput($"ret".PadRight(16, ' ') + $"\t{m_argument_size}");
    }
    public void gen_assingment(Stmt stmt)
    {
        NodeStmtAssing nodeStmtAssing = GetType<NodeStmtAssing>(stmt);
        int size = 0;
        switch (nodeStmtAssing.Type.Token.Type)
        {
            case TokenType._int:
                push((Convert.ToInt64(gen_term(nodeStmtAssing.expr)) >> 16).ToString());
                push((Convert.ToInt64(gen_term(nodeStmtAssing.expr)) & 0x0000FFFF).ToString());
                size = 2;
                break;
            default:
                break;
        }
        m_vars.Add(new Var()
        {
            name = gen_term(nodeStmtAssing.Name),
            size = size,
            stack_loc = m_stack_size,
        }) ;
    }

    public void gen_stmt(Stmt stmt)
    {
        if (IsType<NodeStmtFunc>(stmt))
        {
            gen_func(stmt);
        }
        else if (IsType<NodeScope>(stmt))
        {
            gen_scope(stmt);
        }
        else if (IsType<NodeStmtReturn>(stmt))
        {
            gen_return(stmt);
        }
        else if (IsType<NodeStmtAssing>(stmt))
        {
            gen_assingment(stmt);
        }
    }

    public string[] Gen_prog(NodeProg nodeProg)
    {
        m_NodeProg = nodeProg;

        for (int i = 0; i < m_NodeProg.stmts.Length; i++)
        {
            gen_stmt(m_NodeProg.stmts[i].stmt);
        }
        pop("BP");
        AddToOutput($"mov".PadRight(16, ' ') + $"\tA,\t1");
        return m_output.ToArray();
    }

    bool IsType<T>(object stmt)
    {
        return stmt.GetType() == typeof(T);
    }
    T GetType<T>(object stmt)
    {
        if (IsType<T>(stmt))
        {
            return (T)stmt;
        }
        else
        {
            throw new InvalidCastException();
        }
    }

    void AddToOutput(string Str, string preFix = "\t")
    {
        m_output.Add(preFix + Str);
    }
    void push(string reg)
    {
        AddToOutput($"push".PadRight(16, ' ') + $"\t{reg}");
        m_stack_size++;
    }

    void pop(string reg)
    {
        AddToOutput($"pop".PadRight(16, ' ') + $"\t{reg}");
        m_stack_size--;
    }

    void begin_scope()
    {
        m_scopes.Push(m_vars.Count);
    }

    void end_scope()
    {
        int pop_count = m_vars.Count - m_scopes.Last();
        int Stack_size = 0;
        for (int i = 0; i < pop_count; i++)
        {
            Stack_size += m_vars[i].size;
            m_vars.RemoveAt(i);
        }
        if (pop_count != 0)
        {
            AddToOutput("sub".PadRight(16, ' ') + $"\tSP,\t{Stack_size}");
        }
        m_stack_size -= pop_count;
        m_scopes.Pop();
    }

    string create_label()
    {
        return "label" + m_label_count++;
    }
}
public class Var
{
    public string name;
    public int stack_loc;
    public int size;
}