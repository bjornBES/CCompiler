#nullable disable
public enum AssingmentOperator
{
	_null = -1,
    assignment,
}
[Serializable]
public class NodeTermIntLit : Term
{
	public Token Int_lit;
}
[Serializable]
public class NodeTermExpr : Term
{
	public Token Ident;
}
[Serializable]
public class NodeTermType : Term
{
	public bool IsUnsigned;
	public bool IsPointer;
	public Token Token;
}
[Serializable]
public class NodeTerm : Expr
{
	public Term term;
}

[Serializable]
public class NodeExpr
{
	public Expr expr;
}

public class NodeStmtAssing : Stmt
{
	public NodeTermType Type;
	public NodeTerm Name;
	public AssingmentOperator Operator;
	public NodeTerm expr;
}
[Serializable]
public class NodeStmtInt : Stmt
{
	public Token Name;
	public NodeExpr Expr;
}
[Serializable]
public class NodeStmtFunc : Stmt
{
	public NodeTerm Name;
	public NodeTermType Return_Type;
}
[Serializable]
public class NodeStmtReturn : Stmt
{
	public NodeExpr Expr;
}
[Serializable]
public class NodeScope : Stmt
{
	public NodeStmt[] Stmts;
}
[Serializable]
public class NodeStmt
{
	public Stmt stmt;
}
[Serializable]
public class NodeProg
{
	public NodeStmt[] stmts;
}
[Serializable]
public class Stmt { }
[Serializable]
public class Expr { }
[Serializable]
public class Term { }