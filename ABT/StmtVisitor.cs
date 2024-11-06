using System;
using System.Collections.Generic;

namespace CCompiler.ABT
{
    public abstract class StmtVisitor
    {
        public virtual void Visit(Stmt stmt) { }
        public virtual void Visit(GotoStmt stmt) { }
        public virtual void Visit(LabeledStmt stmt) { }
        public virtual void Visit(ContStmt stmt) { }
        public virtual void Visit(BreakStmt stmt) { }
        public virtual void Visit(ExprStmt stmt) { }
        public virtual void Visit(CompoundStmt stmt) { }
        public virtual void Visit(ReturnStmt stmt) { }
        public virtual void Visit(WhileStmt stmt) { }
        public virtual void Visit(DoWhileStmt stmt) { }
        public virtual void Visit(ForStmt stmt) { }
        public virtual void Visit(SwitchStmt stmt) { }
        public virtual void Visit(CaseStmt stmt) { }
        public virtual void Visit(DefaultStmt stmt) { }
        public virtual void Visit(IfStmt stmt) { }
        public virtual void Visit(IfElseStmt stmt) { }
    }

    public class CaseLabelsGrabber : StmtVisitor
    {
        private readonly List<int> _labels = new List<int>();
        public IReadOnlyList<int> Labels => _labels;

        public static IReadOnlyList<int> GrabLabels(SwitchStmt stmt)
        {
            CaseLabelsGrabber grabber = new CaseLabelsGrabber();
            stmt.Stmt.Accept(grabber);
            return grabber.Labels;
        }

        public override void Visit(Stmt stmt)
        {
            throw new InvalidOperationException("Cannot visit abstract Stmt");
        }

        public override void Visit(GotoStmt stmt) { }

        public override void Visit(LabeledStmt stmt) =>
            stmt.Stmt.Accept(this);

        public override void Visit(ContStmt stmt) { }

        public override void Visit(BreakStmt stmt) { }

        public override void Visit(ExprStmt stmt) { }

        public override void Visit(CompoundStmt stmt) =>
            stmt.Stmts.ForEach(_ => _.Item2.Accept(this));

        public override void Visit(ReturnStmt stmt) { }

        public override void Visit(WhileStmt stmt) =>
            stmt.Body.Accept(this);

        public override void Visit(DoWhileStmt stmt) =>
            stmt.Body.Accept(this);

        public override void Visit(ForStmt stmt) =>
            stmt.Body.Accept(this);

        public override void Visit(SwitchStmt stmt)
        {
            // Must ignore this.
        }

        public override void Visit(CaseStmt stmt)
        {
            // Record the Value.
            _labels.Add(stmt.Value);
            stmt.Stmt.Accept(this);
        }

        public override void Visit(DefaultStmt stmt) =>
            stmt.Stmt.Accept(this);

        public override void Visit(IfStmt stmt) =>
            stmt.Stmt.Accept(this);

        public override void Visit(IfElseStmt stmt)
        {
            stmt.TrueStmt.Accept(this);
            stmt.FalseStmt.Accept(this);
        }
    }

    public class GotoLabelsGrabber : StmtVisitor
    {
        private readonly List<string> _labels = new List<string>();
        public IReadOnlyList<string> Labels => _labels;

        public static IReadOnlyList<string> GrabLabels(Stmt stmt)
        {
            GotoLabelsGrabber grabber = new GotoLabelsGrabber();
            stmt.Accept(grabber);
            return grabber.Labels;
        }

        public override void Visit(Stmt stmt)
        {
            throw new InvalidOperationException("Cannot visit abstract Stmt");
        }

        public override void Visit(GotoStmt stmt) { }

        public override void Visit(LabeledStmt stmt)
        {
            _labels.Add(stmt.Label);
            stmt.Stmt.Accept(this);
        }

        public override void Visit(ContStmt stmt) { }

        public override void Visit(BreakStmt stmt) { }

        public override void Visit(ExprStmt stmt) { }

        public override void Visit(CompoundStmt stmt) =>
            stmt.Stmts.ForEach(_ => _.Item2.Accept(this));

        public override void Visit(ReturnStmt stmt) { }

        public override void Visit(WhileStmt stmt) =>
            stmt.Body.Accept(this);

        public override void Visit(DoWhileStmt stmt) =>
            stmt.Body.Accept(this);

        public override void Visit(ForStmt stmt) =>
            stmt.Body.Accept(this);

        public override void Visit(SwitchStmt stmt)
        {
            stmt.Stmt.Accept(this);
        }

        public override void Visit(CaseStmt stmt)
        {
            stmt.Stmt.Accept(this);
        }

        public override void Visit(DefaultStmt stmt) =>
            stmt.Stmt.Accept(this);

        public override void Visit(IfStmt stmt) =>
            stmt.Stmt.Accept(this);

        public override void Visit(IfElseStmt stmt)
        {
            stmt.TrueStmt.Accept(this);
            stmt.FalseStmt.Accept(this);
        }
    }
}

