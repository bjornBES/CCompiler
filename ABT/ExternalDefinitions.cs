
using CCompiler.CodeGeneration;

namespace CCompiler.ABT
{
    public class TranslnUnit
    {
        public TranslnUnit(List<Tuple<Env, ExternDecln>> _declns)
        {
            declns = _declns;
        }
        public readonly List<Tuple<Env, ExternDecln>> declns;

        public void CodeGenerate(CGenState state)
        {
            foreach (Tuple<Env, ExternDecln> decln in declns)
            {
                state.COMMENT($"; Current line {decln.Item2}");
                decln.Item2.CGenDecln(decln.Item1, state);
            }

        }
    }

    public interface ExternDecln
    {
        void CGenDecln(Env env, CGenState state);
    }

    public class FuncDef : ExternDecln
    {
        public FuncDef(string name, StorageClass scs, FunctionType type, Stmt stmt)
        {
            this.name = name;
            this.scs = scs;
            this.type = type;
            this.stmt = stmt;
        }

        public override string ToString() => $"fn {name}: {type}";

        public void CGenDecln(Env env, CGenState state)
        {
            state.TEXT();
            Env.Entry entry = env.Find(name).Value;
            state.COMMENT(ToString());
            switch (entry.Kind)
            {
                case Env.EntryKind.GLOBAL:
                    switch (scs)
                    {
                        case StorageClass.AUTO:
                        case StorageClass.EXTERN:
                            state.GLOBL(name);
                            break;
                        case StorageClass.STATIC:
                            // static definition
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                    break;
                default:
                    throw new InvalidOperationException();
            }
            state.CGenFuncStart(name);

            state.InFunction(GotoLabelsGrabber.GrabLabels(stmt));

            stmt.CGenStmt(env, state);

            state.CGenLabel(state.ReturnLabel);
            state.OutFunction();

            //     leave
            //     ret
            state.POPR();
            state.LEAVE();
            state.RET(type);
            state.NEWLINE();
        }

        public readonly string name;
        public readonly StorageClass scs;
        public readonly FunctionType type;
        public readonly Stmt stmt;
    }
}